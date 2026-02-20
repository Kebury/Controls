using System;
using System.Windows;
using Controls.Services;

namespace Controls.Views
{
    public partial class OverdueReportDialog : Window
    {
        private readonly NotificationService _notificationService;
        private readonly int _notificationId;

        public bool IsSaved { get; private set; }

        public OverdueReportDialog(NotificationService notificationService, int notificationId)
        {
            InitializeComponent();
            _notificationService = notificationService;
            _notificationId = notificationId;

            OutgoingDatePicker.SelectedDate = DateTime.Today;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var saveButton = sender as System.Windows.Controls.Button;
            if (saveButton != null) saveButton.IsEnabled = false;
            
            try
            {
                if (string.IsNullOrWhiteSpace(OutgoingNumberTextBox.Text))
                {
                    MessageBox.Show("Введите номер исходящего", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    OutgoingNumberTextBox.Focus();
                    return;
                }

                if (!OutgoingDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Выберите дату исходящего", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    OutgoingDatePicker.Focus();
                    return;
                }

                await _notificationService.MarkReportSentAsync(
                    _notificationId,
                    OutgoingNumberTextBox.Text.Trim(),
                    OutgoingDatePicker.SelectedDate.Value
                );

                IsSaved = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (saveButton != null) saveButton.IsEnabled = true;
                
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
