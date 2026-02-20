using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using Controls.ViewModels;
using Controls.Models;

namespace Controls.Views
{
    public partial class DepartmentTasksView : UserControl
    {
        public DepartmentTasksView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Обработчик двойного клика на DataGrid для открытия редактирования
        /// </summary>
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem is DepartmentTask task && DataContext is DepartmentTasksViewModel viewModel)
            {
                if (viewModel.EditTaskCommand.CanExecute(task))
                {
                    viewModel.EditTaskCommand.Execute(task);
                }
            }
        }

        /// <summary>
        /// Обработчик клика для снятия фокуса с полей ввода при клике на пустую область
        /// </summary>
        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var source = e.OriginalSource as System.Windows.DependencyObject;
            
            while (source != null && source != this)
            {
                if (source is TextBox || 
                    source is ComboBox || 
                    source is ComboBoxItem || 
                    source is Button ||
                    source is System.Windows.Controls.Primitives.Popup)
                {
                    return;
                }
                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }
            
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                MainGrid.Focus();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        /// <summary>
        /// Обработчик клика по чекбоксу отдела
        /// </summary>
        private void DepartmentCheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is DepartmentTaskDepartment departmentLink)
            {
                if (departmentLink.IsCompleted)
                {
                    e.Handled = true;
                    return;
                }

                e.Handled = true;
                
                if (DataContext is DepartmentTasksViewModel viewModel)
                {
                    if (viewModel.ToggleDepartmentCompletedCommand.CanExecute(departmentLink))
                    {
                        viewModel.ToggleDepartmentCompletedCommand.Execute(departmentLink);
                    }
                }
            }
        }
    }
}
