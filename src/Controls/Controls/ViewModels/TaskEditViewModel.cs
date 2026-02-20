using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.IO;
using Controls.Data;
using Controls.Models;
using Microsoft.Win32;
using Microsoft.EntityFrameworkCore;

namespace Controls.ViewModels
{
    public class TaskEditViewModel : ViewModelBase
    {
        private readonly ControlsDbContext _dbContext;
        private readonly ControlTask? _existingTask;
        private readonly bool _isNewTask;
        private bool _isLoadingExistingTask = false;

        private string _title = string.Empty;
        private string _description = string.Empty;
        private string _assignee = string.Empty;
        private string _controlNumber = string.Empty;
        private string _controlDocumentPath = string.Empty;
        private string _reportTemplatePath = string.Empty;
        private string _reportsFolderPath = string.Empty;
        private string _notes = string.Empty;
        private DateTime _createdDate = DateTime.Now;
        private DateTime _dueDate = DateTime.Now.AddDays(7).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
        private TaskType _selectedTaskType = TaskType.Разовое;
        private ImportancePriority _selectedImportance = ImportancePriority.Стандартная;

        public TaskEditViewModel(ControlsDbContext dbContext, ControlTask? task = null)
        {
            _dbContext = dbContext;
            _existingTask = task;
            _isNewTask = task == null;

            LoadAssignees();

            if (task != null)
            {
                _isLoadingExistingTask = true;

                Title = task.Title;
                Description = task.Description;
                Assignee = task.Assignee;
                ControlNumber = task.ControlNumber;
                ControlDocumentPath = task.ControlDocumentPath;
                ReportTemplatePath = task.ReportTemplatePath;
                ReportsFolderPath = task.ReportsFolderPath;
                Notes = task.Notes;
                CreatedDate = task.CreatedDate;
                SelectedTaskType = task.TaskType;
                SelectedImportance = task.ImportancePriority;
                DueDate = task.DueDate;

                _isLoadingExistingTask = false;

                var docs = _dbContext.Documents.Where(d => d.ControlTaskId == task.Id).ToList();
                foreach (var doc in docs)
                {
                    switch (doc.Category)
                    {
                        case "Контроль ГВСУ/ВСУ":
                        case "Основной документ":
                            ControlDocuments.Add(doc);
                            break;
                        case "Образец донесения":
                        case "Шаблон ответа":
                            ReportTemplateDocuments.Add(doc);
                            break;
                        case "Направленные донесения":
                        case "Направленные ответы":
                            SentReportsDocuments.Add(doc);
                            break;
                    }
                }
            }

            SaveCommand = new RelayCommand(_ => Save(), _ => CanSave());
            CancelCommand = new RelayCommand(_ => Cancel());
            BrowseControlDocumentCommand = new RelayCommand(_ => BrowseControlDocument());
            BrowseReportTemplateCommand = new RelayCommand(_ => BrowseReportTemplate());
            BrowseReportsFolderCommand = new RelayCommand(_ => BrowseReportsFolder());
            
            AttachControlDocumentCommand = new RelayCommand(_ => AttachDocument("Основной документ"));
            AttachReportTemplateDocumentCommand = new RelayCommand(_ => AttachDocument("Шаблон ответа"));
            AttachSentReportDocumentCommand = new RelayCommand(_ => AttachDocument("Направленные ответы"));
            
            RemoveDocumentCommand = new RelayCommand(doc => RemoveDocument(doc as Document));
            OpenDocumentCommand = new RelayCommand(doc => OpenDocument(doc as Document));
        }

        private void LoadAssignees()
        {
            var executors = _dbContext.Executors
                .OrderBy(e => e.FullName)
                .ToList();

            Assignees.Clear();
            foreach (var executor in executors)
            {
                Assignees.Add(executor.FullName);
            }
            
            if (!Assignees.Any())
            {
                var assignees = _dbContext.ControlTasks
                    .Where(t => !string.IsNullOrWhiteSpace(t.Assignee))
                    .Select(t => t.Assignee)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                foreach (var assignee in assignees)
                {
                    Assignees.Add(assignee);
                }
            }
        }

        public bool IsArchived => _existingTask?.Status == "Исполнено";
        public bool CanEditFields => !IsArchived;
        public string WindowTitle => _isNewTask ? "Добавить новое задание" : (IsArchived ? "Просмотр задания (Архив)" : "Редактировать задание");

        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                {
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public string Assignee
        {
            get => _assignee;
            set => SetProperty(ref _assignee, value);
        }

        public ObservableCollection<string> Assignees { get; } = new();
        public ObservableCollection<Document> ControlDocuments { get; } = new();
        public ObservableCollection<Document> ReportTemplateDocuments { get; } = new();
        public ObservableCollection<Document> SentReportsDocuments { get; } = new();

        public string ControlNumber
        {
            get => _controlNumber;
            set => SetProperty(ref _controlNumber, value);
        }

        public string ControlDocumentPath
        {
            get => _controlDocumentPath;
            set => SetProperty(ref _controlDocumentPath, value);
        }

        public string ReportTemplatePath
        {
            get => _reportTemplatePath;
            set => SetProperty(ref _reportTemplatePath, value);
        }

        public string ReportsFolderPath
        {
            get => _reportsFolderPath;
            set => SetProperty(ref _reportsFolderPath, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                if (SetProperty(ref _createdDate, value))
                {
                    CalculateDueDate();
                }
            }
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

        public TaskType SelectedTaskType
        {
            get => _selectedTaskType;
            set
            {
                if (SetProperty(ref _selectedTaskType, value))
                {
                    if (!_isLoadingExistingTask)
                    {
                        CalculateDueDate();
                    }
                }
            }
        }

        private void CalculateDueDate()
        {
            switch (SelectedTaskType)
            {
                case TaskType.Разовое:
                    DueDate = CreatedDate.AddDays(7).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
                case TaskType.Ежедневное:
                    DueDate = CreatedDate.AddDays(1).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
                case TaskType.Еженедельное:
                    DueDate = CreatedDate.AddDays(7).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
                case TaskType.Ежемесячное:
                    DueDate = CreatedDate.AddMonths(1).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
                case TaskType.Ежеквартальное:
                    DueDate = CreatedDate.AddMonths(3).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
                case TaskType.Полугодовое:
                    DueDate = CreatedDate.AddMonths(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
                case TaskType.Годовое:
                    DueDate = CreatedDate.AddYears(1).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
                case TaskType.Обращение:
                case TaskType.Запрос:
                    DueDate = CreatedDate.AddDays(3).Date.AddHours(23).AddMinutes(59).AddSeconds(59);
                    break;
            }
        }

        public ImportancePriority SelectedImportance
        {
            get => _selectedImportance;
            set => SetProperty(ref _selectedImportance, value);
        }

        public IEnumerable<TaskType> TaskTypes => Enum.GetValues(typeof(TaskType)).Cast<TaskType>();
        public IEnumerable<ImportancePriority> ImportancePriorities => Enum.GetValues(typeof(ImportancePriority)).Cast<ImportancePriority>();

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseControlDocumentCommand { get; }
        public ICommand BrowseReportTemplateCommand { get; }
        public ICommand BrowseReportsFolderCommand { get; }
        public ICommand AttachControlDocumentCommand { get; }
        public ICommand AttachReportTemplateDocumentCommand { get; }
        public ICommand AttachSentReportDocumentCommand { get; }
        public ICommand RemoveDocumentCommand { get; }
        public ICommand OpenDocumentCommand { get; }

        public event Action<bool>? RequestClose;

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Title);
        }

        private void Save()
        {
            try
            {
                if (_isNewTask)
                {
                    var duplicate = _dbContext.ControlTasks
                        .FirstOrDefault(t => t.Title == Title && t.DueDate.Date == DueDate.Date);
                    
                    if (duplicate != null)
                    {
                        var result = MessageBox.Show(
                            $"Задание с таким названием и сроком уже существует (ID: {duplicate.Id}).\n\nПродолжить сохранение?",
                            "Предупреждение о дубликате",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);
                        
                        if (result == MessageBoxResult.No)
                            return;
                    }
                    
                    var newTask = new ControlTask
                    {
                        Title = Title,
                        Description = Description,
                        Assignee = Assignee,
                        Status = "Новое",
                        ControlNumber = ControlNumber,
                        ControlDocumentPath = ControlDocumentPath,
                        ReportTemplatePath = ReportTemplatePath,
                        ReportsFolderPath = ReportsFolderPath,
                        Notes = Notes,
                        CreatedDate = CreatedDate,
                        DueDate = DueDate,
                        TaskType = SelectedTaskType,
                        ImportancePriority = SelectedImportance,
                        UrgencyPriority = UrgencyPriority.Обычно
                    };

                    _dbContext.ControlTasks.Add(newTask);
                    _dbContext.SaveChanges();

                    foreach (var doc in ControlDocuments)
                    {
                        doc.ControlTaskId = newTask.Id;
                        _dbContext.Documents.Add(doc);
                    }
                    foreach (var doc in ReportTemplateDocuments)
                    {
                        doc.ControlTaskId = newTask.Id;
                        _dbContext.Documents.Add(doc);
                    }
                    foreach (var doc in SentReportsDocuments)
                    {
                        doc.ControlTaskId = newTask.Id;
                        _dbContext.Documents.Add(doc);
                    }
                }
                else if (_existingTask != null)
                {
                    _existingTask.Title = Title;
                    _existingTask.Description = Description;
                    _existingTask.Assignee = Assignee;
                    _existingTask.ControlNumber = ControlNumber;
                    _existingTask.ControlDocumentPath = ControlDocumentPath;
                    _existingTask.ReportTemplatePath = ReportTemplatePath;
                    _existingTask.ReportsFolderPath = ReportsFolderPath;
                    _existingTask.Notes = Notes;
                    _existingTask.CreatedDate = CreatedDate;
                    _existingTask.DueDate = DueDate;
                    _existingTask.TaskType = SelectedTaskType;
                    _existingTask.ImportancePriority = SelectedImportance;

                    _dbContext.Entry(_existingTask).State = EntityState.Modified;

                    var oldDocs = _dbContext.Documents.Where(d => d.ControlTaskId == _existingTask.Id).ToList();
                    _dbContext.Documents.RemoveRange(oldDocs);
                    
                    foreach (var doc in ControlDocuments)
                    {
                        doc.ControlTaskId = _existingTask.Id;
                        _dbContext.Documents.Add(doc);
                    }
                    foreach (var doc in ReportTemplateDocuments)
                    {
                        doc.ControlTaskId = _existingTask.Id;
                        _dbContext.Documents.Add(doc);
                    }
                    foreach (var doc in SentReportsDocuments)
                    {
                        doc.ControlTaskId = _existingTask.Id;
                        _dbContext.Documents.Add(doc);
                    }
                }

                _dbContext.SaveChanges();
                
                try
                {
                    Services.NotificationService.RaiseTaskUpdated();
                }
                catch
                {
                }
                
                RequestClose?.Invoke(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            RequestClose?.Invoke(false);
        }

        private void BrowseControlDocument()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите документ контроля",
                Filter = "Все файлы (*.*)|*.*|Документы (*.doc;*.docx;*.pdf)|*.doc;*.docx;*.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                ControlDocumentPath = dialog.FileName;
            }
        }

        private void BrowseReportTemplate()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите шаблон ответа",
                Filter = "Все файлы (*.*)|*.*|Документы (*.doc;*.docx;*.pdf)|*.doc;*.docx;*.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                ReportTemplatePath = dialog.FileName;
            }
        }

        private void BrowseReportsFolder()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку с донесениями";
                dialog.ShowNewFolderButton = true;
                
                if (!string.IsNullOrEmpty(ReportsFolderPath) && System.IO.Directory.Exists(ReportsFolderPath))
                {
                    dialog.SelectedPath = ReportsFolderPath;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    ReportsFolderPath = dialog.SelectedPath;
                }
            }
        }

        private void AttachDocument(string category)
        {
            var dialog = new OpenFileDialog
            {
                Title = $"Выберите документ для категории: {category}",
                Filter = "Все файлы (*.*)|*.*|Документы (*.doc;*.docx;*.pdf)|*.doc;*.docx;*.pdf|Изображения (*.jpg;*.png)|*.jpg;*.png",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var filePath in dialog.FileNames)
                {
                    var fileName = Path.GetFileName(filePath);
                    var destFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "documents");
                    Directory.CreateDirectory(destFolder);
                    
                    var destPath = Path.Combine(destFolder, $"{Guid.NewGuid()}_{fileName}");
                    File.Copy(filePath, destPath, true);

                    var document = new Document
                    {
                        FileName = fileName,
                        FilePath = destPath,
                        AddedDate = DateTime.Now,
                        FileType = Path.GetExtension(fileName),
                        FileSize = new FileInfo(filePath).Length,
                        Category = category
                    };

                    switch (category)
                    {
                        case "Основной документ":
                            ControlDocuments.Add(document);
                            break;
                        case "Шаблон ответа":
                            ReportTemplateDocuments.Add(document);
                            break;
                        case "Направленные ответы":
                            SentReportsDocuments.Add(document);
                            break;
                    }
                }
            }
        }

        private void RemoveDocument(Document? doc)
        {
            if (doc != null)
            {
                ControlDocuments.Remove(doc);
                ReportTemplateDocuments.Remove(doc);
                SentReportsDocuments.Remove(doc);
            }
        }

        private void OpenDocument(Document? doc)
        {
            if (doc != null && File.Exists(doc.FilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = doc.FilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка открытия документа: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
