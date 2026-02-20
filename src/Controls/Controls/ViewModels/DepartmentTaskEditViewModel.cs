using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Controls.Data;
using Controls.Models;

namespace Controls.ViewModels
{
    public class DepartmentTaskEditViewModel : ViewModelBase
    {
        private readonly ControlsDbContext _dbContext;
        private readonly DepartmentTask? _existingTask;
        private string _taskNumber = string.Empty;
        private string _department = string.Empty;
        private string _description = string.Empty;
        private DateTime _sentDate = DateTime.Now;
        private DateTime _dueDate = DateTime.Now.AddDays(7).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        private ImportancePriority _importancePriority = ImportancePriority.Стандартная;
        private string _notes = string.Empty;
        private string _source = string.Empty;
        private Executor? _selectedExecutor;
        private int _selectedDepartmentsCount;

        public DepartmentTaskEditViewModel(ControlsDbContext dbContext, DepartmentTask? task)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _existingTask = task;

            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
            AddTaskFileCommand = new RelayCommand(_ => AddTaskFile());
            RemoveTaskFileCommand = new RelayCommand(file => RemoveTaskFile(file as string));
            AddDepartmentFileCommand = new RelayCommand(_ => AddDepartmentFile());
            RemoveDepartmentFileCommand = new RelayCommand(file => RemoveDepartmentFile(file as string));

            LoadDepartments();
            LoadExecutors();

            if (_existingTask != null)
            {
                TaskNumber = _existingTask.TaskNumber;
                Department = _existingTask.Department;
                Description = _existingTask.Description;
                SentDate = _existingTask.SentDate;
                DueDate = _existingTask.DueDate;
                ImportancePriority = _existingTask.ImportancePriority;
                Notes = _existingTask.Notes;
                Source = _existingTask.Source;
                
                if (_existingTask.ExecutorId.HasValue)
                {
                    SelectedExecutor = Executors.FirstOrDefault(e => e.Id == _existingTask.ExecutorId.Value);
                }

                var existingTaskDepartments = _dbContext.DepartmentTaskDepartments
                    .Include(td => td.Department)
                    .Where(td => td.DepartmentTaskId == _existingTask.Id)
                    .ToList();

                foreach (var td in existingTaskDepartments)
                {
                    var item = DepartmentSelections.FirstOrDefault(d => d.Department.Id == td.DepartmentId);
                    if (item != null)
                    {
                        item.IsSelected = true;
                        item.IsCompleted = td.IsCompleted;
                        item.DepartmentTaskDepartmentId = td.Id;
                    }
                }

                UpdateSelectedDepartmentsCount();

                foreach (var file in _existingTask.GetTaskFiles())
                {
                    TaskFiles.Add(file);
                }
                foreach (var file in _existingTask.GetDepartmentFiles())
                {
                    DepartmentFiles.Add(file);
                }
            }
        }

        public ObservableCollection<string> TaskFiles { get; } = new();
        public ObservableCollection<string> DepartmentFiles { get; } = new();
        public ObservableCollection<string> Departments { get; } = new();
        public ObservableCollection<Executor> Executors { get; } = new();
        public ObservableCollection<DepartmentSelectionItem> DepartmentSelections { get; } = new();

        public string TaskNumber
        {
            get => _taskNumber;
            set
            {
                SetProperty(ref _taskNumber, value);
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        public string Department
        {
            get => _department;
            set
            {
                SetProperty(ref _department, value);
                ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            }
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime SentDate
        {
            get => _sentDate;
            set => SetProperty(ref _sentDate, value);
        }

        public DateTime DueDate
        {
            get => _dueDate;
            set
            {
                var newValue = value;
                if (value.TimeOfDay == TimeSpan.Zero)
                {
                    newValue = value.Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                }
                if (SetProperty(ref _dueDate, newValue))
                {
                    OnPropertyChanged(nameof(DueTimeHour));
                    OnPropertyChanged(nameof(DueTimeMinute));
                }
            }
        }

        public int DueTimeHour
        {
            get => _dueDate.Hour;
            set
            {
                var validValue = Math.Max(0, Math.Min(23, value));
                var newDate = _dueDate.Date.AddHours(validValue).AddMinutes(DueTimeMinute).AddSeconds(59);
                if (SetProperty(ref _dueDate, newDate))
                {
                    OnPropertyChanged(nameof(DueDate));
                }
            }
        }

        public int DueTimeMinute
        {
            get => _dueDate.Minute;
            set
            {
                var validValue = Math.Max(0, Math.Min(59, value));
                var newDate = _dueDate.Date.AddHours(DueTimeHour).AddMinutes(validValue).AddSeconds(59);
                if (SetProperty(ref _dueDate, newDate))
                {
                    OnPropertyChanged(nameof(DueDate));
                }
            }
        }

        public ImportancePriority ImportancePriority
        {
            get => _importancePriority;
            set => SetProperty(ref _importancePriority, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string Source
        {
            get => _source;
            set => SetProperty(ref _source, value);
        }

        public Executor? SelectedExecutor
        {
            get => _selectedExecutor;
            set => SetProperty(ref _selectedExecutor, value);
        }

        public int SelectedDepartmentsCount
        {
            get => _selectedDepartmentsCount;
            set => SetProperty(ref _selectedDepartmentsCount, value);
        }

        public bool IsEditMode => _existingTask != null;
        public bool IsArchived => _existingTask?.IsCompleted ?? false;
        public bool CanEditFields => !IsArchived;
        public string WindowTitle => IsEditMode ? (IsArchived ? "Просмотр задания (Архив)" : "Редактирование задания") : "Новое задание в отдел";

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand AddTaskFileCommand { get; }
        public ICommand RemoveTaskFileCommand { get; }
        public ICommand AddDepartmentFileCommand { get; }
        public ICommand RemoveDepartmentFileCommand { get; }

        public bool DialogResult { get; private set; }
        
        public event Action<bool>? RequestClose;

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(TaskNumber) &&
                   DepartmentSelections.Any(d => d.IsSelected);
        }

        private void Save()
        {
            try
            {
                DepartmentTask task;
                
                if (_existingTask != null)
                {
                    task = _existingTask;
                }
                else
                {
                    var duplicate = _dbContext.DepartmentTasks
                        .FirstOrDefault(t => t.TaskNumber == TaskNumber);
                    
                    if (duplicate != null)
                    {
                        var result = MessageBox.Show(
                            $"Задание с номером '{TaskNumber}' уже существует.\n\nПродолжить сохранение?",
                            "Предупреждение о дубликате",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        
                        if (result == MessageBoxResult.No)
                            return;
                    }
                    
                    task = new DepartmentTask();
                    _dbContext.DepartmentTasks.Add(task);
                }

                task.TaskNumber = TaskNumber;
                var firstSelectedDept = DepartmentSelections.FirstOrDefault(d => d.IsSelected);
                task.Department = firstSelectedDept?.Department.ShortName ?? string.Empty;
                task.Description = Description;
                task.SentDate = SentDate;
                task.DueDate = DueDate;
                task.ImportancePriority = ImportancePriority;
                task.Notes = Notes;
                task.Source = Source;
                task.ExecutorId = SelectedExecutor?.Id;

                task.TaskFilePaths = string.Join(";", TaskFiles);
                task.DepartmentFilePaths = string.Join(";", DepartmentFiles);

                if (_existingTask != null)
                {
                    _dbContext.Entry(task).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
                }

                _dbContext.SaveChanges();

                var existingLinks = _dbContext.DepartmentTaskDepartments
                    .Where(td => td.DepartmentTaskId == task.Id)
                    .ToList();
                _dbContext.DepartmentTaskDepartments.RemoveRange(existingLinks);

                foreach (var selection in DepartmentSelections.Where(d => d.IsSelected))
                {
                    var link = new DepartmentTaskDepartment
                    {
                        DepartmentTaskId = task.Id,
                        DepartmentId = selection.Department.Id,
                        IsCompleted = selection.IsCompleted,
                        CompletedDate = selection.IsCompleted ? DateTime.Now : null
                    };
                    _dbContext.DepartmentTaskDepartments.Add(link);
                }

                _dbContext.SaveChanges();

                MessageBox.Show(
                    "Задание успешно сохранено.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                    
                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при сохранении задания: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        private void AddTaskFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл задания",
                Filter = "Все файлы (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    if (!TaskFiles.Contains(fileName))
                    {
                        TaskFiles.Add(fileName);
                    }
                }
            }
        }

        private void RemoveTaskFile(string? file)
        {
            if (file != null && TaskFiles.Contains(file))
            {
                TaskFiles.Remove(file);
            }
        }

        private void AddDepartmentFile()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл из отдела",
                Filter = "Все файлы (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var fileName in dialog.FileNames)
                {
                    if (!DepartmentFiles.Contains(fileName))
                    {
                        DepartmentFiles.Add(fileName);
                    }
                }
            }
        }

        private void RemoveDepartmentFile(string? file)
        {
            if (file != null && DepartmentFiles.Contains(file))
            {
                DepartmentFiles.Remove(file);
            }
        }

        private void LoadDepartments()
        {
            try
            {
                var departments = _dbContext!.Departments
                    .OrderBy(d => d.ShortName)
                    .ToList();

                Departments.Clear();
                DepartmentSelections.Clear();
                
                foreach (var dept in departments)
                {
                    Departments.Add(dept.ShortName);
                    
                    var selectionItem = new DepartmentSelectionItem
                    {
                        Department = dept,
                        IsSelected = false,
                        IsCompleted = false
                    };
                    
                    selectionItem.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(DepartmentSelectionItem.IsSelected))
                        {
                            UpdateSelectedDepartmentsCount();
                            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
                        }
                    };
                    
                    DepartmentSelections.Add(selectionItem);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка отделов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExecutors()
        {
            try
            {
                var executors = _dbContext!.Executors
                    .OrderBy(e => e.FullName)
                    .ToList();

                Executors.Clear();
                foreach (var executor in executors)
                {
                    Executors.Add(executor);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке списка исполнителей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateSelectedDepartmentsCount()
        {
            SelectedDepartmentsCount = DepartmentSelections.Count(d => d.IsSelected);
        }
    }
}
