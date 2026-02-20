using System.Windows;
using Controls.Models;

namespace Controls.Views
{
    public partial class AddDepartmentWindow : Window
    {
        private readonly Department? _existingDepartment;
        private readonly bool _isEditMode;

        public string FullName => FullNameTextBox.Text.Trim();
        public string ShortName => ShortNameTextBox.Text.Trim();

        public AddDepartmentWindow()
        {
            InitializeComponent();
            _isEditMode = false;
            Title = "Добавить отдел";
            AddButton.Content = "Добавить";
        }

        public AddDepartmentWindow(Department department)
        {
            InitializeComponent();
            _existingDepartment = department;
            _isEditMode = true;
            Title = "Редактировать отдел";
            AddButton.Content = "Сохранить";
            
            FullNameTextBox.Text = department.FullName;
            ShortNameTextBox.Text = department.ShortName;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                MessageBox.Show("Укажите полное наименование отдела", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                FullNameTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(ShortName))
            {
                MessageBox.Show("Укажите укороченное наименование отдела", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                ShortNameTextBox.Focus();
                return;
            }

            if (_isEditMode && _existingDepartment != null)
            {
                _existingDepartment.FullName = FullName;
                _existingDepartment.ShortName = ShortName;
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
