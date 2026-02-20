using System;
using System.IO;
using System.Text.Json;

namespace Controls.Helpers
{
    /// <summary>
    /// Класс для хранения и загрузки настроек приложения
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Controls",
            "settings.json"
        );

        /// <summary>
        /// Путь к файлу базы данных (может быть локальным или сетевым, например: \\server\share\controls.db)
        /// </summary>
        public string? DatabasePath { get; set; }

        /// <summary>
        /// Последнее обновление настроек
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        /// <summary>
        /// Загрузить настройки из файла
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch
            {
            }

            return new AppSettings();
        }

        /// <summary>
        /// Сохранить настройки в файл
        /// </summary>
        public void Save()
        {
            try
            {
                LastUpdated = DateTime.Now;

                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Получить путь к файлу настроек (для отладки)
        /// </summary>
        public static string GetSettingsPath() => SettingsFilePath;
    }
}
