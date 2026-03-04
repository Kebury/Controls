using System.Text.RegularExpressions;

namespace Controls.Helpers
{
    /// <summary>
    /// Вспомогательный класс для быстрого обнаружения дат в строках.
    /// Использует compiled regex для производительности.
    /// </summary>
    public static class DateDetector
    {
        // Регулярные выражения для поиска дат в различных форматах
        // Скомпилированы при инициализации класса для производительности
        private static readonly Regex DatePattern1 = new(@"\d{1,2}\.\d{1,2}\.\d{2,4}", RegexOptions.Compiled);
        private static readonly Regex DatePattern2 = new(@"\d{1,2}\.\d{1,2}", RegexOptions.Compiled);
        private static readonly Regex DatePattern3 = new(@"\d{1,2}/\d{1,2}/\d{2,4}", RegexOptions.Compiled);
        private static readonly Regex DatePattern4 = new(@"\d{1,2}/\d{1,2}", RegexOptions.Compiled);
        private static readonly Regex DatePattern5 = new(@"\d{1,2}-\d{1,2}-\d{2,4}", RegexOptions.Compiled);
        private static readonly Regex DatePattern6 = new(@"\d{1,2}-\d{1,2}", RegexOptions.Compiled);

        /// <summary>
        /// Определяет, содержит ли текст дату (форматы: dd.MM.yyyy, dd.MM.yy, dd.MM, dd/MM и т.д.)
        /// </summary>
        /// <param name="text">Текст для проверки</param>
        /// <returns>True, если текст содержит дату; иначе False</returns>
        public static bool ContainsDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            return DatePattern1.IsMatch(text) ||
                   DatePattern2.IsMatch(text) ||
                   DatePattern3.IsMatch(text) ||
                   DatePattern4.IsMatch(text) ||
                   DatePattern5.IsMatch(text) ||
                   DatePattern6.IsMatch(text);
        }
    }
}
