using System;
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
    }
}
