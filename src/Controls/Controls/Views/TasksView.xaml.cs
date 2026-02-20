using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Controls.ViewModels;
using Controls.Models;

namespace Controls.Views
{
    public partial class TasksView : UserControl
    {
        public TasksView()
        {
            InitializeComponent();
        }

        private void AssigneeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selected = comboBox?.SelectedItem as string;
            
            if (DataContext is TasksViewModel vm && selected != null)
            {
                vm.SelectedAssignee = selected;
            }
        }

        private void AssigneeComboBox_DropDownOpened(object sender, System.EventArgs e)
        {
        }

        private void AssigneeComboBox_DropDownClosed(object sender, System.EventArgs e)
        {
        }

        private void AssigneeComboBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void TaskTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            var selected = comboBox?.SelectedItem as string;
            
            if (DataContext is TasksViewModel vm && selected != null)
            {
                vm.SelectedTaskType = selected;
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
        /// Обработчик сортировки с тремя состояниями: Ascending -> Descending -> None
        /// </summary>
        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            
            var column = e.Column;
            var direction = column.SortDirection;

            var dataGrid = (DataGrid)sender;
            foreach (var col in dataGrid.Columns)
            {
                if (col != column)
                {
                    col.SortDirection = null;
                }
            }

            var collectionView = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);
            if (collectionView == null) return;

            collectionView.SortDescriptions.Clear();

            if (direction == null)
            {
                column.SortDirection = ListSortDirection.Ascending;
                collectionView.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Ascending));
            }
            else if (direction == ListSortDirection.Ascending)
            {
                column.SortDirection = ListSortDirection.Descending;
                collectionView.SortDescriptions.Add(new SortDescription(column.SortMemberPath, ListSortDirection.Descending));
            }
            else
            {
                column.SortDirection = null;
            }

            collectionView.Refresh();
        }

        /// <summary>
        /// Обработчик двойного клика по строке для открытия редактирования
        /// </summary>
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid?.SelectedItem is ControlTask task && DataContext is TasksViewModel viewModel)
            {
                if (viewModel.EditTaskCommand.CanExecute(task))
                {
                    viewModel.EditTaskCommand.Execute(task);
                }
            }
        }

        /// <summary>
        /// Обработчик открытия контекстного меню для установки правильного DataContext
        /// </summary>
        private void DataGridRow_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var row = sender as DataGridRow;
            if (row?.ContextMenu != null && DataContext is TasksViewModel viewModel)
            {
                row.ContextMenu.DataContext = viewModel;
            }
        }
    }
}
