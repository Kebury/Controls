using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Controls.Helpers
{
    /// <summary>
    /// Конвертер для скрытия элемента, если счетчик больше 0
    /// </summary>
    public class InverseCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count > 0)
            {
                return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
