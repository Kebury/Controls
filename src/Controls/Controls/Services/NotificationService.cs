using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Controls.Data;
using Controls.Models;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Diagnostics;
using Controls.Helpers;

namespace Controls.Services
{
    /// <summary>
    /// Сервис для работы с уведомлениями
    /// </summary>
    public class NotificationService
    {
        private readonly ControlsDbContext _context;
        
        /// <summary>
        /// Событие, вызываемое при обновлении задания
        /// </summary>
        public static event Action? TaskUpdated;

        /// <summary>
        /// Вызов события обновления задания
        /// </summary>
        public static void RaiseTaskUpdated()
        {
            TaskUpdated?.Invoke();
        }

        public NotificationService(ControlsDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Генерация уведомлений для всех контрольных заданий
        /// </summary>
        public async Task GenerateNotificationsAsync()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            
            await CleanupOutdatedNotificationsAsync(today, tomorrow);
            
            var tasks = await _context.ControlTasks
                .Where(t => t.Status != "Исполнено" && t.Status != "Отменено")
                .ToListAsync();

            foreach (var task in tasks)
            {
                await CreateNotificationIfNeededAsync(task, today, tomorrow);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Очистка устаревших необработанных уведомлений
        /// </summary>
        private async Task CleanupOutdatedNotificationsAsync(DateTime today, DateTime tomorrow)
        {
            var allNotifications = await _context.Notifications
                .AsTracking()
                .Include(n => n.ControlTask)
                .ToListAsync();

            var notificationsToRemove = new List<Notification>();

            foreach (var notification in allNotifications)
            {
                var task = notification.ControlTask;
                if (task == null)
                {
                    notificationsToRemove.Add(notification);
                    continue;
                }

                // Удалять все уведомления (в том числе обработанные) для завершённых/отменённых заданий
                if (task.Status == "Исполнено" || task.Status == "Отменено")
                {
                    notificationsToRemove.Add(notification);
                    continue;
                }

                // Необработанные уведомления проверяем на актуальность типа
                if (!notification.IsProcessed)
                {
                    var dueDate = task.DueDate.Date;

                    NotificationType? currentType = null;
                    if (dueDate == tomorrow)
                        currentType = NotificationType.DueTomorrow;
                    else if (dueDate == today)
                        currentType = NotificationType.DueToday;
                    else if (dueDate < today)
                        currentType = NotificationType.Overdue;

                    if (currentType == null || notification.NotificationType != currentType)
                    {
                        notificationsToRemove.Add(notification);
                    }
                }
            }

            if (notificationsToRemove.Any())
            {
                _context.Notifications.RemoveRange(notificationsToRemove);
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Создание уведомления для задания если необходимо
        /// </summary>
        private async Task CreateNotificationIfNeededAsync(ControlTask task, DateTime today, DateTime tomorrow)
        {
            var dueDate = task.DueDate.Date;

            NotificationType? notificationType = null;
            
            if (dueDate == tomorrow)
            {
                notificationType = NotificationType.DueTomorrow;
            }
            else if (dueDate == today)
            {
                notificationType = NotificationType.DueToday;
            }
            else if (dueDate < today)
            {
                notificationType = NotificationType.Overdue;
            }

            if (!notificationType.HasValue)
                return;

            var existingNotifications = await _context.Notifications
                .AsTracking()
                .Where(n => n.ControlTaskId == task.Id && n.NotificationType == notificationType.Value)
                .ToListAsync();

            var existingNotification = existingNotifications.FirstOrDefault(n => !n.IsProcessed);
            
            if (existingNotification != null)
            {
                var updatedMessage = GenerateNotificationMessage(task, notificationType.Value);
                if (existingNotification.Message != updatedMessage)
                {
                    existingNotification.Message = updatedMessage;
                    _context.Entry(existingNotification).State = EntityState.Modified;
                }
                return;
            }

            var notification = new Notification
            {
                ControlTaskId = task.Id,
                NotificationType = notificationType.Value,
                NotificationDate = DateTime.Now,
                Message = GenerateNotificationMessage(task, notificationType.Value),
                IsRead = false,
                IsOsNotificationSent = false
            };

            _context.Notifications.Add(notification);
        }

        /// <summary>
        /// Генерация текста уведомления
        /// </summary>
        private string GenerateNotificationMessage(ControlTask task, NotificationType type)
        {
            return type switch
            {
                NotificationType.DueTomorrow => $"Завтра {task.DueDate:dd.MM.yyyy} истекает срок по заданию: {task.Title}",
                NotificationType.DueToday => $"Сегодня истекает срок по заданию: {task.Title}",
                NotificationType.Overdue => $"Просрочено выполнение задания: {task.Title} (срок был {task.DueDate:dd.MM.yyyy})",
                _ => $"Уведомление по заданию: {task.Title}"
            };
        }

        /// <summary>
        /// Отправка Windows Toast уведомлений для необработанных уведомлений
        /// </summary>
        public async Task SendOsNotificationsAsync()
        {
            var settings = _context.GetAppSettings();
            
            var allNotifications = await _context.Notifications
                .AsTracking()
                .Include(n => n.ControlTask)
                .ToListAsync();
            
            var pendingNotifications = allNotifications.Where(n => 
                n.NotificationType == NotificationType.DueTomorrow ||
                n.NotificationType == NotificationType.DueToday ||
                n.NotificationType == NotificationType.Overdue
            ).ToList();
            
            pendingNotifications = pendingNotifications.Where(n => 
                n.ControlTask != null && 
                n.ControlTask.Status != "Исполнено" && 
                n.ControlTask.Status != "Отменено" &&
                !n.IsProcessed
            ).ToList();

            var now = DateTime.Now;

            foreach (var notification in pendingNotifications)
            {
                bool shouldSend = notification.NotificationType switch
                {
                    NotificationType.DueTomorrow => 
                        notification.LastOsNotificationSent == null || 
                        (now - notification.LastOsNotificationSent.Value).TotalMinutes >= settings.DueTomorrowIntervalMinutes,
                    
                    NotificationType.DueToday => 
                        notification.LastOsNotificationSent == null || 
                        (now - notification.LastOsNotificationSent.Value).TotalMinutes >= settings.DueTodayIntervalMinutes,
                    
                    NotificationType.Overdue => 
                        notification.LastOsNotificationSent == null || 
                        (now - notification.LastOsNotificationSent.Value).TotalMinutes >= settings.OverdueIntervalMinutes,
                    
                    _ => false
                };

                if (!shouldSend)
                {
                    continue;
                }

                try
                {
                    SendWindowsToast(notification);
                    notification.IsOsNotificationSent = true;
                    notification.LastOsNotificationSent = now;
                    
                    _context.Entry(notification).State = EntityState.Modified;
                }
                catch (Exception)
                {
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Отправка Windows Toast уведомления (информационное, без кнопок)
        /// </summary>
        private void SendWindowsToast(Notification notification)
        {
            var title = notification.NotificationTypeDisplay;
            var message = notification.Message;
            
            try
            {
                var toastBuilder = new ToastContentBuilder()
                    .AddText(title)
                    .AddText(message)
                    .AddAudio(new Uri("ms-winsoundevent:Notification.Default"))
                    .SetToastDuration(ToastDuration.Long);

                toastBuilder.Show();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Получение количества необработанных уведомлений
        /// </summary>
        public async Task<int> GetUnprocessedCountAsync()
        {
            using var freshContext = new ControlsDbContext();
            var allNotifications = await freshContext.Notifications.ToListAsync();
            return allNotifications.Count(n => !n.IsProcessed);
        }

        /// <summary>
        /// Отметить уведомление как "Уведомлен"
        /// </summary>
        public async Task MarkAsNotifiedAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && notification.NotificationType == NotificationType.DueTomorrow)
            {
                notification.IsNotified = true;
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                
                TaskUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Отметить как "Донесение направлено" с номером исходящего
        /// </summary>
        public async Task MarkReportSentAsync(int notificationId, string outgoingNumber, DateTime? outgoingDate = null)
        {
            var notification = await _context.Notifications
                .Include(n => n.ControlTask)
                .FirstOrDefaultAsync(n => n.Id == notificationId);
            
            if (notification != null)
            {
                notification.IsReportSent = true;
                notification.OutgoingNumber = outgoingNumber;
                notification.OutgoingDate = outgoingDate ?? DateTime.Today;
                notification.IsRead = true;
                notification.IsAwaitingReport = false;
                
                if (notification.ControlTask != null)
                {
                    if (notification.ControlTask.IsCyclicTask)
                    {
                        AddIntermediateResponse(notification.ControlTask, outgoingDate ?? DateTime.Today, "Ответ направлен", outgoingNumber);
                        notification.ControlTask.Status = "На исполнении";
                    }
                    else
                    {
                        AddIntermediateResponse(notification.ControlTask, outgoingDate ?? DateTime.Today, "Ответ направлен (задание завершено)", outgoingNumber);
                        notification.ControlTask.Status = "Исполнено";
                        notification.ControlTask.CompletedDate = DateTime.Now;
                    }
                }
                
                _context.Entry(notification).State = EntityState.Modified;
                if (notification.ControlTask != null)
                {
                    _context.Entry(notification.ControlTask).State = EntityState.Modified;
                }
                
                await _context.SaveChangesAsync();
                
                try
                {
                    TaskUpdated?.Invoke();
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Отметить как "Исполнено в рабочем порядке"
        /// </summary>
        public async Task MarkCompletedInWorkingOrderAsync(int notificationId)
        {
            var notification = await _context.Notifications
                .Include(n => n.ControlTask)
                .FirstOrDefaultAsync(n => n.Id == notificationId);
            
            if (notification != null && notification.NotificationType == NotificationType.DueToday)
            {
                notification.IsCompletedInWorkingOrder = true;
                notification.IsRead = true;
                
                if (notification.ControlTask != null)
                {
                    if (notification.ControlTask.IsCyclicTask)
                    {
                        AddIntermediateResponse(notification.ControlTask, DateTime.Today, "Исполнено в рабочем порядке");
                        notification.ControlTask.Status = "На исполнении";
                    }
                    else
                    {
                        AddIntermediateResponse(notification.ControlTask, DateTime.Today, "Исполнено в рабочем порядке (задание завершено)");
                        notification.ControlTask.Status = "Исполнено";
                        notification.ControlTask.CompletedDate = DateTime.Now;
                    }
                }
                
                _context.Entry(notification).State = EntityState.Modified;
                if (notification.ControlTask != null)
                {
                    _context.Entry(notification.ControlTask).State = EntityState.Modified;
                }
                
                await _context.SaveChangesAsync();
                
                try
                {
                    TaskUpdated?.Invoke();
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Отметить как "Ожидает направления донесения"
        /// </summary>
        public async Task MarkAsAwaitingReportAsync(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            
            if (notification != null)
            {
                notification.IsAwaitingReport = true;
                notification.IsRead = true;
                
                _context.Entry(notification).State = EntityState.Modified;
                
                await _context.SaveChangesAsync();
                
                TaskUpdated?.Invoke();
            }
        }

        /// <summary>
        /// Синхронизация статусов уведомлений с актуальными статусами заданий
        /// </summary>
        public async Task SyncNotificationStatusesAsync()
        {
            var notifications = await _context.Notifications
                .Include(n => n.ControlTask)
                .ToListAsync();

            bool hasChanges = false;

            foreach (var notification in notifications)
            {
                if (notification.ControlTask == null)
                    continue;

                if (!string.IsNullOrEmpty(notification.OutgoingNumber) && !notification.IsReportSent)
                {
                    notification.IsReportSent = true;
                    notification.IsAwaitingReport = false;
                    
                    if (notification.ControlTask.Status != "Исполнено")
                    {
                        if (notification.ControlTask.IsCyclicTask)
                        {
                            AddIntermediateResponse(notification.ControlTask, notification.OutgoingDate ?? DateTime.Now, "Донесение направлено", notification.OutgoingNumber);
                            notification.ControlTask.Status = "На исполнении";
                        }
                        else
                        {
                            AddIntermediateResponse(notification.ControlTask, notification.OutgoingDate ?? DateTime.Now, "Донесение направлено (задание завершено)", notification.OutgoingNumber);
                            notification.ControlTask.Status = "Исполнено";
                            notification.ControlTask.CompletedDate = notification.OutgoingDate ?? DateTime.Now;
                        }
                    }
                    
                    hasChanges = true;
                }

                if (notification.ControlTask.Status == "Исполнено" && notification.IsAwaitingReport)
                {
                    if (!string.IsNullOrEmpty(notification.OutgoingNumber))
                    {
                        notification.IsAwaitingReport = false;
                        notification.IsReportSent = true;
                        hasChanges = true;
                    }
                }

                if (notification.ControlTask.Status == "Исполнено" && 
                    !notification.IsReportSent && 
                    !notification.IsCompletedInWorkingOrder &&
                    !notification.IsAwaitingReport)
                {
                    if (!string.IsNullOrEmpty(notification.OutgoingNumber))
                    {
                        notification.IsReportSent = true;
                        hasChanges = true;
                    }
                    else
                    {
                        notification.IsCompletedInWorkingOrder = true;
                        hasChanges = true;
                    }
                }
            }

            if (hasChanges)
            {
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Обработка уведомления с проверкой типа и открытием необходимых диалогов
        /// </summary>
        public async Task<bool> ProcessNotificationActionAsync(int notificationId, string action)
        {
            var notification = await _context.Notifications
                .Include(n => n.ControlTask)
                .FirstOrDefaultAsync(n => n.Id == notificationId);

            if (notification == null)
                return false;

            switch (action)
            {
                case "notified":
                    if (notification.NotificationType == NotificationType.DueTomorrow)
                    {
                        await MarkAsNotifiedAsync(notificationId);
                        return true;
                    }
                    break;

                case "reportSent":
                    return false;

                case "workingOrder":
                    if (notification.NotificationType == NotificationType.DueToday)
                    {
                        await MarkCompletedInWorkingOrderAsync(notificationId);
                        return true;
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// Получение уведомления по ID
        /// </summary>
        public async Task<Notification?> GetNotificationByIdAsync(int notificationId)
        {
            return await _context.Notifications
                .Include(n => n.ControlTask)
                .FirstOrDefaultAsync(n => n.Id == notificationId);
        }
        /// <summary>
        /// Добавить промежуточный ответ для цикличного задания
        /// </summary>
        private void AddIntermediateResponse(ControlTask task, DateTime responseDate, string actionType = "", string? outgoingNumber = null)
        {
            try
            {
                var originalDueDate = task.DueDate;
                
                List<IntermediateResponse> responses;
                
                if (!string.IsNullOrWhiteSpace(task.IntermediateResponsesJson))
                {
                    responses = JsonSerializer.Deserialize<List<IntermediateResponse>>(task.IntermediateResponsesJson) 
                        ?? new List<IntermediateResponse>();
                }
                else
                {
                    responses = new List<IntermediateResponse>();
                }
                
                responses.Add(new IntermediateResponse
                {
                    Date = responseDate,
                    ActionType = actionType,
                    OutgoingNumber = outgoingNumber,
                    OriginalDueDate = originalDueDate
                });
                
                task.IntermediateResponsesJson = JsonSerializer.Serialize(responses);
                
                if (task.IsCyclicTask)
                {
                    task.DueDate = task.CalculateNextDueDate();
                }
            }
            catch
            {
            }
        }    }
}

namespace Microsoft.Toolkit.Uwp.Notifications
{
    public static class ToastContentBuilderExtensions
    {
        public static void Show(this ToastContentBuilder builder)
        {
            try
            {
                var toastContent = builder.GetToastContent();
                var xml = toastContent.GetContent();
                
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $@"-NoProfile -WindowStyle Hidden -Command ""
                        [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                        [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
                        $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
                        $xml.LoadXml('{xml.Replace("'", "''")}')
                        $toast = New-Object Windows.UI.Notifications.ToastNotification($xml)
                        [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier().Show($toast)
                    """,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception)
            {
            }
        }
    }
}
