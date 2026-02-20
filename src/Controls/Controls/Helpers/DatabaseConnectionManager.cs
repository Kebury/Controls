using System;
using System.Threading.Tasks;
using System.Windows;
using Controls.Data;
using Controls.Views;
using Microsoft.Win32;

namespace Controls.Helpers
{
    /// <summary>
    /// Менеджер подключения к базе данных с обработкой недоступности
    /// </summary>
    public static class DatabaseConnectionManager
    {
        /// <summary>
        /// Попытка подключения к БД с UI для пользователя
        /// Возвращает true если подключение успешно, false если нужно завершить приложение
        /// </summary>
        public static async Task<bool> EnsureDatabaseConnectionAsync()
        {
            var dbPath = DatabaseConfiguration.GetDatabasePath();

            var isAccessible = await DatabaseErrorHandler.CheckDatabaseAccessAsync(dbPath, 5000);

            if (isAccessible)
            {
                return true;
            }

            return await HandleDatabaseUnavailableAsync(dbPath);
        }

        /// <summary>
        /// Обработка ситуации когда БД недоступна
        /// </summary>
        private static async Task<bool> HandleDatabaseUnavailableAsync(string databasePath)
        {
            while (true)
            {
                var isNetworkPath = databasePath.StartsWith(@"\\");
                var errorMessage = isNetworkPath
                    ? "Сетевая база данных временно недоступна.\n\nВозможно сеть перегружена или сервер не отвечает."
                    : "Файл базы данных недоступен.\n\nПроверьте наличие файла и права доступа.";

                var dialog = new DatabaseUnavailableDialog(databasePath, errorMessage);
                var result = dialog.ShowDialog();

                if (result != true)
                {
                    return false;
                }

                switch (dialog.SelectedAction)
                {
                    case DatabaseUnavailableAction.WaitForAccess:
                        var waitResult = await WaitForDatabaseAccessAsync(databasePath);
                        if (waitResult)
                        {
                            return true;
                        }
                        break;

                    case DatabaseUnavailableAction.SelectOtherDatabase:
                        var selectResult = await SelectAlternativeDatabaseAsync();
                        if (selectResult)
                        {
                            databasePath = DatabaseConfiguration.GetDatabasePath();
                            var isAccessible = await DatabaseErrorHandler.CheckDatabaseAccessAsync(databasePath, 5000);
                            if (isAccessible)
                            {
                                return true;
                            }
                        }
                        break;

                    case DatabaseUnavailableAction.Exit:
                        return false;
                }
            }
        }

        /// <summary>
        /// Ожидание доступа к БД с индикатором загрузки
        /// </summary>
        private static async Task<bool> WaitForDatabaseAccessAsync(string databasePath)
        {
            var loadingDialog = new DatabaseLoadingDialog();
            
            var connectionTask = Task.Run(async () =>
            {
                var result = await DatabaseErrorHandler.TryConnectWithRetryAsync(
                    databasePath,
                    maxAttempts: 5,
                    onAttempt: (current, max) =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            loadingDialog.UpdateStatus("Подключение");
                        });
                    },
                    cancellationToken: loadingDialog.CancellationToken
                );
                return result;
            });

            loadingDialog.ShowDialog();

            var (success, errorMessage) = await connectionTask;

            if (success)
            {
                await loadingDialog.ShowSuccess();
                return true;
            }
            else if (loadingDialog.IsCancelled)
            {
                return false;
            }
            else
            {
                loadingDialog.ShowError("Не удалось подключиться");
                await Task.Delay(2000);
                return false;
            }
        }

        /// <summary>
        /// Выбор альтернативной базы данных
        /// </summary>
        private static async Task<bool> SelectAlternativeDatabaseAsync()
        {
            return await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "SQLite Database|*.db|All files|*.*",
                    Title = "Выбрать базу данных для подключения (локальную или сетевую)",
                    CheckFileExists = true,
                    Multiselect = false
                };

                if (openDialog.ShowDialog() == true)
                {
                    var selectedPath = openDialog.FileName;

                    try
                    {
                        DatabaseConfiguration.SetDatabasePath(selectedPath);
                        
                        if (selectedPath.StartsWith(@"\\"))
                        {
                            DatabaseConfiguration.ConfigureForNetworkAccess(selectedPath);
                        }

                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка при подключении к выбранной БД:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return false;
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// Показать предупреждение о проблемах с БД
        /// </summary>
        public static void ShowDatabaseWarning(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    message,
                    "⚠️ Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            });
        }
    }
}
