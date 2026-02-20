using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Controls.Models;
using Controls.ViewModels;

namespace Controls.Views
{
    public partial class DateTasksWindow : Window
    {
        public DateTasksWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);
            
            if (DataContext is DateTasksViewModel viewModel)
            {
                viewModel.RequestClose += () => Close();
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.FrameworkElement element && 
                element.DataContext is ControlTask task &&
                DataContext is DateTasksViewModel viewModel)
            {
                if (viewModel.ViewTaskDetailsCommand.CanExecute(task))
                {
                    viewModel.ViewTaskDetailsCommand.Execute(task);
                }
            }
        }
    }
}
