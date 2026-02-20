using System.Windows;

namespace Controls.Views
{
    /// <summary>
    /// Результат выбора действия пользователем
    /// </summary>
    public enum DatabaseUnavailableAction
    {
        WaitForAccess,
        SelectOtherDatabase,
        Exit
    }

    /// <summary>
    /// Диалог, отображаемый при недоступности базы данных
    /// </summary>
    public partial class DatabaseUnavailableDialog : Window
    {
        public DatabaseUnavailableAction SelectedAction { get; private set; }
        public string DatabasePath { get; private set; }

        public DatabaseUnavailableDialog(string databasePath, string errorMessage)
        {
            InitializeComponent();
            DatabasePath = databasePath;
            
            MessageTextBlock.Text = errorMessage;
            
            PathTextBlock.Text = databasePath;
        }

        private void WaitForAccess_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = DatabaseUnavailableAction.WaitForAccess;
            DialogResult = true;
            Close();
        }

        private void SelectOtherDatabase_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = DatabaseUnavailableAction.SelectOtherDatabase;
            DialogResult = true;
            Close();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            SelectedAction = DatabaseUnavailableAction.Exit;
            DialogResult = false;
            Close();
        }
    }
}
