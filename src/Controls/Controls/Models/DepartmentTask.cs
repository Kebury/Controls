using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Controls.Models
{
    /// <summary>
    /// Задание в отдел
    /// </summary>
    public class DepartmentTask
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Номер задания
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string TaskNumber { get; set; } = string.Empty;

        /// <summary>
        /// Военный следственный отдел, в который направлено задание
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Department { get; set; } = string.Empty;

        /// <summary>
        /// Связь с отделами (многие ко многим) - для множественных получателей
        /// </summary>
        public ICollection<DepartmentTaskDepartment> TaskDepartments { get; set; } = new List<DepartmentTaskDepartment>();

        /// <summary>
        /// Краткое описание задания
        /// </summary>
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Дата направления
        /// </summary>
        [Required]
        public DateTime SentDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Дата исполнения
        /// </summary>
        [Required]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// Важность
        /// </summary>
        [Required]
        public ImportancePriority ImportancePriority { get; set; } = ImportancePriority.Стандартная;

        /// <summary>
        /// ID исполнителя (устаревшее поле, оставлено для совместимости)
        /// </summary>
        public int? ExecutorId { get; set; }

        /// <summary>
        /// Исполнитель (устаревшее поле, оставлено для совместимости)
        /// </summary>
        public Executor? Executor { get; set; }

        /// <summary>
        /// Флаг принудительного завершения задания (через кнопку "отметить исполненным")
        /// </summary>
        public bool IsForcedCompleted { get; set; } = false;

        /// <summary>
        /// Отметка об исполнении
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                if (IsForcedCompleted)
                    return true;
                
                if (TaskDepartments != null && TaskDepartments.Any())
                    return TaskDepartments.All(td => td.IsCompleted);
                
                return _isCompleted;
            }
            set => _isCompleted = value;
        }
        private bool _isCompleted = false;

        /// <summary>
        /// Дата отметки об исполнении
        /// </summary>
        public DateTime? CompletedDate
        {
            get
            {
                if (TaskDepartments != null && TaskDepartments.Any() && TaskDepartments.All(td => td.IsCompleted))
                    return TaskDepartments.Max(td => td.CompletedDate);
                
                return _completedDate;
            }
            set => _completedDate = value;
        }
        private DateTime? _completedDate;

        /// <summary>
        /// Пути к прилагаемым файлам задания (разделенные точкой с запятой)
        /// </summary>
        [MaxLength(2000)]
        public string TaskFilePaths { get; set; } = string.Empty;

        /// <summary>
        /// Пути к файлам, поступающим из отделов (разделенные точкой с запятой)
        /// </summary>
        [MaxLength(2000)]
        public string DepartmentFilePaths { get; set; } = string.Empty;

        /// <summary>
        /// Примечания
        /// </summary>
        [MaxLength(1000)]
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Откуда поступило задание
        /// </summary>
        [MaxLength(500)]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// История промежуточных ответов (JSON формат)
        /// </summary>
        [MaxLength(5000)]
        public string IntermediateResponsesJson { get; set; } = string.Empty;

        /// <summary>
        /// Проверка просрочено ли задание
        /// </summary>
        public bool IsOverdue => !IsCompleted && DueDate < DateTime.Now;

        /// <summary>
        /// Получить список названий отделов
        /// </summary>
        public string GetDepartmentNames()
        {
            if (TaskDepartments == null || !TaskDepartments.Any())
                return Department;

            return string.Join(", ", TaskDepartments.Select(td => td.Department?.ShortName ?? "Неизвестен"));
        }

        /// <summary>
        /// Количество завершивших отделов
        /// </summary>
        public int CompletedDepartmentsCount => TaskDepartments.Count(td => td.IsCompleted);

        /// <summary>
        /// Общее количество отделов
        /// </summary>
        public int TotalDepartmentsCount => TaskDepartments.Count;

        /// <summary>
        /// Количество дней до срока
        /// </summary>
        public int DaysRemaining => (DueDate - DateTime.Now).Days;

        /// <summary>
        /// Строковое представление важности
        /// </summary>
        public string ImportanceDisplay => ImportancePriority.ToString();

        /// <summary>
        /// Список файлов задания
        /// </summary>
        public List<string> GetTaskFiles()
        {
            if (string.IsNullOrWhiteSpace(TaskFilePaths))
                return new List<string>();

            return new List<string>(TaskFilePaths.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Список файлов из отделов
        /// </summary>
        public List<string> GetDepartmentFiles()
        {
            if (string.IsNullOrWhiteSpace(DepartmentFilePaths))
                return new List<string>();

            return new List<string>(DepartmentFilePaths.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Добавить файл задания
        /// </summary>
        public void AddTaskFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(TaskFilePaths))
                TaskFilePaths = filePath;
            else
                TaskFilePaths += ";" + filePath;
        }

        /// <summary>
        /// Добавить файл из отдела
        /// </summary>
        public void AddDepartmentFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(DepartmentFilePaths))
                DepartmentFilePaths = filePath;
            else
                DepartmentFilePaths += ";" + filePath;
        }
    }
}
