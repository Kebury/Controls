using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using Controls.Models;

namespace Controls.Helpers
{
    /// <summary>
    /// Конвертер для отображения списка названий отделов из DepartmentTask
    /// </summary>
    public class DepartmentNamesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DepartmentTask task)
            {
                return task.GetDepartmentNames();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
