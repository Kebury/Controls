using System;
using System.IO;
using System.Linq;
using System.Windows;
using Controls.ViewModels;
using Controls.Data;
using Microsoft.EntityFrameworkCore;

namespace Controls.Views
{
    public partial class DepartmentTaskDetailWindow : Window
    {
        public DepartmentTaskDetailWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);
            
            if (DataContext is DepartmentTaskDetailViewModel viewModel)
            {
                viewModel.RequestEdit += () =>
                {
                    try
                    {
                        
                        if (viewModel.Task == null)
                        {
                            MessageBox.Show("Задание не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        
                        using (var dbContext = new ControlsDbContext())
                        {
                            
                            var taskToEdit = dbContext.DepartmentTasks
                                .Include(t => t.Executor)
                                .FirstOrDefault(t => t.Id == viewModel.Task.Id);
                            
                            if (taskToEdit == null)
                            {
                                MessageBox.Show("Задание не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }
                            
                            var editWindow = new DepartmentTaskEditWindow
                            {
                                Owner = this
                            };
                            var editViewModel = new DepartmentTaskEditViewModel(dbContext, taskToEdit);
                            editWindow.DataContext = editViewModel;
                            
                            editViewModel.RequestClose += (saved) =>
                            {
                                editWindow.DialogResult = saved;
                            };
                            
                            if (editWindow.ShowDialog() == true)
                            {
                                Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                };

                viewModel.RequestMarkAsCompleted += (success) =>
                {
                    try
                    {
                        
                        using (var dbContext = new ControlsDbContext())
                        {
                            var task = dbContext.DepartmentTasks.FirstOrDefault(t => t.Id == viewModel.Task!.Id);
                            if (task != null)
                            {
                                task.IsForcedCompleted = true;
                                task.CompletedDate = DateTime.Now;
                                dbContext.SaveChanges();
                                
                                MessageBox.Show(
                                    "Задание отмечено как исполненное и перемещено в архив.\nОтделы, которые не отметили исполнение, получили статус 'Не исполнено'.",
                                    "Успех",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                                
                                Close();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка при отметке задания как исполненного: {ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                };
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
