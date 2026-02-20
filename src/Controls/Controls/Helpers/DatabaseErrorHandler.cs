using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.Sqlite;

namespace Controls.Helpers
{
    /// <summary>
    /// Обработчик ошибок доступа к базе данных
    /// </summary>
    public static class DatabaseErrorHandler
    {
        private const int DefaultConnectionTimeout = 10000;
        private const int MaxRetryAttempts = 5;
        private const int RetryDelayMs = 5000;
        /// <summary>
        /// Проверить доступность БД перед операцией
        /// </summary>
        public static bool CheckDatabaseAccess(string databasePath, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                if (!File.Exists(databasePath))
                {
                    errorMessage = $"Файл базы данных не найден:\n{databasePath}";
                    return false;
                }

                try
                {
                    using var fs = File.Open(databasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                }
                catch (UnauthorizedAccessException)
                {
                    errorMessage = $"Нет прав доступа к БД:\n{databasePath}";
                    return false;
                }
                catch (IOException ex)
                {
                    errorMessage = $"Файл БД заблокирован или недоступен:\n{ex.Message}";
                    return false;
                }

                if (databasePath.StartsWith(@"\\"))
                {
                    var directory = Path.GetDirectoryName(databasePath);
                    if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                    {
                        errorMessage = $"Сетевая папка недоступна:\n{directory}";
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMessage = $"Ошибка проверки доступа к БД:\n{ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Обработать SQLite исключение и вернуть понятное сообщение пользователю
        /// </summary>
        public static string HandleSqliteException(SqliteException ex, string databasePath)
        {
            var errorMessage = string.Empty;
            var isNetworkPath = databasePath.StartsWith(@"\\");
            
            switch (ex.SqliteErrorCode)
            {
                case 5:
                    errorMessage = "База данных временно заблокирована другим пользователем.\n" +
                                 "Это нормально при многопользовательском доступе.\n\n" +
                                 "Приложение автоматически повторит попытку...";
                    if (isNetworkPath)
                    {
                        errorMessage += "\n\n💡 Работа через сеть: небольшие задержки - это нормально.";
                    }
                    break;

                case 8:
                    errorMessage = "База данных открыта только для чтения.\n" +
                                 "Проверьте права доступа к файлу.";
                    break;

                case 10:
                    errorMessage = "Ошибка ввода-вывода при работе с БД.";
                    if (isNetworkPath)
                    {
                        errorMessage += "\n\nВозможные причины:\n" +
                                      "• Нестабильное сетевое соединение\n" +
                                      "• Сетевая папка отключена\n" +
                                      "• Недостаточно прав доступа";
                    }
                    break;

                case 11:
                    errorMessage = "⚠️ База данных повреждена!\n\n" +
                                 "Восстановите БД из резервной копии.";
                    break;

                case 13:
                    errorMessage = "Недостаточно места на диске.";
                    break;

                case 14:
                    errorMessage = "Не удается открыть файл базы данных.\n\n";
                    
                    var directory = Path.GetDirectoryName(databasePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        errorMessage += $"Папка не существует:\n{directory}\n\n" +
                                      "Проверьте права доступа для создания папки.";
                    }
                    else if (isNetworkPath)
                    {
                        errorMessage += "Проверьте:\n" +
                                      "• Доступность сетевой папки\n" +
                                      "• Правильность пути к файлу\n" +
                                      "• Сетевое подключение\n" +
                                      "• Права доступа на запись";
                    }
                    else
                    {
                        errorMessage += "Проверьте:\n" +
                                      "• Права доступа к папке\n" +
                                      "• Наличие свободного места\n" +
                                      $"• Путь: {databasePath}";
                    }
                    break;

                case 26:
                    errorMessage = "Выбранный файл не является базой данных SQLite.";
                    break;

                default:
                    errorMessage = $"Ошибка SQLite (код {ex.SqliteErrorCode}):\n{ex.Message}";
                    break;
            }

            return errorMessage;
        }

        /// <summary>
        /// Показать предупреждение пользователю об ошибке БД
        /// </summary>
        public static void ShowDatabaseError(string errorMessage, string title = "Ошибка базы данных")
        {
            MessageBox.Show(
                errorMessage,
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        /// <summary>
        /// Проверить подключение к БД и показать результат
        /// </summary>
        public static bool TestDatabaseConnection(string databasePath, bool showSuccessMessage = false)
        {
            try
            {
                if (!CheckDatabaseAccess(databasePath, out var errorMessage))
                {
                    ShowDatabaseError(errorMessage, "Проверка доступа к БД");
                    return false;
                }

                var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
                    Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
                    DefaultTimeout = 60
                };
                
                using var connection = new SqliteConnection(connectionString.ToString());
                connection.Open();
                
                using var command = connection.CreateCommand();
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
                command.ExecuteScalar();
                
                connection.Close();

                if (showSuccessMessage)
                {
                    var isNetworkPath = databasePath.StartsWith(@"\\");
                    var pathType = isNetworkPath ? "🌐 Сетевая" : "💻 Локальная";
                    
                    MessageBox.Show(
                        $"✅ Подключение успешно!\n\n{pathType} БД доступна:\n{databasePath}",
                        "Проверка подключения",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                return true;
            }
            catch (SqliteException ex)
            {
                var errorMessage = HandleSqliteException(ex, databasePath);
                ShowDatabaseError(errorMessage, "Проверка подключения");
                return false;
            }
            catch (Exception ex)
            {
                ShowDatabaseError($"Неожиданная ошибка:\n{ex.Message}", "Проверка подключения");
                return false;
            }
        }

        /// <summary>
        /// Асинхронная проверка доступности БД с таймаутом
        /// </summary>
        public static async Task<bool> CheckDatabaseAccessAsync(string databasePath, int timeoutMs = DefaultConnectionTimeout, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var checkTask = Task.Run(() =>
                    {
                        if (!File.Exists(databasePath))
                        {
                            return false;
                        }

                        if (databasePath.StartsWith(@"\\"))
                        {
                            var directory = Path.GetDirectoryName(databasePath);
                            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                            {
                                return false;
                            }
                        }

                        using var fs = File.Open(databasePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                        return true;
                    }, cancellationToken);

                    if (checkTask.Wait(timeoutMs, cancellationToken))
                    {
                        return checkTask.Result;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Попытка подключения к БД с повторными попытками
        /// </summary>
        public static async Task<(bool success, string errorMessage)> TryConnectWithRetryAsync(
            string databasePath,
            int maxAttempts = MaxRetryAttempts,
            Action<int, int>? onAttempt = null,
            CancellationToken cancellationToken = default)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return (false, "Операция отменена пользователем");
                }

                onAttempt?.Invoke(attempt, maxAttempts);

                try
                {
                    var isAccessible = await CheckDatabaseAccessAsync(databasePath, DefaultConnectionTimeout, cancellationToken);
                    
                    if (!isAccessible)
                    {
                        if (attempt < maxAttempts)
                        {
                            await Task.Delay(RetryDelayMs, cancellationToken);
                            continue;
                        }
                        else
                        {
                            return (false, "База данных остается недоступной после всех попыток");
                        }
                    }

                    await Task.Run(() =>
                    {
                        var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                        {
                            DataSource = databasePath,
                            Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
                            Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
                            DefaultTimeout = 60
                        };
                        
                        using var connection = new SqliteConnection(connectionString.ToString());
                        connection.Open();
                        using var command = connection.CreateCommand();
                        command.CommandText = "SELECT COUNT(*) FROM sqlite_master;";
                        command.ExecuteScalar();
                        connection.Close();
                    }, cancellationToken);

                    return (true, string.Empty);
                }
                catch (SqliteException ex)
                {
                    var errorMsg = HandleSqliteException(ex, databasePath);

                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(RetryDelayMs, cancellationToken);
                    }
                    else
                    {
                        return (false, errorMsg);
                    }
                }
                catch (OperationCanceledException)
                {
                    return (false, "Операция отменена пользователем");
                }
                catch (Exception ex)
                {
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(RetryDelayMs, cancellationToken);
                    }
                    else
                    {
                        return (false, $"Не удалось подключиться: {ex.Message}");
                    }
                }
            }

            return (false, "Превышено максимальное количество попыток подключения");
        }
    }
}
