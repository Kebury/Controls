using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;
using Controls.Data;
using Controls.Models;
using Controls.Services;
using Microsoft.EntityFrameworkCore;

namespace Controls.ViewModels
{
    public class TasksViewModel : ViewModelBase
    {
        private readonly ControlsDbContext _dbContext;
        private string _searchText = string.Empty;
        private string _selectedAssignee = "Все исполнители";
        private string _selectedTaskType = "Все типы";
        private string _currentView = "today";
        private ICollectionView? _tasksView;
        private bool _isDateDetected = false;
        private string _dateSearchType = "Все даты";

        public TasksViewModel(ControlsDbContext dbContext)
        {
            _dbContext = dbContext;

            AddTaskCommand = new RelayCommand(_ => AddNewTask());
            EditTaskCommand = new RelayCommand(task => EditTask(task as ControlTask));
            DeleteTaskCommand = new RelayCommand(task => DeleteTask(task as ControlTask));
            ViewTaskDetailsCommand = new RelayCommand(task => ViewTaskDetails(task as ControlTask));
            ShowAllTasksCommand = new RelayCommand(_ => { CurrentView = "all"; });
            ShowTodayTasksCommand = new RelayCommand(_ => { CurrentView = "today"; });
            ShowOverdueTasksCommand = new RelayCommand(_ => { CurrentView = "overdue"; });
            
            NotificationService.TaskUpdated += OnTaskUpdated;
            
            LoadTasks();
        }

        /// <summary>
        /// Освобождение ресурсов (отписка от событий)
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NotificationService.TaskUpdated -= OnTaskUpdated;
            }
            
            base.Dispose(disposing);
        }

        public ObservableCollection<ControlTask> Tasks { get; } = new();
        public ObservableCollection<string> Assignees { get; } = new();
        public ObservableCollection<string> TaskTypes { get; } = new() 
        { 
            "Все типы",
            "Разовые",
            "Ежедневные",
            "Еженедельные",
            "Ежемесячные",
            "Ежеквартальные",
            "Полугодовые",
            "Годовые",
            "Обращение",
            "Запрос"
        };
        public ObservableCollection<string> DateSearchTypes { get; } = new()
        {
            "Все даты",
            "Дата создания",
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

        public string SelectedAssignee
        {
            get => _selectedAssignee;
            set
            {
                if (SetProperty(ref _selectedAssignee, value))
                {
                    FilterTasks();
                }
            }
        }

        public string SelectedTaskType
        {
            get => _selectedTaskType;
            set
            {
                if (SetProperty(ref _selectedTaskType, value))
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
                    OnPropertyChanged(nameof(ShowOnlyToday));
                    OnPropertyChanged(nameof(ShowAllTasks));
                    OnPropertyChanged(nameof(ShowOverdue));
                }
            }
        }

        public bool ShowOnlyToday => CurrentView == "today";
        public bool ShowAllTasks => CurrentView == "all";
        public bool ShowOverdue => CurrentView == "overdue";

        public int TodayTasksCount { get; private set; }
        public int AllTasksCount { get; private set; }
        public int OverdueTasksCount { get; private set; }

        public bool HasTasks => Tasks.Any();

        public ICommand AddTaskCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand ViewTaskDetailsCommand { get; }
        public ICommand ShowAllTasksCommand { get; }
        public ICommand ShowTodayTasksCommand { get; }
        public ICommand ShowOverdueTasksCommand { get; }

        private void LoadTasks()
        {
            try
            {
                _dbContext.ChangeTracker.Clear();
                
                var savedAssignee = SelectedAssignee;
                var savedTaskType = SelectedTaskType;
                
                Tasks.Clear();

                var allTasks = _dbContext.ControlTasks
                    .Include(t => t.Documents)
                    .Where(t => t.Status != "Исполнено")
                    .ToList();

                var executors = _dbContext.Executors.ToList();
                foreach (var task in allTasks)
                {
                    if (!string.IsNullOrWhiteSpace(task.Assignee))
                    {
                        var executor = executors.FirstOrDefault(e => e.FullName == task.Assignee);
                        if (executor != null)
                        {
                            task.ExecutorInfo = executor.FullInfo;
                        }
                    }
                }

                var today = DateTime.Today;
                
                TodayTasksCount = allTasks.Count(t => t.DueDate.Date == today);
                AllTasksCount = allTasks.Count;
                OverdueTasksCount = allTasks.Count(t => t.DueDate < DateTime.Now && t.Status != "Исполнено");

                OnPropertyChanged(nameof(TodayTasksCount));
                OnPropertyChanged(nameof(AllTasksCount));
                OnPropertyChanged(nameof(OverdueTasksCount));

                var uniqueAssignees = allTasks
                    .Where(t => !string.IsNullOrWhiteSpace(t.Assignee))
                    .Select(t => t.Assignee)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                if (!Assignees.Contains("Все исполнители"))
                {
                    Assignees.Insert(0, "Все исполнители");
                }

                for (int i = Assignees.Count - 1; i >= 0; i--)
                {
                    if (Assignees[i] != "Все исполнители" && !uniqueAssignees.Contains(Assignees[i]))
                    {
                        Assignees.RemoveAt(i);
                    }
                }

                foreach (var assignee in uniqueAssignees)
                {
                    if (!Assignees.Contains(assignee))
                    {
                        Assignees.Add(assignee);
                    }
                }

                if (!string.IsNullOrEmpty(savedAssignee) && Assignees.Contains(savedAssignee))
                {
                    if (_selectedAssignee != savedAssignee)
                    {
                        _selectedAssignee = savedAssignee;
                        OnPropertyChanged(nameof(SelectedAssignee));
                    }
                }
                else
                {
                    if (_selectedAssignee != "Все исполнители")
                    {
                        _selectedAssignee = "Все исполнители";
                        OnPropertyChanged(nameof(SelectedAssignee));
                    }
                }

                if (!string.IsNullOrEmpty(savedTaskType) && TaskTypes.Contains(savedTaskType))
                {
                    if (_selectedTaskType != savedTaskType)
                    {
                        _selectedTaskType = savedTaskType;
                        OnPropertyChanged(nameof(SelectedTaskType));
                    }
                }
                else
                {
                    if (_selectedTaskType != "Все типы")
                    {
                        _selectedTaskType = "Все типы";
                        OnPropertyChanged(nameof(SelectedTaskType));
                    }
                }

                IEnumerable<ControlTask> filteredTasks = CurrentView switch
                {
                    "today" => allTasks.Where(t => t.DueDate.Date == today),
                    "overdue" => allTasks.Where(t => t.DueDate < DateTime.Now),
                    _ => allTasks
                };

                foreach (var task in filteredTasks.OrderBy(t => t.DueDate))
                {
                    Tasks.Add(task);
                }

                _tasksView = CollectionViewSource.GetDefaultView(Tasks);
                if (_tasksView != null)
                {
                    _tasksView.Filter = null;
                }
                FilterTasks();
                
                OnPropertyChanged(nameof(HasTasks));
                OnPropertyChanged(nameof(Tasks));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки задач: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterTasks()
        {
            if (_tasksView == null)
            {
                _tasksView = CollectionViewSource.GetDefaultView(Tasks);
            }

            if (_tasksView == null) return;

            _tasksView.Filter = obj =>
            {
                if (obj is not ControlTask task) return false;

                if (!string.IsNullOrEmpty(SelectedAssignee) && 
                    SelectedAssignee != "Все исполнители" && 
                    task.Assignee != SelectedAssignee)
                {
                    return false;
                }

                if (!string.IsNullOrEmpty(SelectedTaskType) && 
                    SelectedTaskType != "Все типы")
                {
                    var taskTypeStr = task.TaskType.ToString();
                    bool typeMatches = SelectedTaskType switch
                    {
                        "Разовые" => taskTypeStr == "Разовое",
                        "Ежедневные" => taskTypeStr == "Ежедневное",
                        "Еженедельные" => taskTypeStr == "Еженедельное",
                        "Ежемесячные" => taskTypeStr == "Ежемесячное",
                        "Ежеквартальные" => taskTypeStr == "Ежеквартальное",
                        "Полугодовые" => taskTypeStr == "Полугодовое",
                        "Годовые" => taskTypeStr == "Годовое",
                        "Обращение" => taskTypeStr == "Обращение",
                        "Запрос" => taskTypeStr == "Запрос",
                        _ => false
                    };
                    
                    if (!typeMatches)
                    {
                        return false;
                    }
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    
                    if (IsDateDetected)
                    {
                        bool dateMatch = DateSearchType switch
                        {
                            "Дата создания" => task.CreatedDate.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                               task.CreatedDate.ToString("dd.MM.yy").Contains(searchLower),
                            "Срок исполнения" => task.DueDate.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                                 task.DueDate.ToString("dd.MM.yy").Contains(searchLower),
                            "Дата завершения" => task.CompletedDate.HasValue && 
                                                 (task.CompletedDate.Value.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                                  task.CompletedDate.Value.ToString("dd.MM.yy").Contains(searchLower)),
                            _ =>
                                task.CreatedDate.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                task.CreatedDate.ToString("dd.MM.yy").Contains(searchLower) ||
                                task.DueDate.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                task.DueDate.ToString("dd.MM.yy").Contains(searchLower) ||
                                (task.CompletedDate.HasValue && 
                                 (task.CompletedDate.Value.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                  task.CompletedDate.Value.ToString("dd.MM.yy").Contains(searchLower)))
                        };
                        
                        return dateMatch;
                    }
                    else
                    {
                        var textMatch = task.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                        task.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                        task.Assignee.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                        task.ControlNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                        
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
                var editWindow = new Views.TaskEditWindow
                {
                    DataContext = new TaskEditViewModel(editDbContext),
                    Owner = Application.Current.MainWindow
                };

                var viewModel = (TaskEditViewModel)editWindow.DataContext;
                viewModel.RequestClose += (saved) =>
                {
                    editWindow.DialogResult = saved;
                };

                if (editWindow.ShowDialog() == true)
                {
                    LoadTasks();
                }
            }
        }

        private void EditTask(ControlTask? task)
        {
            if (task == null) return;

            using (var editDbContext = new Data.ControlsDbContext())
            {
                var taskToEdit = editDbContext.ControlTasks
                    .Include(t => t.Documents)
                    .FirstOrDefault(t => t.Id == task.Id);

                if (taskToEdit == null)
                {
                    MessageBox.Show("Задание не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var editWindow = new Views.TaskEditWindow
                {
                    DataContext = new TaskEditViewModel(editDbContext, taskToEdit),
                    Owner = Application.Current.MainWindow
                };

                var viewModel = (TaskEditViewModel)editWindow.DataContext;
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

        private void DeleteTask(ControlTask? task)
        {
            if (task == null) return;
            
            var result = MessageBox.Show(
                $"Удалить задачу '{task.Title}'?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.ControlTasks.Remove(task);
                    _dbContext.SaveChanges();
                    Tasks.Remove(task);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления задачи: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewTaskDetails(ControlTask? task)
        {
            if (task == null) return;

            var freshTask = _dbContext.ControlTasks
                .Include(t => t.Documents)
                .FirstOrDefault(t => t.Id == task.Id);
            
            if (freshTask == null) return;

            var detailWindow = new Views.TaskDetailWindow();
            var viewModel = new TaskDetailViewModel(freshTask);
            viewModel.RequestClose += () => detailWindow.Close();
            detailWindow.DataContext = viewModel;
            detailWindow.Owner = Application.Current.MainWindow;
            detailWindow.ShowDialog();
            
            LoadTasks();
        }
        
        /// <summary>
        /// Обработчик события обновления задания из NotificationService
        /// </summary>
        private void OnTaskUpdated()
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                LoadTasks();
            });
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
