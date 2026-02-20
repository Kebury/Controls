using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using Controls.Data;
using Controls.Models;
using Controls.Services;
using Microsoft.EntityFrameworkCore;

namespace Controls.ViewModels
{
    public class NotificationsViewModel : ViewModelBase
    {
        public static event Action? NotificationsCountChanged;
        
        /// <summary>
        /// Вызвать событие обновления счетчика уведомлений
        /// </summary>
        public static void RaiseNotificationsCountChanged()
        {
            NotificationsCountChanged?.Invoke();
        }
        
        private readonly ControlsDbContext _context;
        private readonly NotificationService _notificationService;
        private ObservableCollection<Notification> _notifications;
        private ICollectionView? _notificationsView;
        private int _unprocessedCount;
        private int _activeCount;
        private int _awaitingCount;
        private string _currentView = "active";
        private string _searchText = string.Empty;
        private bool _isDateDetected = false;
        private string _dateSearchType = "Все даты";

        public ObservableCollection<Notification> Notifications
        {
            get => _notifications;
            set
            {
                _notifications = value;
                OnPropertyChanged();
            }
        }

        public int UnprocessedCount
        {
            get => _unprocessedCount;
            set
            {
                _unprocessedCount = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasUnprocessedNotifications));
            }
        }

        public bool HasUnprocessedNotifications => UnprocessedCount > 0;

        public int ActiveCount
        {
            get => _activeCount;
            set
            {
                _activeCount = value;
                OnPropertyChanged();
            }
        }

        public int AwaitingCount
        {
            get => _awaitingCount;
            set
            {
                _awaitingCount = value;
                OnPropertyChanged();
            }
        }

        public string CurrentView
        {
            get => _currentView;
            set
            {
                if (SetProperty(ref _currentView, value))
                {
                    OnPropertyChanged(nameof(ShowActive));
                    OnPropertyChanged(nameof(ShowAwaiting));
                    OnPropertyChanged(nameof(ShowHistory));
                    FilterNotifications();
                }
            }
        }

        public bool ShowActive => _currentView == "active";
        public bool ShowAwaiting => _currentView == "awaiting";
        public bool ShowHistory => _currentView == "history";

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    IsDateDetected = DetectDate(value);
                    FilterNotifications();
                }
            }
        }

        public bool IsDateDetected
        {
            get => _isDateDetected;
            set => SetProperty(ref _isDateDetected, value);
        }

        public string DateSearchType
        {
            get => _dateSearchType;
            set
            {
                if (SetProperty(ref _dateSearchType, value))
                {
                    FilterNotifications();
                }
            }
        }

        public ObservableCollection<string> DateSearchTypes { get; } = new()
        {
            "Все даты",
            "Дата уведомления",
            "Срок исполнения",
            "Дата исходящего"
        };

        public ICommand MarkAsNotifiedCommand { get; }
        public ICommand ShowReportDialogCommand { get; }
        public ICommand ShowWorkingOrderDialogCommand { get; }
        public ICommand ViewTaskDetailsCommand { get; }
        public ICommand ShowActiveCommand { get; }
        public ICommand ShowAwaitingCommand { get; }
        public ICommand ShowHistoryCommand { get; }

        public NotificationsViewModel()
        {
            _context = new ControlsDbContext();
            _notificationService = new NotificationService(_context);
            _notifications = new ObservableCollection<Notification>();

            MarkAsNotifiedCommand = new RelayCommand(param => MarkAsNotified(param as Notification));
            ShowReportDialogCommand = new RelayCommand(param => ShowReportDialog(param as Notification));
            ShowWorkingOrderDialogCommand = new RelayCommand(param => ShowWorkingOrderDialog(param as Notification));
            ViewTaskDetailsCommand = new RelayCommand(param => ViewTaskDetails(param as Notification));
            ShowActiveCommand = new RelayCommand(_ => CurrentView = "active");
            ShowAwaitingCommand = new RelayCommand(_ => CurrentView = "awaiting");
            ShowHistoryCommand = new RelayCommand(_ => CurrentView = "history");

            NotificationService.TaskUpdated += OnTaskUpdated;

            _ = LoadNotifications();
        }

        /// <summary>
        /// Освобождение ресурсов (DbContext и отписка от событий)
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                NotificationService.TaskUpdated -= OnTaskUpdated;
                
                _context?.Dispose();
            }
            
            base.Dispose(disposing);
        }

        /// <summary>
        /// Обработчик события обновления заданий - автоматически обновляет список уведомлений
        /// </summary>
        private void OnTaskUpdated()
        {
            _ = Application.Current?.Dispatcher.InvokeAsync(async () =>
            {
                await RefreshNotificationsAsync();
            });
        }

        /// <summary>
        /// Обновление уведомлений с повторной генерацией
        /// </summary>
        private async Task RefreshNotificationsAsync()
        {
            try
            {
                await _notificationService.GenerateNotificationsAsync();
                await LoadNotifications();
            }
            catch
            {
            }
        }

        private async Task LoadNotifications()
        {
            try
            {
                _context.ChangeTracker.Clear();
                
                var query = _context.Notifications
                    .Include(n => n.ControlTask)
                    .AsNoTracking()
                    .OrderByDescending(n => n.NotificationDate)
                    .AsQueryable();

                var allNotifications = await query.ToListAsync();
                
                Notifications.Clear();
                foreach (var notification in allNotifications)
                {
                    Notifications.Add(notification);
                }

                _notificationsView = CollectionViewSource.GetDefaultView(Notifications);
                if (_notificationsView != null)
                {
                    _notificationsView.Filter = null;
                }
                FilterNotifications();
                
                await UpdateUnprocessedCount();
                await UpdateCounts(allNotifications);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Фильтрация уведомлений по текущей вкладке и поисковому запросу
        /// </summary>
        private void FilterNotifications()
        {
            if (_notificationsView == null) return;

            _notificationsView.Filter = obj =>
            {
                if (obj is not Notification notification) return false;
                if (notification.ControlTask == null) return false;

                bool viewMatches = _currentView switch
                {
                    "active" => notification.ControlTask.Status != "Исполнено" && 
                                !notification.IsAwaitingReport &&
                                !notification.IsCompletedInWorkingOrder &&
                                string.IsNullOrEmpty(notification.OutgoingNumber),
                    "awaiting" => notification.IsAwaitingReport && 
                                  notification.ControlTask.Status != "Исполнено",
                    "history" => notification.IsReportSent || 
                                 notification.IsCompletedInWorkingOrder || 
                                 !string.IsNullOrEmpty(notification.OutgoingNumber),
                    _ => true
                };

                if (!viewMatches)
                {
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    var searchLower = SearchText.ToLower();
                    
                    if (IsDateDetected)
                    {
                        bool dateMatch = DateSearchType switch
                        {
                            "Дата уведомления" => notification.NotificationDate.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                                   notification.NotificationDate.ToString("dd.MM.yy").Contains(searchLower),
                            "Срок исполнения" => notification.ControlTask.DueDate.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                                     notification.ControlTask.DueDate.ToString("dd.MM.yy").Contains(searchLower),
                            "Дата исходящего" => notification.OutgoingDate.HasValue && 
                                                      (notification.OutgoingDate.Value.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                                       notification.OutgoingDate.Value.ToString("dd.MM.yy").Contains(searchLower)),
                            _ =>
                                notification.NotificationDate.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                notification.NotificationDate.ToString("dd.MM.yy").Contains(searchLower) ||
                                notification.ControlTask.DueDate.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                notification.ControlTask.DueDate.ToString("dd.MM.yy").Contains(searchLower) ||
                                (notification.OutgoingDate.HasValue && 
                                 (notification.OutgoingDate.Value.ToString("dd.MM.yyyy").Contains(searchLower) ||
                                  notification.OutgoingDate.Value.ToString("dd.MM.yy").Contains(searchLower)))
                        };
                        
                        return dateMatch;
                    }
                    else
                    {
                        var textMatch = notification.ControlTask.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                        notification.ControlTask.ControlNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                        notification.ControlTask.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                        notification.Message.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                        
                        return textMatch;
                    }
                }

                return true;
            };

            _notificationsView.Refresh();
        }

        private async Task UpdateUnprocessedCount()
        {
            UnprocessedCount = await _notificationService.GetUnprocessedCountAsync();
            NotificationsCountChanged?.Invoke();
        }

        private Task UpdateCounts(List<Notification> allNotifications)
        {
            ActiveCount = allNotifications.Count(n => n.ControlTask != null && 
                                                       n.ControlTask.Status != "Исполнено" && 
                                                       !n.IsAwaitingReport &&
                                                       !n.IsCompletedInWorkingOrder &&
                                                       string.IsNullOrEmpty(n.OutgoingNumber));
            
            AwaitingCount = allNotifications.Count(n => n.IsAwaitingReport && 
                                                         n.ControlTask?.Status != "Исполнено");
            
            return Task.CompletedTask;
        }

        private async void MarkAsNotified(Notification? notification)
        {
            if (notification == null || notification.NotificationType != NotificationType.DueTomorrow)
                return;

            try
            {
                using (var saveContext = new ControlsDbContext())
                {
                    var saveService = new NotificationService(saveContext);
                    await saveService.MarkAsNotifiedAsync(notification.Id);
                }
                await RefreshNotificationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки уведомления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ShowReportDialog(Notification? notification)
        {
            if (notification == null)
            {
                return;
            }

            if (notification.IsAwaitingReport || notification.NotificationType == NotificationType.Overdue)
            {
                using (var saveContext = new ControlsDbContext())
                {
                    var saveService = new NotificationService(saveContext);
                    var overdueDialog = new Views.OverdueReportDialog(saveService, notification.Id);
                    overdueDialog.Owner = Application.Current.MainWindow;
                    var result = overdueDialog.ShowDialog();
                    if (result == true)
                    {
                        await RefreshNotificationsAsync();
                    }
                }
            }
            else if (notification.NotificationType == NotificationType.DueToday)
            {
                using (var saveContext = new ControlsDbContext())
                {
                    var saveService = new NotificationService(saveContext);
                    var dialog = new Views.OutgoingReportDialog(saveService, notification.Id, showDatePicker: false);
                    dialog.Owner = Application.Current.MainWindow;
                    var result = dialog.ShowDialog();
                    if (result == true)
                    {
                        await RefreshNotificationsAsync();
                    }
                }
            }
        }

        private async void ShowWorkingOrderDialog(Notification? notification)
        {
            if (notification == null || notification.NotificationType != NotificationType.DueToday)
            {
                return;
            }

            var dialog = new Views.WorkingOrderDialog();
            dialog.Owner = Application.Current.MainWindow;
            var result = dialog.ShowDialog();
            
            if (result == true)
            {
                switch (dialog.Result)
                {
                    case Views.WorkingOrderDialog.WorkingOrderResult.WithReport:
                        await MarkAsAwaitingReport(notification);
                        break;
                    case Views.WorkingOrderDialog.WorkingOrderResult.WithoutReport:
                        await MarkCompletedInWorkingOrder(notification);
                        break;
                }
            }
        }

        private async Task MarkAsAwaitingReport(Notification notification)
        {
            try
            {
                using (var saveContext = new ControlsDbContext())
                {
                    var saveService = new NotificationService(saveContext);
                    await saveService.MarkAsAwaitingReportAsync(notification.Id);
                }
                await RefreshNotificationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task MarkCompletedInWorkingOrder(Notification notification)
        {
            try
            {
                using (var saveContext = new ControlsDbContext())
                {
                    var saveService = new NotificationService(saveContext);
                    await saveService.MarkCompletedInWorkingOrderAsync(notification.Id);
                }
                await RefreshNotificationsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обработки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewTaskDetails(Notification? notification)
        {
            if (notification?.ControlTask == null)
                return;

            var freshTask = _context.ControlTasks
                .Include(t => t.Documents)
                .FirstOrDefault(t => t.Id == notification.ControlTask.Id);
            
            if (freshTask == null)
                return;

            var detailWindow = new Views.TaskDetailWindow();
            var viewModel = new TaskDetailViewModel(freshTask);
            viewModel.RequestClose += () => detailWindow.Close();
            detailWindow.DataContext = viewModel;
            detailWindow.Owner = Application.Current.MainWindow;
            detailWindow.ShowDialog();
        }

        /// <summary>
        /// Определяет, содержит ли текст дату (форматы: dd.MM.yyyy, dd.MM.yy, dd.MM, dd/MM и т.д.)
        /// </summary>
        private bool DetectDate(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            
            var datePatterns = new[]
            {
                @"\d{1,2}\.\d{1,2}\.\d{2,4}",
                @"\d{1,2}\.\d{1,2}",
                @"\d{1,2}/\d{1,2}/\d{2,4}",
                @"\d{1,2}/\d{1,2}",
                @"\d{1,2}-\d{1,2}-\d{2,4}",
                @"\d{1,2}-\d{1,2}"
            };
            
            foreach (var pattern in datePatterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(text, pattern))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
