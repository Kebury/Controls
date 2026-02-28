using System;
using System.Globalization;
using System.Windows.Data;
using Controls.Models;

namespace Controls.Helpers
{
    /// <summary>
    /// Конвертер для отображения человекочитаемых названий типов заданий в UI-элементах
    /// </summary>
    public class TaskTypeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TaskType tt)
                return tt.GetDisplayName();
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
