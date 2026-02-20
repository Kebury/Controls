using System.Windows;
using Controls.Models;

namespace Controls.Views
{
    public partial class AddExecutorWindow : Window
    {
        private readonly Executor? _existingExecutor;
        private readonly bool _isEditMode;

        public string FullName => FullNameTextBox.Text.Trim();
        public string Position => PositionTextBox.Text.Trim();

        public AddExecutorWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            Title = "Добавить исполнителя";
            AddButton.Content = "Добавить";
            UpdateShortNamePreview();
        }

        public AddExecutorWindow(Executor executor)
        {
            InitializeComponent();
            _existingExecutor = executor;
            _isEditMode = true;
            Title = "Редактировать исполнителя";
            AddButton.Content = "Сохранить";
            
            FullNameTextBox.Text = executor.FullName;
            PositionTextBox.Text = executor.Position;
            UpdateShortNamePreview();
        }

        private void FullNameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateShortNamePreview();
        }

        private void UpdateShortNamePreview()
        {
            string shortName = GenerateShortName(FullNameTextBox.Text.Trim());
            if (string.IsNullOrEmpty(shortName))
            {
                ShortNamePreview.Text = "Будет отображаться как: ";
            }
            else
            {
                ShortNamePreview.Text = $"Будет отображаться как: {shortName}";
            }
        }

        private string GenerateShortName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;
            
            var parts = fullName.Trim().Split(' ');
            if (parts.Length < 2)
                return fullName;
            
            var lastName = parts[0];
            var initials = string.Empty;
            
            for (int i = 1; i < parts.Length && i < 3; i++)
            {
                if (!string.IsNullOrWhiteSpace(parts[i]))
                {
                    initials += parts[i][0] + ".";
                }
            }
            
            return $"{lastName} {initials}";
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                MessageBox.Show("Пожалуйста, введите ФИО", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(Position))
            {
                MessageBox.Show("Пожалуйста, введите должность", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                PositionTextBox.Focus();
                return;
            }

            if (_isEditMode && _existingExecutor != null)
            {
                _existingExecutor.FullName = FullName;
                _existingExecutor.Position = Position;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
