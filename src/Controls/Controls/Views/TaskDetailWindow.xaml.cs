using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Text.Json;
using Controls.ViewModels;
using Controls.Data;
using Controls.Models;
using Microsoft.EntityFrameworkCore;

namespace Controls.Views
{
    public partial class TaskDetailWindow : Window
    {
        public TaskDetailWindow()
        {
            InitializeComponent();
        }

        protected override void OnContentRendered(System.EventArgs e)
        {
            base.OnContentRendered(e);
            
            if (DataContext is TaskDetailViewModel viewModel)
            {
                viewModel.RequestClose += () => Close();
                viewModel.RequestEdit += () =>
                {
                    using (var dbContext = new ControlsDbContext())
                    {
                        var taskToEdit = dbContext.ControlTasks
                            .Include(t => t.Documents)
                            .FirstOrDefault(t => t.Id == viewModel.Task.Id);

                        if (taskToEdit == null)
                        {
                            MessageBox.Show("Задание не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        var editWindow = new TaskEditWindow
                        {
                            DataContext = new TaskEditViewModel(dbContext, taskToEdit),
                            Owner = this
                        };
                        
                        var editViewModel = (TaskEditViewModel)editWindow.DataContext;
                        editViewModel.RequestClose += (saved) =>
                        {
                            try
                            {
                                editWindow.DialogResult = saved;
                            }
                            catch
                            {
                                editWindow.Close();
                            }
                        };
                        
                        if (editWindow.ShowDialog() == true)
                        {
                            Close();
                        }
                    }
                };

                viewModel.RequestMarkAsCompleted += (success) =>
                {
                    try
                    {
                        using (var dbContext = new ControlsDbContext())
                        {
                            var task = dbContext.ControlTasks.FirstOrDefault(t => t.Id == viewModel.Task.Id);
                            if (task != null)
                            {
                                if (task.IsCyclicTask)
                                {
                                    AddIntermediateResponse(task, System.DateTime.Now, "Отметка об исполнении");
                                    task.Status = "На исполнении";
                                    dbContext.SaveChanges();
                                    
                                    try
                                    {
                                        Services.NotificationService.RaiseTaskUpdated();
                                    }
                                    catch { /* Игнорируем ошибки обновления уведомлений */ }
                                    
                                    MessageBox.Show(
                                        "Промежуточный ответ добавлен. Задание остаётся в списке заданий.",
                                        "Успех",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                                }
                                else
                                {
                                    AddIntermediateResponse(task, System.DateTime.Now, "Отметка об исполнении (задание завершено)");
                                    task.Status = "Исполнено";
                                    task.CompletedDate = System.DateTime.Now;
                                    dbContext.SaveChanges();
                                    
                                    try
                                    {
                                        Services.NotificationService.RaiseTaskUpdated();
                                    }
                                    catch { /* Игнорируем ошибки обновления уведомлений */ }
                                    
                                    MessageBox.Show(
                                        "Задание отмечено как исполненное и перемещено в архив.",
                                        "Успех",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Information);
                                }
                                
                                Close();
                            }
                        }
                    }
                    catch (System.Exception ex)
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

        private void AddIntermediateResponse(ControlTask task, System.DateTime responseDate, string actionType = "")
        {
            try
            {
                var originalDueDate = task.DueDate;
                
                List<IntermediateResponse> responses;
                
                if (!string.IsNullOrWhiteSpace(task.IntermediateResponsesJson))
                {
                    responses = JsonSerializer.Deserialize<List<IntermediateResponse>>(task.IntermediateResponsesJson) 
                        ?? new List<IntermediateResponse>();
                }
                else
                {
                    responses = new List<IntermediateResponse>();
                }
                
                responses.Add(new IntermediateResponse
                {
                    Date = responseDate,
                    ActionType = actionType,
                    OriginalDueDate = originalDueDate
                });
                
                task.IntermediateResponsesJson = JsonSerializer.Serialize(responses);
                
                task.DueDate = task.CalculateNextDueDate();
            }
            catch
            {
            }
        }
    }
}
