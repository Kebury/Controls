using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;

namespace Controls.Models
{
    /// <summary>
    /// Контрольное задание
    /// </summary>
    public class ControlTask
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Наименование задания
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Содержание задания
        /// </summary>
        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания задания
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Срок исполнения
        /// </summary>
        [Required]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Тип контрольного задания
        /// </summary>
        [Required]
        public TaskType TaskType { get; set; } = TaskType.Разовое;

        /// <summary>
        /// Приоритет важности
        /// </summary>
        [Required]
        public ImportancePriority ImportancePriority { get; set; } = ImportancePriority.Стандартная;

        /// <summary>
        /// Приоритет срочности
        /// </summary>
        [Required]
        public UrgencyPriority UrgencyPriority { get; set; } = UrgencyPriority.Обычно;

        /// <summary>
        /// Статус (Новое, На исполнении, Исполнено, Просрочено)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Новое";

        /// <summary>
        /// Исполнитель
        /// </summary>
        [MaxLength(100)]
        public string Assignee { get; set; } = string.Empty;

        /// <summary>
        /// Контроль ГВСУ/ВСУ - путь к документу контрольного задания
        /// </summary>
        [MaxLength(500)]
        public string ControlDocumentPath { get; set; } = string.Empty;

        /// <summary>
        /// Номер контроля ВСУ
        /// </summary>
        [MaxLength(100)]
        public string ControlNumber { get; set; } = string.Empty;

        /// <summary>
        /// Образец донесения - путь к файлу образца
        /// </summary>
        [MaxLength(500)]
        public string ReportTemplatePath { get; set; } = string.Empty;

        /// <summary>
        /// Папка с донесениями - путь к сетевой папке
        /// </summary>
        [MaxLength(500)]
        public string ReportsFolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Примечания
        /// </summary>
        [MaxLength(1000)]
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// История промежуточных ответов для цикличных заданий (JSON формат)
        /// </summary>
        [MaxLength(5000)]
        public string IntermediateResponsesJson { get; set; } = string.Empty;

        /// <summary>
        /// JSON массив дат исполнения для типа "НесколькоРазВГод" (формат: ["yyyy-MM-ddTHH:mm:ss", ...])
        /// </summary>
        [MaxLength(5000)]
        public string CustomDatesJson { get; set; } = string.Empty;

        /// <summary>
        /// Дата завершения
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Связанные документы
        /// </summary>
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

        /// <summary>
        /// Проверка просрочено ли задание
        /// </summary>
        public bool IsOverdue => Status != "Исполнено" && DueDate < DateTime.Now;

        /// <summary>
        /// Количество дней до срока
        /// </summary>
        public int DaysRemaining => (DueDate - DateTime.Now).Days;

        /// <summary>
        /// Строковое представление типа задания
        /// </summary>
        public string TaskTypeDisplay => TaskType.GetDisplayName();

        /// <summary>
        /// Строковое представление важности
        /// </summary>
        public string ImportanceDisplay => ImportancePriority.ToString();

        /// <summary>
        /// Строковое представление срочности
        /// </summary>
        public string UrgencyDisplay => UrgencyPriority.ToString();
        
        /// <summary>
        /// Проверка является ли задание цикличным (не разовым)
        /// </summary>
        public bool IsCyclicTask => TaskType != TaskType.Разовое && 
                                     TaskType != TaskType.Обращение && 
                                     TaskType != TaskType.Запрос;
        
        /// <summary>
        /// Отображение последнего промежуточного ответа для интерфейса
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string LastIntermediateResponseDisplay
        {
            get
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(IntermediateResponsesJson))
                        return string.Empty;
                    
                    var responses = JsonSerializer.Deserialize<System.Collections.Generic.List<IntermediateResponse>>(IntermediateResponsesJson);
                    if (responses == null || !responses.Any())
                        return string.Empty;
                    
                    var lastResponse = responses.OrderByDescending(r => r.Date).FirstOrDefault();
                    if (lastResponse == null)
                        return string.Empty;
                    
                    var parts = new System.Collections.Generic.List<string>();
                    
                    if (lastResponse.OriginalDueDate.HasValue)
                    {
                        parts.Add($"Срок задания истекал {lastResponse.OriginalDueDate.Value:dd.MM.yyyy}");
                    }
                    
                    if (!string.IsNullOrWhiteSpace(lastResponse.ActionType))
                    {
                        var actionText = lastResponse.ActionType.ToLower();
                        if (actionText.Contains("донесение"))
                        {
                            var text = $"донесение направлено {lastResponse.Date:dd.MM.yyyy}";
                            if (!string.IsNullOrWhiteSpace(lastResponse.OutgoingNumber))
                            {
                                text += $" (исх. № {lastResponse.OutgoingNumber})";
                            }
                            parts.Add(text);
                        }
                        else if (actionText.Contains("рабочем порядке"))
                        {
                            parts.Add($"исполнено в рабочем порядке {lastResponse.Date:dd.MM.yyyy}");
                        }
                        else if (actionText.Contains("отметка"))
                        {
                            parts.Add($"отметка об исполнении {lastResponse.Date:dd.MM.yyyy}");
                        }
                        else
                        {
                            parts.Add($"{lastResponse.ActionType} {lastResponse.Date:dd.MM.yyyy}");
                        }
                    }
                    
                    return string.Join(", ", parts);
                }
                catch
                {
                    return string.Empty;
                }
            }
        }
        
        /// <summary>
        /// Полная информация об исполнителе для tooltip (загружается из БД)
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string? ExecutorInfo { get; set; }
        
        /// <summary>
        /// Фамилия и инициалы исполнителя для отображения в списках
        /// </summary>
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string AssigneeShortName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Assignee))
                    return string.Empty;
                
                var parts = Assignee.Trim().Split(' ');
                if (parts.Length < 2)
                    return Assignee;
                
                var lastName = parts[0];
                var initials = string.Empty;
                
                for (int i = 1; i < parts.Length && i < 3; i++)
                {
                    if (!string.IsNullOrWhiteSpace(parts[i]))
                    {
                        initials += parts[i][0] + ".";
                    }
                }
                
                return $"{lastName} {initials}";
            }
        }
        
        /// <summary>
        /// Рассчитать следующую дату исполнения для цикличного задания
        /// </summary>
        public DateTime CalculateNextDueDate()
        {
            if (!IsCyclicTask)
                return DueDate;
            
            // Для типа НесколькоРазВГод - выбираем следующую дату из списка пользователя
            if (TaskType == TaskType.НесколькоРазВГод)
            {
                if (!string.IsNullOrWhiteSpace(CustomDatesJson))
                {
                    try
                    {
                        var dates = JsonSerializer.Deserialize<System.Collections.Generic.List<DateTime>>(CustomDatesJson);
                        if (dates != null && dates.Count > 0)
                        {
                            var nextCustom = dates
                                .Where(d => d.Date > DateTime.Now.Date)
                                .OrderBy(d => d)
                                .FirstOrDefault();

                            if (nextCustom != default)
                                return new DateTime(nextCustom.Year, nextCustom.Month, nextCustom.Day, 23, 59, 59);

                            var first = dates.OrderBy(d => d).First();
                            return new DateTime(first.Year + 1, first.Month, first.Day, 23, 59, 59);
                        }
                    }
                    catch { /* fallthrough */ }
                }
                return DueDate;
            }

            DateTime nextDate = TaskType switch
            {
                TaskType.Ежедневное => DueDate.AddDays(1),
                TaskType.Еженедельное => DueDate.AddDays(7),
                TaskType.Ежемесячное => DueDate.AddMonths(1),
                TaskType.Ежеквартальное => DueDate.AddMonths(3),
                TaskType.Полугодовое => DueDate.AddMonths(6),
                TaskType.Годовое => DueDate.AddYears(1),
                _ => DueDate
            };
            
            while (nextDate < DateTime.Now)
            {
                nextDate = TaskType switch
                {
                    TaskType.Ежедневное => nextDate.AddDays(1),
                    TaskType.Еженедельное => nextDate.AddDays(7),
                    TaskType.Ежемесячное => nextDate.AddMonths(1),
                    TaskType.Ежеквартальное => nextDate.AddMonths(3),
                    TaskType.Полугодовое => nextDate.AddMonths(6),
                    TaskType.Годовое => nextDate.AddYears(1),
                    _ => nextDate
                };
            }
            
            return new DateTime(nextDate.Year, nextDate.Month, nextDate.Day, 23, 59, 59);
        }
    }
}
