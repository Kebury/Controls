using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Controls.Data;
using Controls.Models;
using Controls.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace Controls.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ControlsDbContext _dbContext;
        private readonly bool _ownsContext;
        private bool _isDarkTheme;
        private bool _isAutoStartEnabled;

        private int _checkIntervalMinutes;
        private int _dueTomorrowIntervalMinutes;
        private int _dueTodayIntervalMinutes;
        private int _overdueIntervalMinutes;

        private string _organizationName = "Организация";

        /// <summary>
        /// Статическое событие — уведомляет приложение об изменении наименования организации
        /// </summary>
        public static event Action<string>? OrganizationNameChanged;

        public ObservableCollection<Executor> Executors { get; } = new();
        public ObservableCollection<Department> Departments { get; } = new();

        public ICommand AddExecutorCommand { get; }
        public ICommand EditExecutorCommand { get; }
        public ICommand DeleteExecutorCommand { get; }
        public ICommand AddDepartmentCommand { get; }
        public ICommand EditDepartmentCommand { get; }
        public ICommand DeleteDepartmentCommand { get; }
        public ICommand ShowCacheInfoCommand { get; }
        public ICommand ShowDatabaseInfoCommand { get; }
        public ICommand CreateBackupCommand { get; }
        public ICommand ExportDatabaseCommand { get; }
        public ICommand CreateNewDatabaseCommand { get; }
        public ICommand ConnectToDatabaseCommand { get; }
        public ICommand ResetDatabasePathCommand { get; }
        public ICommand ToggleThemeCommand { get; }
        public ICommand ToggleAutoStartCommand { get; }

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set
            {
                if (SetProperty(ref _isDarkTheme, value))
                {
                    var theme = value ? ThemeService.Theme.Dark : ThemeService.Theme.Light;
                    ThemeService.ApplyTheme(theme);
                }
            }
        }

        public bool IsAutoStartEnabled
        {
            get => _isAutoStartEnabled;
            set
            {
                if (SetProperty(ref _isAutoStartEnabled, value))
                {
                    SetAutoStart(value);
                }
            }
        }

        /// <summary>
        /// Интервал проверки уведомлений (в минутах)
        /// </summary>
        public int CheckIntervalMinutes
        {
            get => _checkIntervalMinutes;
            set
            {
                if (SetProperty(ref _checkIntervalMinutes, value))
                {
                    SaveNotificationSettings();
                }
            }
        }

        /// <summary>
        /// Интервал для уведомлений "Срок исполнения завтра" (в минутах)
        /// </summary>
        public int DueTomorrowIntervalMinutes
        {
            get => _dueTomorrowIntervalMinutes;
            set
            {
                if (SetProperty(ref _dueTomorrowIntervalMinutes, value))
                {
                    OnPropertyChanged(nameof(DueTomorrowIntervalHours));
                    SaveNotificationSettings();
                }
            }
        }

        /// <summary>
        /// Интервал для уведомлений "Срок исполнения завтра" в часах (вычисляемое свойство)
        /// </summary>
        public double DueTomorrowIntervalHours => _dueTomorrowIntervalMinutes / 60.0;

        /// <summary>
        /// Интервал для уведомлений "Срок исполнения сегодня" (в минутах)
        /// </summary>
        public int DueTodayIntervalMinutes
        {
            get => _dueTodayIntervalMinutes;
            set
            {
                if (SetProperty(ref _dueTodayIntervalMinutes, value))
                {
                    SaveNotificationSettings();
                }
            }
        }

        /// <summary>
        /// Интервал для уведомлений "Просрочено" (в минутах, фиксировано 15)
        /// </summary>
        public int OverdueIntervalMinutes
        {
            get => _overdueIntervalMinutes;
            set => SetProperty(ref _overdueIntervalMinutes, value);
        }

        /// <summary>
        /// Наименование организации (отображается в заголовке и навигации)
        /// </summary>
        public string OrganizationName
        {
            get => _organizationName;
            set
            {
                if (SetProperty(ref _organizationName, value))
                {
                    SaveOrganizationName();
                }
            }
        }

        public SettingsViewModel()
        {
            _dbContext = new ControlsDbContext();
            _ownsContext = true;

            AddExecutorCommand = new RelayCommand(_ => AddExecutor());
            EditExecutorCommand = new RelayCommand(executor => EditExecutor(executor as Executor));
            DeleteExecutorCommand = new RelayCommand(executor => DeleteExecutor(executor as Executor));
            AddDepartmentCommand = new RelayCommand(_ => AddDepartment());
            EditDepartmentCommand = new RelayCommand(department => EditDepartment(department as Department));
            DeleteDepartmentCommand = new RelayCommand(department => DeleteDepartment(department as Department));
            ShowCacheInfoCommand = new RelayCommand(_ => ShowCacheInfo());
            ShowDatabaseInfoCommand = new RelayCommand(_ => ShowDatabaseInfo());
            CreateBackupCommand = new RelayCommand(_ => CreateBackup());
            ExportDatabaseCommand = new RelayCommand(_ => ExportDatabase());
            CreateNewDatabaseCommand = new RelayCommand(_ => CreateNewDatabase());
            ConnectToDatabaseCommand = new RelayCommand(_ => ConnectToDatabase());
            ResetDatabasePathCommand = new RelayCommand(_ => ResetDatabasePath());
            ToggleThemeCommand = new RelayCommand(_ => IsDarkTheme = !IsDarkTheme);
            ToggleAutoStartCommand = new RelayCommand(_ => IsAutoStartEnabled = !IsAutoStartEnabled);

            _isDarkTheme = ThemeService.GetCurrentTheme() == ThemeService.Theme.Dark;
            
            _isAutoStartEnabled = IsAutoStartSet();

            LoadExecutors();
            LoadDepartments();
            LoadNotificationSettings();
        }

        public SettingsViewModel(ControlsDbContext dbContext)
        {
            _dbContext = dbContext;
            _ownsContext = false;

            AddExecutorCommand = new RelayCommand(_ => AddExecutor());
            EditExecutorCommand = new RelayCommand(executor => EditExecutor(executor as Executor));
            DeleteExecutorCommand = new RelayCommand(executor => DeleteExecutor(executor as Executor));
            AddDepartmentCommand = new RelayCommand(_ => AddDepartment());
            EditDepartmentCommand = new RelayCommand(department => EditDepartment(department as Department));
            DeleteDepartmentCommand = new RelayCommand(department => DeleteDepartment(department as Department));
            ShowCacheInfoCommand = new RelayCommand(_ => ShowCacheInfo());
            ShowDatabaseInfoCommand = new RelayCommand(_ => ShowDatabaseInfo());
            CreateBackupCommand = new RelayCommand(_ => CreateBackup());
            ExportDatabaseCommand = new RelayCommand(_ => ExportDatabase());
            CreateNewDatabaseCommand = new RelayCommand(_ => CreateNewDatabase());
            ConnectToDatabaseCommand = new RelayCommand(_ => ConnectToDatabase());
            ResetDatabasePathCommand = new RelayCommand(_ => ResetDatabasePath());
            ToggleThemeCommand = new RelayCommand(_ => IsDarkTheme = !IsDarkTheme);
            ToggleAutoStartCommand = new RelayCommand(_ => IsAutoStartEnabled = !IsAutoStartEnabled);

            _isDarkTheme = ThemeService.GetCurrentTheme() == ThemeService.Theme.Dark;
            
            _isAutoStartEnabled = IsAutoStartSet();

            LoadExecutors();
            LoadDepartments();
            LoadNotificationSettings();
        }

        private void LoadExecutors()
        {
            try
            {
                foreach (var entry in _dbContext.ChangeTracker.Entries<Executor>().ToList())
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;

                Executors.Clear();
                var executors = _dbContext.Executors.OrderBy(e => e.FullName).ToList();
                foreach (var executor in executors)
                {
                    Executors.Add(executor);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки исполнителей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDepartments()
        {
            try
            {
                foreach (var entry in _dbContext.ChangeTracker.Entries<Department>().ToList())
                    entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;

                Departments.Clear();
                var departments = _dbContext.Departments.OrderBy(d => d.ShortName).ToList();
                foreach (var department in departments)
                {
                    Departments.Add(department);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки организаций: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddExecutor()
        {
            var dialog = new Views.AddExecutorWindow();
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var executor = new Executor
                    {
                        Position = dialog.Position,
                        FullName = dialog.FullName
                    };

                    _dbContext.Executors.Add(executor);
                    _dbContext.SaveChanges();
                    
                    LoadExecutors();
                    MessageBox.Show("Исполнитель успешно добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления исполнителя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteExecutor(Executor? executor)
        {
            if (executor == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить исполнителя\n{executor.FullName}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Executors.Remove(executor);
                    _dbContext.SaveChanges();
                    LoadExecutors();
                    MessageBox.Show("Исполнитель успешно удалён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления исполнителя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditExecutor(Executor? executor)
        {
            if (executor == null) return;

            var dialog = new Views.AddExecutorWindow(executor);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _dbContext.Executors.Update(executor);
                    _dbContext.SaveChanges();
                    LoadExecutors();
                    MessageBox.Show("Исполнитель успешно обновлён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления исполнителя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddDepartment()
        {
            var dialog = new Views.AddDepartmentWindow();
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var department = new Department
                    {
                        FullName = dialog.FullName,
                        ShortName = dialog.ShortName
                    };

                    _dbContext.Departments.Add(department);
                    _dbContext.SaveChanges();
                    
                    LoadDepartments();
                    MessageBox.Show("Отдел успешно добавлен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления отдела: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteDepartment(Department? department)
        {
            if (department == null) return;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить отдел\n{department.FullName}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _dbContext.Departments.Remove(department);
                    _dbContext.SaveChanges();
                    LoadDepartments();
                    MessageBox.Show("Отдел успешно удалён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка удаления отдела: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditDepartment(Department? department)
        {
            if (department == null) return;

            var dialog = new Views.AddDepartmentWindow(department);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _dbContext.Departments.Update(department);
                    _dbContext.SaveChanges();
                    LoadDepartments();
                    MessageBox.Show("Отдел успешно обновлён", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка обновления отдела: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ShowCacheInfo()
        {
            try
            {
                var tempPath = Path.GetTempPath();
                var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Controls.TaskManager");
                
                var cacheInfo = $"Расположение временных файлов:\n{tempPath}\n\n";
                cacheInfo += $"Расположение данных приложения:\n{appDataPath}\n\n";
                
                if (Directory.Exists(appDataPath))
                {
                    var files = Directory.GetFiles(appDataPath, "*", SearchOption.AllDirectories);
                    long totalSize = files.Sum(f => new FileInfo(f).Length);
                    cacheInfo += $"Файлов в кэше: {files.Length}\n";
                    cacheInfo += $"Общий размер: {FormatFileSize(totalSize)}";
                }
                else
                {
                    cacheInfo += "Кэш пуст";
                }

                MessageBox.Show(cacheInfo, "Информация о кэше", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения информации о кэше: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowDatabaseInfo()
        {
            try
            {
                var dbPath = DatabaseConfiguration.GetDatabasePath();
                var savedPath = DatabaseConfiguration.GetSavedDatabasePath();
                
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("Файл базы данных не найден", "Информация", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var fileInfo = new FileInfo(dbPath);
                var isNetworkPath = dbPath.StartsWith(@"\\");
                var pathType = isNetworkPath ? "🌐 Сетевой" : "💻 Локальный";
                
                var info = $"{pathType} путь к БД\n\n";
                info += $"Файл: {fileInfo.Name}\n\n";
                info += $"Расположение:\n{fileInfo.DirectoryName}\n\n";
                info += $"Размер: {FormatFileSize(fileInfo.Length)}\n\n";
                info += $"Последнее изменение:\n{fileInfo.LastWriteTime:dd.MM.yyyy HH:mm:ss}\n\n";
                
                if (savedPath != null)
                {
                    info += "\n✅ Используется настроенный путь\n(см. настройки для сброса)";
                }
                else
                {
                    info += "\nℹ️ Используется путь по умолчанию";
                }

                MessageBox.Show(info, "Информация о базе данных", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка получения информации о БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateBackup()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "SQLite Database|*.db|All files|*.*",
                    FileName = $"Controls_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                    Title = "Сохранить резервную копию базы данных"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var sourcePath = DatabaseConfiguration.GetDatabasePath();
                    File.Copy(sourcePath, saveDialog.FileName, true);
                    MessageBox.Show($"Резервная копия успешно создана:\n{saveDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания резервной копии: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportDatabase()
        {
            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "SQLite Database|*.db|All files|*.*",
                    FileName = $"Controls_Export_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                    Title = "Экспорт базы данных"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var sourcePath = DatabaseConfiguration.GetDatabasePath();
                    File.Copy(sourcePath, saveDialog.FileName, true);
                    MessageBox.Show($"База данных успешно экспортирована:\n{saveDialog.FileName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка экспорта базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateNewDatabase()
        {
            var result = MessageBox.Show(
                "Создание новой базы данных заменит текущую базу.\nВсе существующие данные будут потеряны.\n\nПродолжить?",
                "Предупреждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var saveDialog = new SaveFileDialog
                {
                    Filter = "SQLite Database|*.db|All files|*.*",
                    FileName = "controls.db",
                    Title = "Создать новую базу данных"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    var connectionString = $"Data Source={saveDialog.FileName}";
                    var optionsBuilder = new DbContextOptionsBuilder<ControlsDbContext>();
                    optionsBuilder.UseSqlite(connectionString);

                    using (var context = new ControlsDbContext(optionsBuilder.Options))
                    {
                        context.Database.EnsureCreated();
                    }

                    MessageBox.Show(
                        $"Новая база данных успешно создана:\n{saveDialog.FileName}\n\nПерезапустите приложение для использования новой базы данных.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка создания новой базы данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConnectToDatabase()
        {
            try
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = "SQLite Database|*.db|All files|*.*",
                    Title = "Выбрать базу данных для подключения (локальную или сетевую)"
                };

                if (openDialog.ShowDialog() == true)
                {
                    var selectedPath = openDialog.FileName;
                    
                    try
                    {
                        var connectionString = $"Data Source={selectedPath}";
                        var optionsBuilder = new DbContextOptionsBuilder<ControlsDbContext>();
                        optionsBuilder.UseSqlite(connectionString);

                        using (var context = new ControlsDbContext(optionsBuilder.Options))
                        {
                            if (!context.Database.CanConnect())
                            {
                                throw new Exception("Не удалось подключиться к базе данных");
                            }
                            
                            var tables = context.ControlTasks.Take(1).ToList();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка проверки базы данных:\n{ex.Message}\n\nУбедитесь, что выбран правильный файл SQLite базы данных.",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        return;
                    }

                    try
                    {
                        DatabaseConfiguration.SetDatabasePath(selectedPath);
                        
                        DatabaseConfiguration.ConfigureForNetworkAccess(selectedPath);
                        
                        var isNetworkPath = selectedPath.StartsWith(@"\\");
                        var pathType = isNetworkPath ? "Сетевой" : "Локальный";
                        
                        var result = MessageBox.Show(
                            $"✅ База данных успешно подключена!\n\n" +
                            $"{pathType} путь:\n{selectedPath}\n\n" +
                            $"БД настроена для многопользовательского доступа (WAL режим).\n\n" +
                            $"Настройки сохранены. Перезапустить приложение сейчас?",
                            "Успех",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            System.Diagnostics.Process.Start(
                                System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName);
                            System.Windows.Application.Current.Shutdown();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Ошибка сохранения настроек:\n{ex.Message}",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetDatabasePath()
        {
            var savedPath = DatabaseConfiguration.GetSavedDatabasePath();
            
            if (savedPath == null)
            {
                MessageBox.Show(
                    "Используется путь по умолчанию.\nНичего не нужно сбрасывать.",
                    "Информация",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }
            
            var result = MessageBox.Show(
                $"Текущий путь:\n{savedPath}\n\n" +
                "Сбросить на путь по умолчанию?\n" +
                "Приложение будет перезапущено.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    DatabaseConfiguration.ResetDatabasePath();
                    
                    MessageBox.Show(
                        "Путь к БД сброшен на значение по умолчанию.\nПриложение будет перезапущено.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    
                    System.Diagnostics.Process.Start(
                        System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName);
                    System.Windows.Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Ошибка сброса пути: {ex.Message}",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private bool IsAutoStartSet()
        {
            // Проверяем реестровую запись Run (через настройки приложения)
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                if (key?.GetValue("Controls.TaskManager") != null)
                    return true;
            }
            catch { }

            // Проверяем ярлык в папке автозагрузки (может быть создан установщиком)
            try
            {
                var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                var shortcutPath = System.IO.Path.Combine(startupFolder, "Controls.lnk");
                if (System.IO.File.Exists(shortcutPath))
                    return true;
            }
            catch { }

            return false;
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key == null)
                {
                    MessageBox.Show("Не удалось получить доступ к реестру", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (enable)
                {
                    var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue("Controls.TaskManager", $"\"{exePath}\" --minimized");
                        MessageBox.Show("Автозапуск включен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Удаляем запись реестра
                    key.DeleteValue("Controls.TaskManager", false);

                    // Также удаляем ярлык из папки автозагрузки, если он был создан установщиком
                    try
                    {
                        var startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                        var shortcutPath = System.IO.Path.Combine(startupFolder, "Controls.lnk");
                        if (System.IO.File.Exists(shortcutPath))
                            System.IO.File.Delete(shortcutPath);
                    }
                    catch { }

                    MessageBox.Show("Автозапуск отключен", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка настройки автозапуска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _isAutoStartEnabled = !enable;
                OnPropertyChanged(nameof(IsAutoStartEnabled));
            }
        }

        /// <summary>
        /// Загрузка настроек частоты уведомлений из базы данных
        /// </summary>
        private void LoadNotificationSettings()
        {
            try
            {
                var settings = _dbContext.GetAppSettings();
                _checkIntervalMinutes = settings.CheckIntervalMinutes;
                _dueTomorrowIntervalMinutes = settings.DueTomorrowIntervalMinutes;
                _dueTodayIntervalMinutes = settings.DueTodayIntervalMinutes;
                _overdueIntervalMinutes = settings.OverdueIntervalMinutes;
                _organizationName = string.IsNullOrWhiteSpace(settings.OrganizationName)
                    ? "Организация" : settings.OrganizationName;

                OnPropertyChanged(nameof(CheckIntervalMinutes));
                OnPropertyChanged(nameof(DueTomorrowIntervalMinutes));
                OnPropertyChanged(nameof(DueTomorrowIntervalHours));
                OnPropertyChanged(nameof(DueTodayIntervalMinutes));
                OnPropertyChanged(nameof(OverdueIntervalMinutes));
                OnPropertyChanged(nameof(OrganizationName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек уведомлений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сохранение наименования организации без диалогового сообщения
        /// </summary>
        private void SaveOrganizationName()
        {
            try
            {
                var settings = _dbContext.GetAppSettings();
                settings.OrganizationName = _organizationName;
                _dbContext.SaveChanges();
                OrganizationNameChanged?.Invoke(_organizationName);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения наименования организации: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сохранение настроек частоты уведомлений в базу данных
        /// </summary>
        private void SaveNotificationSettings()
        {
            try
            {
                var settings = _dbContext.GetAppSettings();
                settings.CheckIntervalMinutes = _checkIntervalMinutes;
                settings.DueTomorrowIntervalMinutes = _dueTomorrowIntervalMinutes;
                settings.DueTodayIntervalMinutes = _dueTodayIntervalMinutes;
                settings.OverdueIntervalMinutes = _overdueIntervalMinutes;

                _dbContext.SaveChanges();

                MessageBox.Show(
                    "Настройки сохранены.\n⚠️ Для применения новой частоты проверки уведомлений требуется перезапуск приложения.",
                    "Настройки сохранены",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Освобождение ресурсов (DbContext, если владеем им)
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_ownsContext)
                {
                    _dbContext?.Dispose();
                }
            }
            
            base.Dispose(disposing);
        }
    }
}
