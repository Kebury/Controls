using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Controls.Helpers
{
    /// <summary>
    /// Конвертер для проверки, является ли строка не пустой.
    /// Если targetType == Visibility, возвращает Visible/Collapsed; иначе bool.
    /// </summary>
    public class IsNotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var notEmpty = value is string str && !string.IsNullOrWhiteSpace(str);
            if (targetType == typeof(Visibility))
                return notEmpty ? Visibility.Visible : Visibility.Collapsed;
            return notEmpty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
