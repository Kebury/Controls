using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Controls.Models
{
    /// <summary>
    /// Связь между заданием и отделом (многие ко многим)
    /// Одно задание может быть отправлено в несколько отделов
    /// </summary>
    public class DepartmentTaskDepartment
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID задания
        /// </summary>
        [Required]
        public int DepartmentTaskId { get; set; }

        /// <summary>
        /// Задание
        /// </summary>
        [ForeignKey(nameof(DepartmentTaskId))]
        public DepartmentTask DepartmentTask { get; set; } = null!;

        /// <summary>
        /// ID отдела
        /// </summary>
        [Required]
        public int DepartmentId { get; set; }

        /// <summary>
        /// Отдел
        /// </summary>
        [ForeignKey(nameof(DepartmentId))]
        public Department Department { get; set; } = null!;

        /// <summary>
        /// Отметка об исполнении данным отделом
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// Дата отметки об исполнении данным отделом
        /// </summary>
        public DateTime? CompletedDate { get; set; }

        /// <summary>
        /// Примечания отдела
        /// </summary>
        [MaxLength(1000)]
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// ID исполнителя в данном отделе
        /// </summary>
        public int? ExecutorId { get; set; }

        /// <summary>
        /// Исполнитель в данном отделе
        /// </summary>
        [ForeignKey(nameof(ExecutorId))]
        public Executor? Executor { get; set; }

        // ── Документы по отделу ──────────────────────────────────────

        /// <summary>
        /// Номер направленного документа в данную организацию
        /// (например: исх. № 123/2026)
        /// </summary>
        [MaxLength(200)]
        public string OutgoingDocumentNumber { get; set; } = string.Empty;

        /// <summary>
        /// Пути к направленным (исходящим) файлам для данной организации
        /// (разделённые точкой с запятой)
        /// </summary>
        [MaxLength(4000)]
        public string OutgoingFilePaths { get; set; } = string.Empty;

        /// <summary>
        /// Пути к поступившим (входящим) файлам от данной организации
        /// (разделённые точкой с запятой)
        /// </summary>
        [MaxLength(4000)]
        public string IncomingFilePaths { get; set; } = string.Empty;

        // ── Вспомогательные методы ────────────────────────────────────

        /// <summary>Список направленных файлов.</summary>
        public List<string> GetOutgoingFiles() =>
            string.IsNullOrWhiteSpace(OutgoingFilePaths)
                ? new List<string>()
                : new List<string>(OutgoingFilePaths.Split(';', StringSplitOptions.RemoveEmptyEntries));

        /// <summary>Список поступивших файлов.</summary>
        public List<string> GetIncomingFiles() =>
            string.IsNullOrWhiteSpace(IncomingFilePaths)
                ? new List<string>()
                : new List<string>(IncomingFilePaths.Split(';', StringSplitOptions.RemoveEmptyEntries));
    }
}
