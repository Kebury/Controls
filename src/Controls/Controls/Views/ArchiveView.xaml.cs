using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Controls.ViewModels;
using Controls.Models;

namespace Controls.Views
{
    public partial class ArchiveView : UserControl
    {
        public ArchiveView()
        {
            InitializeComponent();
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
            if (dataGrid?.SelectedItem is ControlTask task && DataContext is ArchiveViewModel viewModel)
            {
                if (viewModel.EditTaskCommand.CanExecute(task))
                {
                    viewModel.EditTaskCommand.Execute(task);
                }
            }
        }
    }
}
