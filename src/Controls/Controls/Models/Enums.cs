namespace Controls.Models
{
    /// <summary>
    /// Типы контрольных заданий
    /// </summary>
    public enum TaskType
    {
        Разовое,
        Ежедневное,
        Еженедельное,
        Ежемесячное,
        Ежеквартальное,
        Полугодовое,
        Годовое,
        Обращение,
        Запрос
    }

    /// <summary>
    /// Приоритет важности
    /// </summary>
    public enum ImportancePriority
    {
        Стандартная = 1,
        Высокая = 2
    }

    /// <summary>
    /// Приоритет срочности
    /// </summary>
    public enum UrgencyPriority
    {
        Несрочно = 1,
        Обычно = 2,
        Срочно = 3,
        Неотложно = 4
    }

    /// <summary>
    /// Тип уведомления
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Завтра нужно исполнить
        /// </summary>
        DueTomorrow,
        /// <summary>
        /// Сегодня нужно исполнить
        /// </summary>
        DueToday,
        /// <summary>
        /// Исполнение просрочено
        /// </summary>
        Overdue,
        /// <summary>
        /// Скоро нужно исполнить (в течение 3-7 дней)
        /// </summary>
        DueSoon
    }
}
