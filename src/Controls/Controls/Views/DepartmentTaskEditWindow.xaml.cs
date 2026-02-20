using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using Controls.ViewModels;

namespace Controls.Views
{
    public partial class DepartmentTaskEditWindow : Window
    {
        public DepartmentTaskEditWindow()
        {
            InitializeComponent();
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextNumeric(e.Text);
        }

        private static bool IsTextNumeric(string text)
        {
            Regex regex = new Regex("[^0-9]+");
            return !regex.IsMatch(text);
        }
    }
}
