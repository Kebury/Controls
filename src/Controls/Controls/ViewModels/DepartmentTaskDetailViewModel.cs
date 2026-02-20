using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using Controls.Data;
using Controls.Models;

namespace Controls.ViewModels
{
    public class DepartmentTaskDetailViewModel : ViewModelBase
    {
        private readonly DepartmentTask _task;

        public DepartmentTaskDetailViewModel(DepartmentTask task)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));

            foreach (var file in _task.GetTaskFiles())
            {
                TaskFiles.Add(file);
            }
            foreach (var file in _task.GetDepartmentFiles())
            {
                DepartmentFiles.Add(file);
            }

            if (_task.TaskDepartments != null)
            {
                foreach (var taskDept in _task.TaskDepartments)
                {
                    DepartmentStatuses.Add(new DepartmentStatusInfo
                    {
                        DepartmentName = taskDept.Department?.ShortName ?? "Неизвестный отдел",
                        DepartmentFullName = taskDept.Department?.FullName ?? "Неизвестный отдел",
                        IsCompleted = taskDept.IsCompleted,
                        CompletedDate = taskDept.CompletedDate,
                        StatusText = taskDept.IsCompleted 
                            ? $"Исполнено {(taskDept.CompletedDate.HasValue ? taskDept.CompletedDate.Value.ToString("dd.MM.yyyy HH:mm") : "")}" 
                            : "Не исполнено",
                        StatusColor = taskDept.IsCompleted ? "#4CAF50" : "#F44336"
                    });
                }
            }

            BuildHistory();

            OpenFileCommand = new RelayCommand(file => OpenFile(file as string));
            EditCommand = new RelayCommand(_ => RequestEdit?.Invoke());
            MarkAsCompletedCommand = new RelayCommand(_ => MarkAsCompleted(), _ => !IsCompleted);
            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
        }

        public ObservableCollection<string> TaskFiles { get; } = new();
        public ObservableCollection<string> DepartmentFiles { get; } = new();
        public ObservableCollection<TaskHistoryEntry> History { get; } = new();
        public ObservableCollection<DepartmentStatusInfo> DepartmentStatuses { get; } = new();

        public DepartmentTask Task => _task;

        public string TaskNumber => _task.TaskNumber;
        public string Department => _task.GetDepartmentNames();
        public string Description => _task.Description;
        public DateTime SentDate => _task.SentDate;
        public DateTime DueDate => _task.DueDate;
        public string ImportanceDisplay => _task.ImportanceDisplay;
        public bool IsCompleted => _task.IsCompleted;
        public DateTime? CompletedDate => _task.CompletedDate;
        public string Notes => _task.Notes;
        public bool IsOverdue => _task.IsOverdue;
        public int DaysRemaining => _task.DaysRemaining;
        public string Source => _task.Source;
        public string ExecutorShortName => _task.Executor?.ShortName ?? "Не назначен";
        public string ExecutorFullInfo => _task.Executor?.FullInfo ?? "Исполнитель не назначен";

        public string StatusText => IsCompleted ? "Исполнено" : (IsOverdue ? "Просрочено" : "На исполнении");
        public string StatusColor => IsCompleted ? "#4CAF50" : (IsOverdue ? "#F44336" : "#FF9800");
        public bool CanEdit => !IsCompleted;
        public bool HasMultipleDepartments => DepartmentStatuses.Count > 0;

        public ICommand OpenFileCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand MarkAsCompletedCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action? RequestEdit;
        public event Action<bool>? RequestMarkAsCompleted;
        public event Action? RequestClose;

        private void BuildHistory()
        {
            History.Add(new TaskHistoryEntry
            {
                Date = _task.SentDate,
                Event = $"Задание направлено в {_task.Department}",
                IsFinalCompletion = false
            });

            if (!string.IsNullOrWhiteSpace(_task.IntermediateResponsesJson))
            {
                try
                {
                    var responses = JsonSerializer.Deserialize<List<IntermediateResponse>>(_task.IntermediateResponsesJson);
                    if (responses != null)
                    {
                        foreach (var response in responses.OrderBy(r => r.Date))
                        {
                            History.Add(new TaskHistoryEntry
                            {
                                Date = response.Date,
                                Event = $"Дан промежуточный ответ",
                                AttachedDocument = response.DocumentPath,
                                IsFinalCompletion = false
                            });
                        }
                    }
                }
                catch { /* Игнорируем ошибки парсинга */ }
            }

            if (_task.IsCompleted && _task.CompletedDate.HasValue)
            {
                History.Add(new TaskHistoryEntry
                {
                    Date = _task.CompletedDate.Value,
                    Event = "Задание исполнено",
                    IsFinalCompletion = true
                });
            }
        }

        private void OpenFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath)) return;

            try
            {
                if (File.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                }
                else
                {
                    MessageBox.Show(
                        $"Файл не найден: {filePath}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при открытии файла: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void MarkAsCompleted()
        {
            var result = MessageBox.Show(
                "Отметить задание как исполненное и переместить в архив?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                RequestMarkAsCompleted?.Invoke(true);
            }
        }
    }

    /// <summary>
    /// Информация о статусе исполнения задания отделом
    /// </summary>
    public class DepartmentStatusInfo
    {
        public string DepartmentName { get; set; } = string.Empty;
        public string DepartmentFullName { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}
