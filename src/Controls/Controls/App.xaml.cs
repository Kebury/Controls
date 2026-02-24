using System;
using System.Configuration;
using System.Data;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Controls.Services;
using Controls.Data;
using Controls.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Controls;

public partial class App : Application
{
    private const string MutexName = "Controls.TaskManager.SingleInstance";

    private static Mutex? _mutex;
    private static bool _mutexOwned = false;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    /// <summary>
    /// Находит главное окно уже запущенного экземпляра приложения и выводит его на передний план.
    /// </summary>
    private static void BringExistingInstanceToFront()
    {
        try
        {
            var currentId = Process.GetCurrentProcess().Id;
            var currentName = Process.GetCurrentProcess().ProcessName;

            foreach (var proc in Process.GetProcessesByName(currentName))
            {
                if (proc.Id == currentId)
                    continue;

                var hWnd = proc.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                    continue;

                if (IsIconic(hWnd))
                    ShowWindow(hWnd, SW_RESTORE);

                SetForegroundWindow(hWnd);
                break;
            }
        }
        catch
        {
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        bool createdNew;
        try
        {
            _mutex = new Mutex(true, MutexName, out createdNew);
        }
        catch (AbandonedMutexException)
        {
            // Предыдущий экземпляр аварийно завершился (например, убит через диспетчер задач).
            // ОС автоматически передал нам владение мьютексом — продолжаем как единственный экземпляр.
            createdNew = true;
        }

        if (!createdNew)
        {
            // Другой экземпляр уже запущен — выводим его на передний план и завершаемся
            _mutex?.Dispose();
            _mutex = null;
            BringExistingInstanceToFront();
            Shutdown();
            return;
        }

        _mutexOwned = true;
        
        RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;
        
        ThemeService.LoadSavedTheme();
        
        try
        {
            RegisterAppForNotifications();
        }
        catch (Exception)
        {
        }
        
        try
        {
            EnsureStartMenuShortcut();
        }
        catch (Exception)
        {
        }
        
        try
        {
            CheckNotificationSettings();
        }
        catch (Exception)
        {
        }
        
        try
        {
            using (var context = new ControlsDbContext())
            {
                var dbPath = DatabaseConfiguration.GetDatabasePath();
                
                var pendingMigrations = context.Database.GetPendingMigrations().ToList();
                
                if (pendingMigrations.Any())
                {
                    try
                    {
                        var backupPath = CreateDatabaseBackup(dbPath);
                        
                        var result = MessageBox.Show(
                            $"Обнаружены обновления структуры базы данных ({pendingMigrations.Count} миграций).\n\n" +
                            $"Резервная копия создана:\n{backupPath}\n\n" +
                            "Применить обновления?\n\n" +
                            "⚠️ Не рекомендуется отказываться от обновления - это может привести к ошибкам в работе приложения.",
                            "Обновление базы данных",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            context.Database.Migrate();
                            
                            MessageBox.Show(
                                "✅ База данных успешно обновлена!\n\n" +
                                "Все данные сохранены.",
                                "Обновление завершено",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show(
                                "⚠️ Обновление отменено.\n\n" +
                                "Приложение может работать нестабильно с устаревшей версией БД.",
                                "Предупреждение",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"⚠️ Не удалось создать резервную копию базы данных.\n\n" +
                            $"Ошибка: {ex.Message}\n\n" +
                            "Миграции будут применены без резервного копирования.",
                            "Предупреждение",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        
                        context.Database.Migrate();
                    }
                }
                else
                {
                    context.Database.Migrate();
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"❌ Ошибка при обновлении базы данных:\n\n{ex.Message}\n\n" +
                "Приложение может работать нестабильно. Обратитесь к администратору.",
                "Ошибка базы данных",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        
        try
        {
            using (var context = new ControlsDbContext())
            {
                var tasksToUpdate = context.ControlTasks.Where(t => t.Status == "В работе").ToList();
                if (tasksToUpdate.Any())
                {
                    foreach (var task in tasksToUpdate)
                    {
                        task.Status = "На исполнении";
                    }
                    context.SaveChanges();
                }
                
                var completedTasksToUpdate = context.ControlTasks.Where(t => t.Status == "Выполнено").ToList();
                if (completedTasksToUpdate.Any())
                {
                    foreach (var task in completedTasksToUpdate)
                    {
                        task.Status = "Исполнено";
                    }
                    context.SaveChanges();
                }
            }
        }
        catch (Exception)
        {
        }

        Services.NetworkDatabaseMonitor.Instance.Start();
        
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var exception = args.ExceptionObject as Exception;
            MessageBox.Show($"Критическая ошибка:\n{exception?.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show($"Ошибка приложения:\n{args.Exception.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Создаём главное окно вручную (StartupUri удалён, чтобы окно не появлялось
        // до проверки mutex при повторном запуске)
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private static void EnsureDepartmentTaskDepartmentsTable(ControlsDbContext context)
    {
        try
        {
            var connection = context.Database.GetDbConnection();
            connection.Open();
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS DepartmentTaskDepartments (
                    Id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                    DepartmentTaskId INTEGER NOT NULL,
                    DepartmentId INTEGER NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    CompletedDate TEXT NULL,
                    Notes TEXT NOT NULL DEFAULT '',
                    ExecutorId INTEGER NULL,
                    FOREIGN KEY (DepartmentTaskId) REFERENCES DepartmentTasks (Id) ON DELETE CASCADE,
                    FOREIGN KEY (DepartmentId) REFERENCES Departments (Id) ON DELETE RESTRICT
                );
                
                CREATE INDEX IF NOT EXISTS IX_DepartmentTaskDepartments_DepartmentTaskId 
                    ON DepartmentTaskDepartments (DepartmentTaskId);
                CREATE INDEX IF NOT EXISTS IX_DepartmentTaskDepartments_DepartmentId 
                    ON DepartmentTaskDepartments (DepartmentId);
                CREATE INDEX IF NOT EXISTS IX_DepartmentTaskDepartments_IsCompleted 
                    ON DepartmentTaskDepartments (IsCompleted);
            ";
            command.ExecuteNonQuery();
            
            connection.Close();
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Регистрирует приложение в реестре Windows для корректного отображения Toast popup баннеров.
    /// Без этой регистрации Windows принимает уведомления в историю, но НЕ показывает popup.
    /// </summary>
    private static void RegisterAppForNotifications()
    {
        try
        {
            const string appId = "Controls.TaskManager";
            
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }
            
            var registryPath = $@"Software\Classes\AppUserModelId\{appId}";
            
            using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(registryPath, true))
            {
                if (key == null)
                {
                    return;
                }
                
                key.SetValue("DisplayName", "Задачи", Microsoft.Win32.RegistryValueKind.String);
                
                key.SetValue("IconUri", exePath, Microsoft.Win32.RegistryValueKind.String);
                
                key.SetValue("ShowInSettings", 1, Microsoft.Win32.RegistryValueKind.DWord);
                
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Создает ярлык в Start Menu для корректной работы Toast уведомлений
    /// </summary>
    private static void EnsureStartMenuShortcut()
    {
        var startMenuPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Windows\Start Menu\Programs"
        );
        
        var shortcutPath = Path.Combine(startMenuPath, "Controls.lnk");
        
        if (File.Exists(shortcutPath))
        {
            try
            {
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(currentExePath))
                {
                    Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                    if (shellType != null)
                    {
                        dynamic? shell = Activator.CreateInstance(shellType);
                        if (shell != null)
                        {
                            dynamic? existingShortcut = shell.CreateShortcut(shortcutPath);
                            
                            if (existingShortcut != null)
                            {
                                string targetPath = existingShortcut.TargetPath;
                                if (targetPath.Equals(currentExePath, StringComparison.OrdinalIgnoreCase))
                                {
                                    Marshal.ReleaseComObject(existingShortcut);
                                    Marshal.ReleaseComObject(shell);
                                    return;
                                }
                                Marshal.ReleaseComObject(existingShortcut);
                            }
                            
                            Marshal.ReleaseComObject(shell);
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }
        
        try
        {
            var exePath = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(exePath))
            {
                return;
            }
            
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                return;
            }
            
            dynamic? shell = Activator.CreateInstance(shellType);
            if (shell == null)
            {
                return;
            }
            
            dynamic? shortcut = shell.CreateShortcut(shortcutPath);
            if (shortcut == null)
            {
                if (shell != null)
                    Marshal.ReleaseComObject(shell);
                return;
            }
            
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
            shortcut.Description = "Система контроля исполнения заданий";
            shortcut.IconLocation = exePath + ",0";
            
            shortcut.Save();
            
            Marshal.ReleaseComObject(shortcut);
            Marshal.ReleaseComObject(shell);
            
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Проверка настроек уведомлений Windows и вывод предупреждения при необходимости
    /// </summary>
    private static void CheckNotificationSettings()
    {
        try
        {
            var notifier = global::Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier("Controls.TaskManager");
            var setting = notifier.Setting;
            
            switch (setting)
            {
                case global::Windows.UI.Notifications.NotificationSetting.Enabled:
                    break;
                    
                case global::Windows.UI.Notifications.NotificationSetting.DisabledForApplication:
                    ShowNotificationWarningDialog("Уведомления отключены для этого приложения в Windows.\n\n" +
                        "Для включения:\n" +
                        "1. Откройте Параметры Windows\n" +
                        "2. Перейдите в Система → Уведомления\n" +
                        "3. Найдите 'Задачи' и включите уведомления");
                    break;
                    
                case global::Windows.UI.Notifications.NotificationSetting.DisabledForUser:
                    ShowNotificationWarningDialog("Уведомления отключены для вашей учетной записи в Windows.\n\n" +
                        "Для включения откройте Параметры → Система → Уведомления");
                    break;
                    
                case global::Windows.UI.Notifications.NotificationSetting.DisabledByGroupPolicy:
                    ShowNotificationWarningDialog("Уведомления заблокированы групповой политикой системы.\n\n" +
                        "Обратитесь к системному администратору.");
                    break;
                    
                case global::Windows.UI.Notifications.NotificationSetting.DisabledByManifest:
                    break;
                    
                default:
                    break;
            }
        }
        catch (Exception)
        {
        }
    }

    /// <summary>
    /// Показ диалогового окна с предупреждением о настройках уведомлений
    /// </summary>
    private static void ShowNotificationWarningDialog(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            var result = System.Windows.MessageBox.Show(
                message,
                "Настройки уведомлений",
                System.Windows.MessageBoxButton.OKCancel,
                System.Windows.MessageBoxImage.Warning);
            
            if (result == System.Windows.MessageBoxResult.OK)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "ms-settings:notifications",
                        UseShellExecute = true
                    });
                }
                catch (Exception)
                {
                }
            }
        });
    }

    /// <summary>
    /// Создание резервной копии базы данных перед миграцией
    /// </summary>
    private static string CreateDatabaseBackup(string dbPath)
    {
        var dbDirectory = Path.GetDirectoryName(dbPath);
        if (string.IsNullOrEmpty(dbDirectory))
            throw new Exception("Не удалось определить папку базы данных");
        
        var backupDirectory = Path.Combine(dbDirectory, "backups");
        
        if (!Directory.Exists(backupDirectory))
        {
            Directory.CreateDirectory(backupDirectory);
        }
        
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFileName = $"Controls_backup_{timestamp}.db";
        var backupPath = Path.Combine(backupDirectory, backupFileName);
        
        File.Copy(dbPath, backupPath, true);
        
        var walPath = dbPath + "-wal";
        var shmPath = dbPath + "-shm";
        
        if (File.Exists(walPath))
        {
            File.Copy(walPath, backupPath + "-wal", true);
        }
        
        if (File.Exists(shmPath))
        {
            File.Copy(shmPath, backupPath + "-shm", true);
        }
        
        CleanupOldBackups(backupDirectory, 5);
        
        return backupPath;
    }

    /// <summary>
    /// Очистка старых резервных копий (оставляем только заданное количество последних)
    /// </summary>
    private static void CleanupOldBackups(string backupDirectory, int keepCount)
    {
        try
        {
            var backupFiles = Directory.GetFiles(backupDirectory, "Controls_backup_*.db")
                .OrderByDescending(f => File.GetCreationTime(f))
                .ToList();
            
            foreach (var oldBackup in backupFiles.Skip(keepCount))
            {
                try
                {
                    File.Delete(oldBackup);
                    
                    var walFile = oldBackup + "-wal";
                    var shmFile = oldBackup + "-shm";
                    
                    if (File.Exists(walFile))
                        File.Delete(walFile);
                    
                    if (File.Exists(shmFile))
                        File.Delete(shmFile);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Гарантированный выход: если что-то в очистке зависнет, через 5 с процесс всё равно завершится
        var killer = new System.Threading.Thread(() =>
        {
            System.Threading.Thread.Sleep(5000);
            Environment.Exit(0);
        })
        {
            IsBackground = true,
            Name = "ForceExitWatchdog"
        };
        killer.Start();

        try
        {
            Services.NetworkDatabaseMonitor.Instance.Stop();
        }
        catch
        {
        }

        try
        {
            ToastNotificationManagerCompat.Uninstall();
        }
        catch
        {
        }

        if (_mutexOwned)
        {
            try { _mutex?.ReleaseMutex(); } catch { }
        }
        try { _mutex?.Dispose(); } catch { }

        base.OnExit(e);

        Environment.Exit(0);
    }
}
