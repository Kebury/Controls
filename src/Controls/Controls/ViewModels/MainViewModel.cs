using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Controls.Data;
using Controls.Helpers;
using Controls.Models;
using Microsoft.EntityFrameworkCore;

namespace Controls.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ControlsDbContext _dbContext;
        private object? _currentView;
        private int _unreadNotificationsCount;
        private string _organizationName = "Организация";
        
        private Action? _notificationsCountChangedHandler;
        
        private TasksViewModel? _tasksViewModel;
        private DepartmentTasksViewModel? _departmentTasksViewModel;
        private NotificationsViewModel? _notificationsViewModel;
        private CalendarViewModel? _calendarViewModel;
        private ArchiveViewModel? _archiveViewModel;
        private SettingsViewModel? _settingsViewModel;

        public MainViewModel()
        {
            _dbContext = new ControlsDbContext();
            
            InitializeDatabase();

            try
            {
                var appSettings = _dbContext.GetAppSettings();
                _organizationName = string.IsNullOrWhiteSpace(appSettings.OrganizationName)
                    ? "Организация" : appSettings.OrganizationName;
            }
            catch { }

            LoadTasks();
            _ = LoadNotificationsAsync();
            
            _notificationsCountChangedHandler = async () => await LoadNotificationsAsync();
            NotificationsViewModel.NotificationsCountChanged += _notificationsCountChangedHandler;

            SettingsViewModel.OrganizationNameChanged += name =>
            {
                OrganizationName = name;
            };

            ShowTasksCommand = new RelayCommand(_ => CurrentView = GetTasksViewModel());
            ShowDepartmentTasksCommand = new RelayCommand(_ => CurrentView = GetDepartmentTasksViewModel());
            ShowNotificationsCommand = new RelayCommand(_ => CurrentView = GetNotificationsViewModel());
            ShowCalendarCommand = new RelayCommand(_ => CurrentView = GetCalendarViewModel());
            ShowDocumentsCommand = new RelayCommand(_ => CurrentView = GetArchiveViewModel());
            ShowSettingsCommand = new RelayCommand(_ => CurrentView = GetSettingsViewModel());
            AddTaskCommand = new RelayCommand(_ => AddNewTask());

            CurrentView = GetTasksViewModel();
        }
        
        private TasksViewModel GetTasksViewModel()
        {
            if (_tasksViewModel == null)
            {
                _tasksViewModel = new TasksViewModel(_dbContext);
            }
            return _tasksViewModel;
        }
        
        private DepartmentTasksViewModel GetDepartmentTasksViewModel()
        {
            if (_departmentTasksViewModel == null)
            {
                _departmentTasksViewModel = new DepartmentTasksViewModel(_dbContext);
            }
            return _departmentTasksViewModel;
        }
        
        private NotificationsViewModel GetNotificationsViewModel()
        {
            if (_notificationsViewModel == null)
            {
                _notificationsViewModel = new NotificationsViewModel();
            }
            return _notificationsViewModel;
        }
        
        private CalendarViewModel GetCalendarViewModel()
        {
            if (_calendarViewModel == null)
            {
                _calendarViewModel = new CalendarViewModel(_dbContext);
            }
            return _calendarViewModel;
        }
        
        private ArchiveViewModel GetArchiveViewModel()
        {
            if (_archiveViewModel == null)
            {
                _archiveViewModel = new ArchiveViewModel(_dbContext);
            }
            return _archiveViewModel;
        }
        
        private SettingsViewModel GetSettingsViewModel()
        {
            if (_settingsViewModel == null)
            {
                _settingsViewModel = new SettingsViewModel(_dbContext);
            }
            return _settingsViewModel;
        }

        /// <summary>
        /// Освобождение ресурсов (DbContext, ViewModels и отписка от событий)
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_notificationsCountChangedHandler != null)
                {
                    NotificationsViewModel.NotificationsCountChanged -= _notificationsCountChangedHandler;
                }
                
                _tasksViewModel?.Dispose();
                _departmentTasksViewModel?.Dispose();
                _notificationsViewModel?.Dispose();
                _calendarViewModel?.Dispose();
                _archiveViewModel?.Dispose();
                _settingsViewModel?.Dispose();
                
                _dbContext?.Dispose();
            }
            
            base.Dispose(disposing);
        }

        public ObservableCollection<ControlTask> Tasks { get; } = new();
        public ObservableCollection<Notification> Notifications { get; } = new();

        public object? CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public int UnreadNotificationsCount
        {
            get => _unreadNotificationsCount;
            set => SetProperty(ref _unreadNotificationsCount, value);
        }

        public string OrganizationName
        {
            get => _organizationName;
            set
            {
                if (SetProperty(ref _organizationName, value))
                {
                    OnPropertyChanged(nameof(AppTitle));
                }
            }
        }

        /// <summary>
        /// Заголовок окна приложения
        /// </summary>
        public string AppTitle => $"Задачи — {_organizationName}";

        public ICommand ShowTasksCommand { get; }
        public ICommand ShowDepartmentTasksCommand { get; }
        public ICommand ShowNotificationsCommand { get; }
        public ICommand ShowCalendarCommand { get; }
        public ICommand ShowDocumentsCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand AddTaskCommand { get; }

        private void InitializeDatabase()
        {
            try
            {
                var dbPath = DatabaseConfiguration.GetDatabasePath();
                
                var directory = Path.GetDirectoryName(dbPath);
                
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"⚠️ Не удалось создать папку для базы данных:\n{directory}\n\nОшибка: {ex.Message}\n\n" +
                            "Приложение будет работать в ограниченном режиме.",
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }
                
                var dbExists = File.Exists(dbPath);
                
                _dbContext.Database.EnsureCreated();
                
                DatabaseConfiguration.ConfigureForNetworkAccess(dbPath);
                
                EnsureDepartmentTaskDepartmentsTable();
            }
            catch (Microsoft.Data.Sqlite.SqliteException ex)
            {
                var dbPath = DatabaseConfiguration.GetDatabasePath();
                var errorMessage = DatabaseErrorHandler.HandleSqliteException(ex, dbPath);
                DatabaseErrorHandler.ShowDatabaseError(
                    $"{errorMessage}\n\nПриложение будет работать в ограниченном режиме.",
                    "Ошибка инициализации БД");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка инициализации базы данных:\n{ex.Message}\n\n" +
                    "Приложение будет работать в ограниченном режиме.",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EnsureDepartmentTaskDepartmentsTable()
        {
            try
            {
                var connection = _dbContext.Database.GetDbConnection();
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
                
                command.CommandText = @"
                    -- Проверяем есть ли колонка IntermediateResponsesJson
                    SELECT COUNT(*) FROM pragma_table_info('ControlTasks') WHERE name='IntermediateResponsesJson';
                ";
                var exists = Convert.ToInt32(command.ExecuteScalar());
                
                if (exists == 0)
                {
                    command.CommandText = @"
                        ALTER TABLE ControlTasks ADD COLUMN IntermediateResponsesJson TEXT NOT NULL DEFAULT '';
                    ";
                    command.ExecuteNonQuery();
                }
                
                connection.Close();
            }
            catch
            {
            }
        }

        private void LoadTasks()
        {
            try
            {
                Tasks.Clear();
                var tasks = _dbContext.ControlTasks
                    .Include(t => t.Documents)
                    .OrderBy(t => t.DueDate)
                    .ToList();

                foreach (var task in tasks)
                {
                    Tasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки задач: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadNotificationsAsync()
        {
            try
            {
                using var freshContext = new ControlsDbContext();
                
                var notifications = await freshContext.Notifications
                    .Include(n => n.ControlTask)
                    .OrderByDescending(n => n.NotificationDate)
                    .ToListAsync();

                Notifications.Clear();
                foreach (var notification in notifications)
                {
                    Notifications.Add(notification);
                }

                UnreadNotificationsCount = notifications.Count(n => !n.IsProcessed);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки уведомлений: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNewTask()
        {
            MessageBox.Show("Добавление новой задачи - в разработке", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
