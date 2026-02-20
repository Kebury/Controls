using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Linq;

namespace Controls.Models
{
    /// <summary>
    /// Уведомление о сроках
    /// </summary>
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID контрольного задания
        /// </summary>
        [Required]
        public int ControlTaskId { get; set; }

        /// <summary>
        /// Связанное контрольное задание
        /// </summary>
        public virtual ControlTask ControlTask { get; set; } = null!;

        /// <summary>
        /// Дата и время уведомления
        /// </summary>
        [Required]
        public DateTime NotificationDate { get; set; }

        /// <summary>
        /// Текст уведомления
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Прочитано ли уведомление
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// Тип уведомления
        /// </summary>
        [Required]
        public NotificationType NotificationType { get; set; }

        /// <summary>
        /// Отметка "Уведомлен" (для типа DueTomorrow)
        /// </summary>
        public bool IsNotified { get; set; }

        /// <summary>
        /// Отметка "Донесение направлено" (для типов DueToday и Overdue)
        /// </summary>
        public bool IsReportSent { get; set; }

        /// <summary>
        /// Номер исходящего донесения
        /// </summary>
        [MaxLength(100)]
        public string? OutgoingNumber { get; set; }

        /// <summary>
        /// Дата исходящего донесения
        /// </summary>
        public DateTime? OutgoingDate { get; set; }

        /// <summary>
        /// Задание исполнено в рабочем порядке (донесение будет направлено дополнительно)
        /// </summary>
        public bool IsCompletedInWorkingOrder { get; set; }

        /// <summary>
        /// Ожидает направления донесения (для вкладки "Ожидает")
        /// </summary>
        public bool IsAwaitingReport { get; set; }

        /// <summary>
        /// Отправлено ли уведомление в ОС Windows
        /// </summary>
        public bool IsOsNotificationSent { get; set; }

        /// <summary>
        /// Дата и время последней отправки уведомления в ОС Windows
        /// </summary>
        public DateTime? LastOsNotificationSent { get; set; }

        /// <summary>
        /// Обработано ли уведомление (для скрытия из списка активных)
        /// </summary>
        [NotMapped]
        public bool IsProcessed
        {
            get
            {
                return NotificationType switch
                {
                    NotificationType.DueTomorrow => IsNotified,
                    NotificationType.DueToday => IsReportSent || IsCompletedInWorkingOrder,
                    NotificationType.Overdue => IsReportSent,
                    _ => false
                };
            }
        }

        /// <summary>
        /// Отображаемый тип уведомления
        /// </summary>
        public string NotificationTypeDisplay
        {
            get
            {
                return NotificationType switch
                {
                    NotificationType.DueTomorrow => "Завтра нужно исполнить",
                    NotificationType.DueToday => "Сегодня нужно исполнить",
                    NotificationType.Overdue => "Исполнение просрочено",
                    _ => NotificationType.ToString()
                };
            }
        }
    }
}
