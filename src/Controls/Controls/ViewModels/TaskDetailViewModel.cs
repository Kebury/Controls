using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using Controls.Models;
using Controls.Helpers;

namespace Controls.ViewModels
{
    public class TaskDetailViewModel : ViewModelBase
    {
        private ControlTask _task;

        public TaskDetailViewModel(ControlTask task)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));
            
            OpenControlDocumentCommand = new RelayCommand(_ => OpenControlDocument());
            OpenReportTemplateCommand = new RelayCommand(_ => OpenReportTemplate());
            OpenReportsFolderCommand = new RelayCommand(_ => OpenReportsFolder());
            OpenDocumentCommand = new RelayCommand(doc => OpenDocument(doc as Document));
            EditCommand = new RelayCommand(_ => RequestEdit?.Invoke());
            MarkAsCompletedCommand = new RelayCommand(_ => MarkAsCompleted(), _ => CanEdit);
            CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
            
            LoadIntermediateResponses();
        }

        public ObservableCollection<IntermediateResponseDisplay> IntermediateResponses { get; } = new();

        public ControlTask Task
        {
            get => _task;
            set => SetProperty(ref _task, value);
        }

        /// <summary>
        /// Проверка наличия документов
        /// </summary>
        public bool HasDocuments => Task?.Documents != null && Task.Documents.Any();
        
        /// <summary>
        /// Проверка возможности редактирования (нельзя редактировать архивные задания)
        /// </summary>
        public bool CanEdit => Task?.Status != "Исполнено";

        /// <summary>
        /// Видимость блока дат для типа НесколькоРазВГод
        /// </summary>
        public bool HasCustomDates => Task?.TaskType == TaskType.НесколькоРазВГод &&
                                      !string.IsNullOrWhiteSpace(Task?.CustomDatesJson);

        /// <summary>
        /// Список дат исполнения в году для типа НесколькоРазВГод
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<DateTime> CustomDates
        {
            get
            {
                if (!HasCustomDates) return System.Array.Empty<DateTime>();
                try
                {
                    return JsonSerializer.Deserialize<System.Collections.Generic.List<DateTime>>(Task!.CustomDatesJson)
                           ?? System.Array.Empty<DateTime>() as System.Collections.Generic.IReadOnlyList<DateTime>;
                }
                catch { return System.Array.Empty<DateTime>(); }
            }
        }

        public ICommand OpenControlDocumentCommand { get; }
        public ICommand OpenReportTemplateCommand { get; }
        public ICommand OpenReportsFolderCommand { get; }
        public ICommand OpenDocumentCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand MarkAsCompletedCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action? RequestClose;
        public event Action? RequestEdit;
        public event Action<bool>? RequestMarkAsCompleted;

        private void OpenControlDocument()
        {
            if (!string.IsNullOrEmpty(Task.ControlDocumentPath) && System.IO.File.Exists(Task.ControlDocumentPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = Task.ControlDocumentPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка открытия документа: {ex.Message}", "Ошибка", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Документ контроля не найден", "Информация",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void OpenReportTemplate()
        {
            if (!string.IsNullOrEmpty(Task.ReportTemplatePath) && System.IO.File.Exists(Task.ReportTemplatePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = Task.ReportTemplatePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка открытия образца: {ex.Message}", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Образец донесения не найден", "Информация",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void OpenReportsFolder()
        {
            if (!string.IsNullOrEmpty(Task.ReportsFolderPath) && System.IO.Directory.Exists(Task.ReportsFolderPath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = Task.ReportsFolderPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка открытия папки: {ex.Message}", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Папка с донесениями не найдена", "Информация",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Открытие приложенного документа
        /// </summary>
        private void OpenDocument(Document? document)
        {
            if (document == null) return;

            if (!string.IsNullOrEmpty(document.FilePath) && System.IO.File.Exists(document.FilePath))
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = document.FilePath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка открытия документа: {ex.Message}", "Ошибка",
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show($"Документ не найден: {document.FileName}", "Информация",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void MarkAsCompleted()
        {
            var result = System.Windows.MessageBox.Show(
                "Отметить задание как исполненное и переместить в архив?",
                "Подтверждение",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                RequestMarkAsCompleted?.Invoke(true);
            }
        }
        
        private void LoadIntermediateResponses()
        {
            try
            {
                IntermediateResponses.Clear();
                
                if (!string.IsNullOrWhiteSpace(_task.IntermediateResponsesJson))
                {
                    var responses = JsonSerializer.Deserialize<System.Collections.Generic.List<IntermediateResponse>>(_task.IntermediateResponsesJson);
                    
                    if (responses != null && responses.Any())
                    {
                        foreach (var response in responses.OrderByDescending(r => r.Date))
                        {
                            IntermediateResponses.Add(new IntermediateResponseDisplay
                            {
                                Date = response.Date,
                                ActionType = response.ActionType,
                                OutgoingNumber = response.OutgoingNumber,
                                OriginalDueDate = response.OriginalDueDate
                            });
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
    
    /// <summary>
    /// Класс для отображения промежуточного ответа в UI
    /// </summary>
    public class IntermediateResponseDisplay
    {
        public DateTime Date { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string? OutgoingNumber { get; set; }
        public DateTime? OriginalDueDate { get; set; }
        
        public string DisplayText
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                
                if (OriginalDueDate.HasValue)
                {
                    parts.Add($"Срок задания истекал {OriginalDueDate.Value:dd.MM.yyyy}");
                }
                
                if (!string.IsNullOrWhiteSpace(ActionType))
                {
                    var actionText = ActionType.ToLower();
                    if (actionText.Contains("донесение"))
                    {
                        var text = $"донесение направлено {Date:dd.MM.yyyy}";
                        if (!string.IsNullOrWhiteSpace(OutgoingNumber))
                        {
                            text += $" (исх. № {OutgoingNumber})";
                        }
                        parts.Add(text);
                    }
                    else if (actionText.Contains("рабочем порядке"))
                    {
                        parts.Add($"исполнено в рабочем порядке {Date:dd.MM.yyyy}");
                    }
                    else if (actionText.Contains("отметка"))
                    {
                        parts.Add($"отметка об исполнении {Date:dd.MM.yyyy}");
                    }
                    else
                    {
                        parts.Add($"{ActionType} {Date:dd.MM.yyyy}");
                    }
                }
                
                return string.Join(", ", parts);
            }
        }
        
        public string DateDisplay => Date.ToString("dd.MM.yyyy HH:mm");
    }
}
