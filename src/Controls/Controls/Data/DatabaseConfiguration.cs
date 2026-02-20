using System;
using System.IO;
using Controls.Helpers;

namespace Controls.Data
{
    /// <summary>
    /// Конфигурация базы данных
    /// </summary>
    public static class DatabaseConfiguration
    {
        private static AppSettings? _settings;

        /// <summary>
        /// Получить путь к базе данных
        /// </summary>
        public static string GetDatabasePath()
        {
            _settings ??= AppSettings.Load();

            if (!string.IsNullOrEmpty(_settings.DatabasePath))
            {
                if (File.Exists(_settings.DatabasePath))
                {
                    return _settings.DatabasePath;
                }
            }

            var defaultPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "database",
                "controls.db"
            );

            var normalizedPath = Path.GetFullPath(defaultPath);
            
            var directory = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrEmpty(directory))
            {
                if (!Directory.Exists(directory))
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return GetFallbackDatabasePath();
                    }
                    catch (Exception)
                    {
                        return GetFallbackDatabasePath();
                    }
                }
                
                try
                {
                    var testFile = Path.Combine(directory, $".write_test_{Guid.NewGuid()}.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }
                catch (UnauthorizedAccessException)
                {
                    return GetFallbackDatabasePath();
                }
                catch (Exception)
                {
                }
            }

            return normalizedPath;
        }
        
        /// <summary>
        /// Получить резервный путь к БД в AppData пользователя
        /// </summary>
        private static string GetFallbackDatabasePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var fallbackPath = Path.Combine(appDataPath, "Controls.TaskManager", "database", "controls.db");
            
            var directory = Path.GetDirectoryName(fallbackPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception)
                {
                }
            }
            
            return fallbackPath;
        }

        /// <summary>
        /// Установить пользовательский путь к базе данных и сохранить в настройках
        /// Поддерживает локальные и сетевые пути (например: \\server\share\controls.db)
        /// </summary>
        public static void SetDatabasePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Путь к базе данных не может быть пустым", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException("Файл базы данных не найден", path);

            _settings = AppSettings.Load();
            _settings.DatabasePath = path;
            
            _settings.Save();
        }

        /// <summary>
        /// Сбросить путь к базе данных (вернуться к пути по умолчанию)
        /// </summary>
        public static void ResetDatabasePath()
        {
            _settings = AppSettings.Load();
            _settings.DatabasePath = null;
            _settings.Save();
        }

        /// <summary>
        /// Проверить существование базы данных
        /// </summary>
        public static bool DatabaseExists()
        {
            var path = GetDatabasePath();
            return File.Exists(path);
        }

        /// <summary>
        /// Получить текущий сохраненный путь к БД (или null, если используется по умолчанию)
        /// </summary>
        public static string? GetSavedDatabasePath()
        {
            _settings ??= AppSettings.Load();
            return _settings.DatabasePath;
        }

        /// <summary>
        /// Настроить SQLite для оптимальной работы в сетевом режиме
        /// Включает WAL режим для лучшей конкурентности
        /// </summary>
        public static void ConfigureForNetworkAccess(string databasePath)
        {
            try
            {
                var connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
                {
                    DataSource = databasePath,
                    Cache = Microsoft.Data.Sqlite.SqliteCacheMode.Shared,
                    Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate,
                    DefaultTimeout = 60
                };

                using var connection = new Microsoft.Data.Sqlite.SqliteConnection(connectionString.ToString());
                connection.Open();
                
                string currentJournalMode = "";
                using (var checkCommand = connection.CreateCommand())
                {
                    checkCommand.CommandText = "PRAGMA journal_mode;";
                    var result = checkCommand.ExecuteScalar();
                    currentJournalMode = result?.ToString()?.ToUpperInvariant() ?? "";
                }
                
                if (currentJournalMode != "WAL")
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA journal_mode=WAL;";
                        command.ExecuteNonQuery();
                    }
                }
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA busy_timeout=60000;";
                    command.ExecuteNonQuery();
                }
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA cache_size=-64000;";
                    command.ExecuteNonQuery();
                }
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA synchronous=NORMAL;";
                    command.ExecuteNonQuery();
                }
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA locking_mode=NORMAL;";
                    command.ExecuteNonQuery();
                }
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA read_uncommitted=1;";
                    command.ExecuteNonQuery();
                }
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "PRAGMA wal_autocheckpoint=1000;";
                    command.ExecuteNonQuery();
                }
                
                var settings = new System.Text.StringBuilder();
                settings.AppendLine("SQLite настройки многопользовательского режима:");
                
                string CheckPragma(string pragmaName)
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $"PRAGMA {pragmaName};";
                    return cmd.ExecuteScalar()?.ToString() ?? "unknown";
                }
                
                settings.AppendLine($"  journal_mode: {CheckPragma("journal_mode")}");
                settings.AppendLine($"  locking_mode: {CheckPragma("locking_mode")}");
                settings.AppendLine($"  synchronous: {CheckPragma("synchronous")}");
                settings.AppendLine($"  read_uncommitted: {CheckPragma("read_uncommitted")}");
                settings.AppendLine($"  busy_timeout: {CheckPragma("busy_timeout")} ms");
            }
            catch (Exception)
            {
            }
        }
    }
}
