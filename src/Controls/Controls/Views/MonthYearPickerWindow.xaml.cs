using System;
using System.Collections.Generic;
using System.Windows;

namespace Controls.Views
{
    public partial class MonthYearPickerWindow : Window
    {
        public DateTime SelectedDate { get; private set; }

        public MonthYearPickerWindow(DateTime initialDate)
        {
            InitializeComponent();
            
            var years = new List<int>();
            for (int year = 2000; year <= 2050; year++)
            {
                years.Add(year);
            }
            YearComboBox.ItemsSource = years;
            YearComboBox.SelectedItem = initialDate.Year;
            
            var months = new List<MonthItem>
            {
                new MonthItem { Number = 1, Name = "Январь" },
                new MonthItem { Number = 2, Name = "Февраль" },
                new MonthItem { Number = 3, Name = "Март" },
                new MonthItem { Number = 4, Name = "Апрель" },
                new MonthItem { Number = 5, Name = "Май" },
                new MonthItem { Number = 6, Name = "Июнь" },
                new MonthItem { Number = 7, Name = "Июль" },
                new MonthItem { Number = 8, Name = "Август" },
                new MonthItem { Number = 9, Name = "Сентябрь" },
                new MonthItem { Number = 10, Name = "Октябрь" },
                new MonthItem { Number = 11, Name = "Ноябрь" },
                new MonthItem { Number = 12, Name = "Декабрь" }
            };
            MonthComboBox.ItemsSource = months;
            MonthComboBox.DisplayMemberPath = "Name";
            MonthComboBox.SelectedValuePath = "Number";
            MonthComboBox.SelectedValue = initialDate.Month;
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            int selectedYear;
            int selectedMonth;
            
            if (YearComboBox.SelectedItem != null)
            {
                selectedYear = (int)YearComboBox.SelectedItem;
            }
            else if (int.TryParse(YearComboBox.Text, out int customYear))
            {
                selectedYear = customYear;
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите или введите корректный год", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (selectedYear < 1900 || selectedYear > 2100)
            {
                MessageBox.Show("Пожалуйста, введите год от 1900 до 2100", "Некорректный год", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (MonthComboBox.SelectedValue != null)
            {
                selectedMonth = (int)MonthComboBox.SelectedValue;
            }
            else
            {
                MessageBox.Show("Пожалуйста, выберите месяц", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            SelectedDate = new DateTime(selectedYear, selectedMonth, 1);
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private class MonthItem
        {
            public int Number { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
