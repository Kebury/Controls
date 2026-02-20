using System;
using System.Windows;
using System.Windows.Controls;
using Controls.Models;
using Controls.Services;
using Controls.Data;

namespace Controls.Views
{
    /// <summary>
    /// Диалог для ввода данных исходящего донесения
    /// </summary>
    public partial class OutgoingReportDialog : Window
    {
        private readonly int _notificationId;
        private readonly NotificationService _notificationService;
        private readonly bool _showDatePicker;

        public OutgoingReportDialog(NotificationService notificationService, int notificationId, bool showDatePicker = false)
        {
            InitializeComponent();
            _notificationService = notificationService;
            _notificationId = notificationId;
            _showDatePicker = showDatePicker;

            if (_showDatePicker)
            {
                DatePanel.Visibility = Visibility.Visible;
                OutgoingDatePicker.SelectedDate = DateTime.Today;
            }
            else
            {
                DatePanel.Visibility = Visibility.Collapsed;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            
            try
            {
                if (string.IsNullOrWhiteSpace(OutgoingNumberTextBox.Text))
                {
                    MessageBox.Show("Введите номер исходящего", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    OutgoingNumberTextBox.Focus();
                    return;
                }

                if (_showDatePicker)
                {
                    if (!OutgoingDatePicker.SelectedDate.HasValue)
                    {
                        MessageBox.Show("Выберите дату исходящего", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        OutgoingDatePicker.Focus();
                        return;
                    }
                }

                var outgoingDate = _showDatePicker && OutgoingDatePicker.SelectedDate.HasValue
                    ? OutgoingDatePicker.SelectedDate.Value
                    : DateTime.Today;

                await _notificationService.MarkReportSentAsync(
                    _notificationId,
                    OutgoingNumberTextBox.Text.Trim(),
                    outgoingDate);

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SaveButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
                
                if (DialogResult == true)
                {
                    Close();
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
