using System.Windows;

namespace Controls.Views
{
    public partial class WorkingOrderDialog : Window
    {
        public enum WorkingOrderResult
        {
            None,
            WithReport,
            WithoutReport
        }

        public WorkingOrderResult Result { get; private set; } = WorkingOrderResult.None;

        public WorkingOrderDialog()
        {
            InitializeComponent();
        }

        private void WithReportButton_Click(object sender, RoutedEventArgs e)
        {
            Result = WorkingOrderResult.WithReport;
            DialogResult = true;
            Close();
        }

        private void WithoutReportButton_Click(object sender, RoutedEventArgs e)
        {
            Result = WorkingOrderResult.WithoutReport;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = WorkingOrderResult.None;
            DialogResult = false;
            Close();
        }
    }
}
