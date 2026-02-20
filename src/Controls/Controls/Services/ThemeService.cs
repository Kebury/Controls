using System;
using System.IO;
using System.Windows;
using System.Text.Json;

namespace Controls.Services
{
    public class ThemeService
    {
        private const string SettingsFileName = "theme_settings.json";
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Controls",
            SettingsFileName
        );

        public enum Theme
        {
            Light,
            Dark
        }

        private class ThemeSettings
        {
            public string Theme { get; set; } = "Light";
        }

        /// <summary>
        /// Применяет тему к приложению
        /// </summary>
        public static void ApplyTheme(Theme theme)
        {
            var app = Application.Current;
            if (app == null) return;

            var mergedDictionaries = app.Resources.MergedDictionaries;
            
            var themeUri = theme == Theme.Dark
                ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Themes/LightTheme.xaml", UriKind.Relative);

            if (mergedDictionaries.Count > 0)
            {
                mergedDictionaries[0].Source = themeUri;
            }
            else
            {
                mergedDictionaries.Add(new ResourceDictionary { Source = themeUri });
            }

            SaveTheme(theme);
        }

        /// <summary>
        /// Получает текущую тему
        /// </summary>
        public static Theme GetCurrentTheme()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                    
                    if (settings != null && Enum.TryParse<Theme>(settings.Theme, out var theme))
                    {
                        return theme;
                    }
                }
            }
            catch
            {
            }

            return Theme.Light;
        }

        /// <summary>
        /// Загружает сохранённую тему при запуске приложения
        /// </summary>
        public static void LoadSavedTheme()
        {
            var theme = GetCurrentTheme();
            ApplyTheme(theme);
        }

        /// <summary>
        /// Сохраняет выбранную тему
        /// </summary>
        private static void SaveTheme(Theme theme)
        {
            try
            {
                var directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var settings = new ThemeSettings { Theme = theme.ToString() };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                File.WriteAllText(SettingsFilePath, json);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Переключает тему на противоположную
        /// </summary>
        public static void ToggleTheme()
        {
            var currentTheme = GetCurrentTheme();
            var newTheme = currentTheme == Theme.Light ? Theme.Dark : Theme.Light;
            ApplyTheme(newTheme);
        }
    }
}
