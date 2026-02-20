using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Controls.ViewModels;
using Controls.Services;
using Controls.Data;

namespace Controls;

public partial class MainWindow : Window
{
    private readonly ControlsDbContext _context;
    private readonly NotificationService _notificationService;
    private readonly TrayIconService _trayIconService;
    private readonly DispatcherTimer _notificationTimer;
    private int _unprocessedCount;
    private bool _isDisposed = false;
    private Views.DatabaseReconnectingWindow? _reconnectingWindow;

    public MainWindow()
    {
        InitializeComponent();
        
        _context = new ControlsDbContext();
        _notificationService = new NotificationService(_context);
        
        DataContext = new MainViewModel();

        var trayIcon = (Hardcodet.Wpf.TaskbarNotification.TaskbarIcon)this.Resources["TrayIcon"];
        
        _trayIconService = new TrayIconService(trayIcon, _context);

        var settings = _context.GetAppSettings();
        
        _notificationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(settings.CheckIntervalMinutes)
        };
        _notificationTimer.Tick += async (s, e) => await UpdateNotificationsAsync();
        _notificationTimer.Start();

        NotificationService.TaskUpdated += OnTaskUpdated;

        Services.NetworkDatabaseMonitor.Instance.ConnectionLost     += OnDatabaseConnectionLost;
        Services.NetworkDatabaseMonitor.Instance.ConnectionRestored += OnDatabaseConnectionRestored;

        Loaded += async (s, e) => await UpdateNotificationsAsync();
    }

    private void OnDatabaseConnectionLost()
    {
        Dispatcher.Invoke(() =>
        {
            IsEnabled = false;

            _reconnectingWindow = new Views.DatabaseReconnectingWindow();
            _reconnectingWindow.Show();
        });
    }

    private void OnDatabaseConnectionRestored()
    {
        Dispatcher.Invoke(() =>
        {
            IsEnabled = true;

            if (_reconnectingWindow != null)
            {
                var isHidden = _reconnectingWindow.IsMinimizedToTray;
                _reconnectingWindow.CloseAfterReconnect();
                _reconnectingWindow = null;

                if (isHidden)
                {
                    _trayIconService?.ShowBalloonTip(
                        "База данных подключена",
                        "Соединение с базой данных восстановлено.");
                }
            }
        });
    }

    /// <summary>
    /// Обработчик события обновления заданий - автоматически обновляет бейджи и счетчики
    /// </summary>
    private void OnTaskUpdated()
    {
        _ = Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await _notificationService.GenerateNotificationsAsync();
                
                await _trayIconService.UpdateBadgeAsync();
                
                NotificationsViewModel.RaiseNotificationsCountChanged();
            }
            catch (Exception)
            {
            }
        });
    }

    private async System.Threading.Tasks.Task UpdateNotificationsAsync()
    {
        try
        {
            using (var freshContext = new ControlsDbContext())
            {
                var freshNotificationService = new NotificationService(freshContext);
                
                await freshNotificationService.GenerateNotificationsAsync();
                
                await freshNotificationService.SendOsNotificationsAsync();
                
                _unprocessedCount = await freshNotificationService.GetUnprocessedCountAsync();
            }
            
            await _trayIconService.UpdateBadgeAsync();
            
            NotificationsViewModel.RaiseNotificationsCountChanged();
        }
        catch
        {
        }
    }

    private async System.Threading.Tasks.Task UpdateTrayIconBadgeAsync()
    {
        if (_trayIconService != null)
            await _trayIconService.UpdateBadgeAsync();
    }

    private void Window_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            
            var trayIcon = (Hardcodet.Wpf.TaskbarNotification.TaskbarIcon)this.Resources["TrayIcon"];
            trayIcon.ShowBalloonTip("Задачи", 
                "Приложение свернуто в трей", 
                Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }
    }

    private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowMainWindow();
    }

    private void ShowWindow_Click(object sender, RoutedEventArgs e)
    {
        ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (_isDisposed) return;
        
        try
        {
            if (_notificationTimer != null)
            {
                _notificationTimer.Stop();
            }
            
            NotificationService.TaskUpdated -= OnTaskUpdated;

            Services.NetworkDatabaseMonitor.Instance.ConnectionLost     -= OnDatabaseConnectionLost;
            Services.NetworkDatabaseMonitor.Instance.ConnectionRestored -= OnDatabaseConnectionRestored;
            
            _trayIconService?.Dispose();
            
            var trayIcon = this.Resources["TrayIcon"] as Hardcodet.Wpf.TaskbarNotification.TaskbarIcon;
            trayIcon?.Dispose();
            
            if (DataContext is IDisposable disposableDataContext)
            {
                disposableDataContext.Dispose();
            }
            
            _context?.Dispose();
            
            _isDisposed = true;
        }
        catch
        {
        }
        finally
        {
            Application.Current.Shutdown();
        }
    }
}
