using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;
using Controls.Data;
using Controls.Models;
using Microsoft.EntityFrameworkCore;
using Controls.Views;

namespace Controls.ViewModels
{
    public class DepartmentTasksViewModel : ViewModelBase
    {
        private readonly ControlsDbContext _dbContext;
        private string _searchText = string.Empty;
        private string _selectedDepartment = "Все организации";
        private string _currentView = "today";
        private ICollectionView? _tasksView;
        private int _totalTasksCount;
        private int _overdueTasksCount;
        private int _todayTasksCount;
        private bool _isDateDetected = false;
        private string _dateSearchType = "Все даты";

        public DepartmentTasksViewModel(ControlsDbContext dbContext)
        {
            _dbContext = dbContext;

            AddTaskCommand = new RelayCommand(_ => AddNewTask());
            EditTaskCommand = new RelayCommand(task => EditTask(task as DepartmentTask));
            DeleteTaskCommand = new RelayCommand(task => DeleteTask(task as DepartmentTask));
            ViewTaskDetailsCommand = new RelayCommand(task => ViewTaskDetails(task as DepartmentTask));
            ToggleCompletedCommand = new RelayCommand(task => ToggleCompleted(task as DepartmentTask));
            ToggleDepartmentCompletedCommand = new RelayCommand(param => ToggleDepartmentCompleted(param));
            ShowTodayTasksCommand = new RelayCommand(_ => { CurrentView = "today"; });
            ShowOverdueTasksCommand = new RelayCommand(_ => { CurrentView = "overdue"; });
            ShowAllTasksCommand = new RelayCommand(_ => { CurrentView = "all"; });

            LoadTasks();
        }

        public ObservableCollection<DepartmentTask> Tasks { get; } = new();
        public ObservableCollection<string> Departments { get; } = new() { "Все организации" };
        public ObservableCollection<string> DateSearchTypes { get; } = new()
        {
            "Все даты",
            "Дата направления",
            "Срок исполнения",
            "Дата завершения"
        };

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    IsDateDetected = DetectDate(value);
                    FilterTasks();
                }
            }
        }

        public bool IsDateDetected
        {
            get => _isDateDetected;
            set => SetProperty(ref _isDateDetected, value);
        }

        public string DateSearchType
        {
            get => _dateSearchType;
            set
            {
                if (SetProperty(ref _dateSearchType, value))
                {
                    FilterTasks();
                }
            }
        }

        public string SelectedDepartment
        {
            get => _selectedDepartment;
            set
            {
                if (SetProperty(ref _selectedDepartment, value))
                {
                    FilterTasks();
                }
            }
        }

        public string CurrentView
        {
            get => _currentView;
            set
            {
                if (SetProperty(ref _currentView, value))
                {
                    LoadTasks();
                    OnPropertyChanged(nameof(ShowToday));
                    OnPropertyChanged(nameof(ShowOverdue));
                    OnPropertyChanged(nameof(ShowAll));
                }
            }
        }

        public bool ShowToday => _currentView == "today";
        public bool ShowOverdue => _currentView == "overdue";
        public bool ShowAll => _currentView == "all";

        public ICommand AddTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ViewTaskDetailsCommand { get; }
        public ICommand ToggleCompletedCommand { get; }
        public ICommand ToggleDepartmentCompletedCommand { get; }
        public ICommand ShowTodayTasksCommand { get; }
        public ICommand ShowOverdueTasksCommand { get; }
        public ICommand ShowAllTasksCommand { get; }

        public int TotalTasksCount
        {
            get => _totalTasksCount;
            set => SetProperty(ref _totalTasksCount, value);
        }

        public int OverdueTasksCount
        {
            get => _overdueTasksCount;
            set => SetProperty(ref _overdueTasksCount, value);
        }

        public int TodayTasksCount
        {
            get => _todayTasksCount;
            set => SetProperty(ref _todayTasksCount, value);
        }

        public int CompletedTasksCount => Tasks.Count(t => t.IsCompleted);
        public int ActiveTasksCount => Tasks.Count(t => !t.IsCompleted);

        private void LoadTasks()
        {
            try
            {
                _dbContext.ChangeTracker.Clear();
                
                var savedDepartment = SelectedDepartment;
                
                Tasks.Clear();
                Departments.Clear();
                Departments.Add("Все организации");

                var allTasks = _dbContext.DepartmentTasks
                    .Include(t => t.Executor)
                    .Include(t => t.TaskDepartments)
                        .ThenInclude(td => td.Department)
                    .ToList();

                var incompleteTasks = allTasks
                    .Where(t => !t.IsCompleted)
                    .OrderByDescending(t => t.SentDate)
                    .ToList();

                var today = DateTime.Today;
                var filteredTasks = _currentView switch
                {
                    "today" => incompleteTasks.Where(t => t.DueDate.Date == today).ToList(),
                    "overdue" => incompleteTasks.Where(t => t.IsOverdue).ToList(),
                    "all" => incompleteTasks,
                    _ => incompleteTasks
                };

                foreach (var task in filteredTasks)
                {
                    Tasks.Add(task);
                }

                var allDepartments = _dbContext.Departments
                    .OrderBy(d => d.ShortName)
                    .Select(d => d.ShortName)
                    .ToList();
                
                foreach (var dept in allDepartments)
                {
                    if (!Departments.Contains(dept))
                    {
                        Departments.Add(dept);
                    }
                }

                if (!string.IsNullOrEmpty(savedDepartment) && Departments.Contains(savedDepartment))
                {
                    _selectedDepartment = savedDepartment;
                }
                else
                {
                    _selectedDepartment = "Все организации";
                }
                OnPropertyChanged(nameof(SelectedDepartment));

                _tasksView = CollectionViewSource.GetDefaultView(Tasks);
                if (_tasksView != null)
                {
                    _tasksView.Filter = null;
                }
                FilterTasks();

                UpdateCounters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заданий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTasks()
        {
            if (_tasksView == null)
            {
                _tasksView = CollectionViewSource.GetDefaultView(Tasks);
            }

            _tasksView.Filter = obj =>
            {
                if (obj is not DepartmentTask task) return false;

                if (SelectedDepartment != "Все организации")
                {
                    var hasSelectedDepartment = task.TaskDepartments.Any(td => 
                        td.Department?.ShortName == SelectedDepartment);
                    
                    if (!hasSelectedDepartment && task.Department != SelectedDepartment)
                        return false;
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    var departmentNames = task.GetDepartmentNames().ToLower();
                    
                    if (IsDateDetected)
                    {
                        bool dateMatch = DateSearchType switch
                        {
                            "Дата направления" => task.SentDate.ToString("dd.MM.yyyy").ToLower().Contains(searchLower) ||
                                                  task.SentDate.ToString("dd.MM.yy").ToLower().Contains(searchLower),
                            "Срок исполнения" => task.DueDate.ToString("dd.MM.yyyy").ToLower().Contains(searchLower) ||
                                                     task.DueDate.ToString("dd.MM.yy").ToLower().Contains(searchLower),
                            "Дата завершения" => task.CompletedDate.HasValue && 
                                                    (task.CompletedDate.Value.ToString("dd.MM.yyyy").ToLower().Contains(searchLower) ||
                                                     task.CompletedDate.Value.ToString("dd.MM.yy").ToLower().Contains(searchLower)),
                            _ =>
                                task.SentDate.ToString("dd.MM.yyyy").ToLower().Contains(searchLower) ||
                                task.SentDate.ToString("dd.MM.yy").ToLower().Contains(searchLower) ||
                                task.DueDate.ToString("dd.MM.yyyy").ToLower().Contains(searchLower) ||
                                task.DueDate.ToString("dd.MM.yy").ToLower().Contains(searchLower) ||
                                (task.CompletedDate.HasValue && 
                                 (task.CompletedDate.Value.ToString("dd.MM.yyyy").ToLower().Contains(searchLower) ||
                                  task.CompletedDate.Value.ToString("dd.MM.yy").ToLower().Contains(searchLower)))
                        };
                        
                        return dateMatch;
                    }
                    else
                    {
                        var textMatch = task.TaskNumber.ToLower().Contains(searchLower) ||
                                        departmentNames.Contains(searchLower) ||
                                        task.Description.ToLower().Contains(searchLower);
                        
                        return textMatch;
                    }
                }

                return true;
            };

            _tasksView.Refresh();
        }

        private void AddNewTask()
        {
            using (var editDbContext = new Data.ControlsDbContext())
            {
                var window = new DepartmentTaskEditWindow();
                var viewModel = new DepartmentTaskEditViewModel(editDbContext, null);
                window.DataContext = viewModel;
                window.Owner = Application.Current.MainWindow;

                viewModel.RequestClose += (saved) =>
                {
                    window.DialogResult = saved;
                };

                if (window.ShowDialog() == true)
                {
                    LoadTasks();
                }
            }
        }

        private void EditTask(DepartmentTask? task)
        {
            if (task == null) return;

            using (var editDbContext = new Data.ControlsDbContext())
            {
                var taskToEdit = editDbContext.DepartmentTasks
                    .Include(t => t.Executor)
                    .Include(t => t.TaskDepartments)
                        .ThenInclude(td => td.Department)
                    .FirstOrDefault(t => t.Id == task.Id);

                if (taskToEdit == null)
                {
                    MessageBox.Show("Задание не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var editWindow = new DepartmentTaskEditWindow
                {
                    DataContext = new DepartmentTaskEditViewModel(editDbContext, taskToEdit),
                    Owner = Application.Current.MainWindow
                };

                var viewModel = (DepartmentTaskEditViewModel)editWindow.DataContext;
                viewModel.RequestClose += (saved) =>
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
                    LoadTasks();
                }
            }
        }

        private void DeleteTask(DepartmentTask? task)
        {
            if (task == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить задание '{task.TaskNumber}'?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.DepartmentTasks.Remove(task);
                    _dbContext.SaveChanges();
                    Tasks.Remove(task);
                    UpdateCounters();

                    MessageBox.Show("Задание успешно удалено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении задания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewTaskDetails(DepartmentTask? task)
        {
            if (task == null) return;

            var taskWithDetails = _dbContext.DepartmentTasks
                .Include(t => t.Executor)
                .Include(t => t.TaskDepartments)
                    .ThenInclude(td => td.Department)
                .FirstOrDefault(t => t.Id == task.Id);
            
            if (taskWithDetails == null) return;

            var window = new DepartmentTaskDetailWindow();
            var viewModel = new DepartmentTaskDetailViewModel(taskWithDetails);
            viewModel.RequestClose += () => window.Close();
            window.DataContext = viewModel;
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
            
            LoadTasks();
        }

        private void ToggleCompleted(DepartmentTask? task)
        {
            if (task == null) return;

            if (task.IsCompleted) return;

            var result = MessageBox.Show(
                $"Отметить задание '{task.TaskNumber}' как исполненное?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                task.IsCompleted = true;
                task.CompletedDate = DateTime.Now;

                _dbContext.DepartmentTasks.Update(task);
                _dbContext.SaveChanges();

                LoadTasks();
                
                MessageBox.Show("Задание отмечено как исполненное.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateCounters()
        {
            var allTasks = _dbContext.DepartmentTasks.ToList();
            var today = DateTime.Today;
            
            TotalTasksCount = allTasks.Count(t => !t.IsCompleted);
            OverdueTasksCount = allTasks.Count(t => !t.IsCompleted && t.IsOverdue);
            TodayTasksCount = allTasks.Count(t => !t.IsCompleted && t.DueDate.Date == today);
            
            OnPropertyChanged(nameof(CompletedTasksCount));
            OnPropertyChanged(nameof(ActiveTasksCount));
        }

        private void ToggleDepartmentCompleted(object? param)
        {
            if (param is not DepartmentTaskDepartment departmentLink)
            {
                return;
            }

            if (departmentLink.IsCompleted)
            {
                return;
            }
            
            var result = MessageBox.Show(
                "Загрузить документ?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var task = _dbContext.DepartmentTasks
                    .Include(t => t.Executor)
                    .Include(t => t.TaskDepartments)
                        .ThenInclude(td => td.Department)
                    .FirstOrDefault(t => t.Id == departmentLink.DepartmentTaskId);

                if (task != null)
                {
                    var editWindow = new DepartmentTaskEditWindow
                    {
                        DataContext = new DepartmentTaskEditViewModel(_dbContext, task),
                        Owner = Application.Current.MainWindow
                    };

                    var viewModel = (DepartmentTaskEditViewModel)editWindow.DataContext;
                    viewModel.RequestClose += (saved) =>
                    {
                        editWindow.DialogResult = saved;
                    };

                    if (editWindow.ShowDialog() == true)
                    {
                        var freshDepartmentLink = _dbContext.DepartmentTaskDepartments
                            .FirstOrDefault(td => td.Id == departmentLink.Id);
                        
                        if (freshDepartmentLink != null)
                        {
                            freshDepartmentLink.IsCompleted = true;
                            freshDepartmentLink.CompletedDate = DateTime.Now;
                            
                            _dbContext.SaveChanges();

                            var taskDepartments = _dbContext.DepartmentTaskDepartments
                                .Where(td => td.DepartmentTaskId == freshDepartmentLink.DepartmentTaskId)
                                .ToList();

                            if (taskDepartments.All(td => td.IsCompleted))
                            {
                                MessageBox.Show(
                                    "Все организации исполнили задание. Задание перемещено в архив.",
                                    "Информация",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }

                            LoadTasks();
                        }
                    }
                }
            }
            else
            {
                departmentLink.IsCompleted = true;
                departmentLink.CompletedDate = DateTime.Now;
                
                _dbContext.SaveChanges();

                var taskDepartments = _dbContext.DepartmentTaskDepartments
                    .Where(td => td.DepartmentTaskId == departmentLink.DepartmentTaskId)
                    .ToList();

                if (taskDepartments.All(td => td.IsCompleted))
                {
                    MessageBox.Show(
                        "Все организации исполнили задание. Задание перемещено в архив.",
                        "Информация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                LoadTasks();
            }
        }

        /// <summary>
        /// Определяет, содержит ли текст дату (форматы: dd.MM.yyyy, dd.MM.yy, dd.MM, dd/MM и т.д.)
        /// </summary>
        private bool DetectDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            var datePatterns = new[]
            {
                @"\d{1,2}\.\d{1,2}\.\d{2,4}",
                @"\d{1,2}\.\d{1,2}",
                @"\d{1,2}/\d{1,2}/\d{2,4}",
                @"\d{1,2}/\d{1,2}",
                @"\d{1,2}-\d{1,2}-\d{2,4}",
                @"\d{1,2}-\d{1,2}"
            };
            
            foreach (var pattern in datePatterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(text, pattern))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
