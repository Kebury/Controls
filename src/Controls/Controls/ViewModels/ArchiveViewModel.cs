using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Data;
using Controls.Data;
using Controls.Models;
using Controls.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Controls.ViewModels
{
    /// <summary>
    /// ViewModel для вкладки "Архив" - отображение всех завершённых заданий
    /// </summary>
    public class ArchiveViewModel : ViewModelBase
    {
        private readonly ControlsDbContext _dbContext;
        private string _searchText = string.Empty;
        private ControlTask? _selectedTask;
        private string _selectedAssignee = "Все исполнители";
        private string _currentTaskType = "control";
        private ICollectionView? _tasksView;
        private bool _isDateDetected = false;
        private string _dateSearchType = "Все даты";

        public ArchiveViewModel(ControlsDbContext dbContext)
        {
            _dbContext = dbContext;
            
            ViewTaskDetailsCommand = new RelayCommand(task => ViewTaskDetails(task as ControlTask));
            EditTaskCommand = new RelayCommand(task => EditTask(task as ControlTask));
            ViewDepartmentTaskDetailsCommand = new RelayCommand(task => ViewDepartmentTaskDetails(task as DepartmentTask));
            ShowControlTasksCommand = new RelayCommand(_ => { CurrentTaskType = "control"; });
            ShowDepartmentTasksCommand = new RelayCommand(_ => { CurrentTaskType = "department"; });
            
            LoadCompletedTasks();
        }

        public ObservableCollection<ControlTask> Tasks { get; } = new();
        public ObservableCollection<DepartmentTask> DepartmentTasks { get; } = new();
        public ObservableCollection<string> Assignees { get; } = new() { "Все исполнители" };
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

        public ControlTask? SelectedTask
        {
            get => _selectedTask;
            set => SetProperty(ref _selectedTask, value);
        }

        public string CurrentTaskType
        {
            get => _currentTaskType;
            set
            {
                if (SetProperty(ref _currentTaskType, value))
                {
                    LoadCompletedTasks();
                    OnPropertyChanged(nameof(ShowControlTasks));
                    OnPropertyChanged(nameof(ShowDepartmentTasks));
                }
            }
        }

        public bool ShowControlTasks => _currentTaskType == "control";
        public bool ShowDepartmentTasks => _currentTaskType == "department";

        public ICommand ViewTaskDetailsCommand { get; }
        public ICommand EditTaskCommand { get; }
        public ICommand ViewDepartmentTaskDetailsCommand { get; }
        public ICommand ShowControlTasksCommand { get; }
        public ICommand ShowDepartmentTasksCommand { get; }

        /// <summary>
        /// Обновить список заданий в архиве
        /// </summary>
        public void Refresh()
        {
            LoadCompletedTasks();
        }

        /// <summary>
        /// Загрузка всех завершённых заданий
        /// </summary>
        private void LoadCompletedTasks()
        {
            try
            {
                _dbContext.ChangeTracker.Clear();
                
                var savedAssignee = SelectedAssignee;
                
                Tasks.Clear();
                DepartmentTasks.Clear();
                Assignees.Clear();
                Assignees.Add("Все исполнители");
                
                if (_currentTaskType == "control")
                {
                    var completedTasks = _dbContext.ControlTasks
                        .Include(t => t.Documents)
                        .Where(t => t.Status == "Исполнено")
                        .ToList();

                    var executors = _dbContext.Executors.ToList();
                    foreach (var task in completedTasks)
                    {
                        if (!string.IsNullOrWhiteSpace(task.Assignee))
                        {
                            var executor = executors.FirstOrDefault(e => e.ShortName == task.Assignee);
                            if (executor != null)
                            {
                                task.ExecutorInfo = executor.FullInfo;
                            }
                        }
                    }

                    var uniqueAssignees = completedTasks
                        .Where(t => !string.IsNullOrWhiteSpace(t.Assignee))
                        .Select(t => t.Assignee)
                        .Distinct()
                        .OrderBy(a => a);

                    foreach (var assignee in uniqueAssignees)
                    {
                        Assignees.Add(assignee);
                    }

                    foreach (var task in completedTasks)
                    {
                        Tasks.Add(task);
                    }
                }
                else
                {
                    var allDepartmentTasks = _dbContext.DepartmentTasks
                        .Include(t => t.Executor)
                        .Include(t => t.TaskDepartments)
                            .ThenInclude(td => td.Department)
                        .ToList();

                    var completedDepartmentTasks = allDepartmentTasks
                        .Where(t => t.IsCompleted)
                        .OrderByDescending(t => t.CompletedDate)
                        .ToList();

                    foreach (var task in completedDepartmentTasks)
                    {
                        DepartmentTasks.Add(task);
                    }
                }

                if (!string.IsNullOrEmpty(savedAssignee) && Assignees.Contains(savedAssignee))
                {
                    _selectedAssignee = savedAssignee;
                }
                else
                {
                    _selectedAssignee = "Все исполнители";
                }
                OnPropertyChanged(nameof(SelectedAssignee));

                _tasksView = CollectionViewSource.GetDefaultView(Tasks);
                if (_tasksView != null)
                {
                    _tasksView.Filter = null;
                }
                FilterTasks();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки архива: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Фильтрация заданий по поисковому запросу и исполнителю
        /// </summary>
        private void FilterTasks()
        {
            if (_tasksView == null) return;

            _tasksView.Filter = obj =>
            {
                if (obj is not ControlTask task) return false;

                if (SelectedAssignee != "Все исполнители" && task.Assignee != SelectedAssignee)
                {
                    return false;
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

        /// <summary>
        /// Просмотр деталей задания
        /// </summary>
        private void ViewTaskDetails(ControlTask? task)
        {
            if (task == null) return;

            try
            {
                var fullTask = _dbContext.ControlTasks
                    .Include(t => t.Documents)
                    .FirstOrDefault(t => t.Id == task.Id);

                if (fullTask != null)
                {
                    var detailWindow = new Views.TaskDetailWindow();
                    var detailViewModel = new TaskDetailViewModel(fullTask);
                    detailViewModel.RequestClose += () => detailWindow.Close();
                    detailWindow.DataContext = detailViewModel;
                    detailWindow.Owner = Application.Current.MainWindow;
                    detailWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия деталей задания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Просмотр деталей задания в отдел
        /// </summary>
        private void ViewDepartmentTaskDetails(DepartmentTask? task)
        {
            if (task == null) return;

            try
            {
                var fullTask = _dbContext.DepartmentTasks
                    .Include(t => t.Executor)
                    .Include(t => t.TaskDepartments)
                        .ThenInclude(td => td.Department)
                    .FirstOrDefault(t => t.Id == task.Id);

                if (fullTask != null)
                {
                    var detailWindow = new Views.DepartmentTaskDetailWindow();
                    var detailViewModel = new DepartmentTaskDetailViewModel(fullTask);
                    detailViewModel.RequestClose += () => detailWindow.Close();
                    detailWindow.DataContext = detailViewModel;
                    detailWindow.Owner = Application.Current.MainWindow;
                    detailWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия деталей задания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Редактирование задания из архива
        /// </summary>
        private void EditTask(ControlTask? task)
        {
            if (task == null) return;

            try
            {
                using (var editDbContext = new Data.ControlsDbContext())
                {
                    var fullTask = editDbContext.ControlTasks
                        .Include(t => t.Documents)
                        .FirstOrDefault(t => t.Id == task.Id);

                    if (fullTask != null)
                    {
                        var editWindow = new Views.TaskEditWindow
                        {
                            DataContext = new TaskEditViewModel(editDbContext, fullTask),
                            Owner = Application.Current.MainWindow
                        };

                        var viewModel = (TaskEditViewModel)editWindow.DataContext;
                        viewModel.RequestClose += (saved) =>
                        {
                            editWindow.DialogResult = saved;
                        };

                        if (editWindow.ShowDialog() == true)
                        {
                            LoadCompletedTasks();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка редактирования задания: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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
