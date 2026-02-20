using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Controls.Models;
using Microsoft.EntityFrameworkCore;

namespace Controls.ViewModels
{
    public class DateTasksViewModel : ViewModelBase
    {
        private readonly DateTime _date;
        private readonly List<ControlTask> _tasks;

        public DateTasksViewModel(DateTime date, List<ControlTask> tasks)
        {
            _date = date;
            _tasks = new List<ControlTask>(tasks ?? new List<ControlTask>());

            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
            ViewTaskDetailsCommand = new RelayCommand(task => ViewTaskDetails(task as ControlTask));
        }

        public DateTime Date => _date;
        public List<ControlTask> Tasks => _tasks;
        public string DateDisplay => _date.ToString("D");
        public int TaskCount => _tasks.Count;

        public ICommand CloseCommand { get; }
        public ICommand ViewTaskDetailsCommand { get; }

        public event Action? RequestClose;

        private void ViewTaskDetails(ControlTask? task)
        {
            if (task == null) return;

            using (var dbContext = new Data.ControlsDbContext())
            {
                var freshTask = dbContext.ControlTasks
                    .Include(t => t.Documents)
                    .FirstOrDefault(t => t.Id == task.Id);

                if (freshTask == null) return;

                var detailWindow = new Views.TaskDetailWindow();
                var viewModel = new TaskDetailViewModel(freshTask);
                viewModel.RequestClose += () => detailWindow.Close();
                detailWindow.DataContext = viewModel;
                detailWindow.Owner = Application.Current.MainWindow;
                detailWindow.ShowDialog();
            }
        }
    }
}
