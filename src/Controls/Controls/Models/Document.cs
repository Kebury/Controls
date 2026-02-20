using System;
using System.ComponentModel.DataAnnotations;

namespace Controls.Models
{
    /// <summary>
    /// Документ (образец PDF, Word, Excel)
    /// </summary>
    public class Document
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Путь к файлу (относительный или абсолютный)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Тип документа (PDF, Word, Excel)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string FileType { get; set; } = string.Empty;

        /// <summary>
        /// Описание документа
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Категория документа (Контроль ГВСУ/ВСУ, Образец донесения, Направленные донесения)
        /// </summary>
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Дата добавления
        /// </summary>
        [Required]
        public DateTime AddedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Размер файла в байтах
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// ID связанного контрольного задания
        /// </summary>
        public int? ControlTaskId { get; set; }

        /// <summary>
        /// Связанное контрольное задание
        /// </summary>
        public virtual ControlTask? ControlTask { get; set; }

        /// <summary>
        /// Является ли шаблоном
        /// </summary>
        public bool IsTemplate { get; set; }
    }
}
