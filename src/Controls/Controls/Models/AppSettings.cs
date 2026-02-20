using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Controls.Models;

/// <summary>
/// Настройки приложения (singleton record в БД)
/// </summary>
public class AppSettings
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; } = 1;

    /// <summary>
    /// Частота проверки уведомлений (в минутах). По умолчанию 30 минут.
    /// </summary>
    public int CheckIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Частота отправки уведомлений для завтрашних заданий (в минутах). По умолчанию 720 минут (12 часов).
    /// </summary>
    public int DueTomorrowIntervalMinutes { get; set; } = 720;

    /// <summary>
    /// Частота отправки уведомлений для сегодняшних заданий (в минутах). По умолчанию 30 минут.
    /// </summary>
    public int DueTodayIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// Частота отправки уведомлений для просроченных заданий (в минутах). По умолчанию 15 минут (фиксировано).
    /// </summary>
    public int OverdueIntervalMinutes { get; set; } = 15;

    /// <summary>
    /// Наименование организации (отображается в заголовке и навигации приложения).
    /// </summary>
    public string OrganizationName { get; set; } = "Организация";
}
