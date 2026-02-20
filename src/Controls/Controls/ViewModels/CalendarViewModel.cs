using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Controls.Data;
using Controls.Models;
using Microsoft.EntityFrameworkCore;

namespace Controls.ViewModels
{
    public class CalendarViewModel : ViewModelBase
    {
        private readonly ControlsDbContext _dbContext;
        private DateTime _selectedDate = DateTime.Today;
        private DateTime _displayedMonth;

        public CalendarViewModel(ControlsDbContext dbContext)
        {
            _dbContext = dbContext;
            _displayedMonth = DateTime.Today;
            
            LoadTasks();
            GenerateCalendarDays();

            PreviousMonthCommand = new RelayCommand(_ => ChangeMonth(-1));
            NextMonthCommand = new RelayCommand(_ => ChangeMonth(1));
            TodayCommand = new RelayCommand(_ => GoToToday());
            DateClickedCommand = new RelayCommand(day => OnDateClicked(day as CalendarDay));
            OpenMonthYearPickerCommand = new RelayCommand(_ => OpenMonthYearPicker());
        }

        public ObservableCollection<CalendarDay> CalendarDays { get; } = new();
        public ObservableCollection<ControlTask> AllTasks { get; } = new();

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        public DateTime DisplayedMonth
        {
            get => _displayedMonth;
            set
            {
                if (SetProperty(ref _displayedMonth, value))
                {
                    GenerateCalendarDays();
                }
            }
        }

        public string MonthYearDisplay => DisplayedMonth.ToString("MMMM yyyy");

        public ICommand PreviousMonthCommand { get; }
        public ICommand NextMonthCommand { get; }
        public ICommand TodayCommand { get; }
        public ICommand DateClickedCommand { get; }
        public ICommand OpenMonthYearPickerCommand { get; }

        private void LoadTasks()
        {
            try
            {
                AllTasks.Clear();
                var tasks = _dbContext.ControlTasks
                    .Where(t => t.Status != "Исполнено")
                    .OrderBy(t => t.DueDate)
                    .ToList();

                foreach (var task in tasks)
                {
                    AllTasks.Add(task);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка загрузки задач: {ex.Message}", "Ошибка");
            }
        }

        private void GenerateCalendarDays()
        {
            CalendarDays.Clear();

            var firstDayOfMonth = new DateTime(DisplayedMonth.Year, DisplayedMonth.Month, 1);
            var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

            int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
            if (firstDayOfWeek == 0) firstDayOfWeek = 7;

            var previousMonthStart = firstDayOfMonth.AddDays(-(firstDayOfWeek - 1));
            for (int i = 0; i < firstDayOfWeek - 1; i++)
            {
                var date = previousMonthStart.AddDays(i);
                CalendarDays.Add(new CalendarDay(date, false, GetTasksForDate(date)));
            }

            for (int day = 1; day <= lastDayOfMonth.Day; day++)
            {
                var date = new DateTime(DisplayedMonth.Year, DisplayedMonth.Month, day);
                CalendarDays.Add(new CalendarDay(date, true, GetTasksForDate(date)));
            }

            var remainingDays = 42 - CalendarDays.Count;
            for (int i = 1; i <= remainingDays; i++)
            {
                var date = lastDayOfMonth.AddDays(i);
                CalendarDays.Add(new CalendarDay(date, false, GetTasksForDate(date)));
            }

            OnPropertyChanged(nameof(MonthYearDisplay));
        }

        private List<ControlTask> GetTasksForDate(DateTime date)
        {
            var tasks = new List<ControlTask>();
            
            foreach (var task in AllTasks)
            {
                if (!task.IsCyclicTask)
                {
                    if (task.DueDate.Date == date.Date)
                    {
                        tasks.Add(task);
                    }
                }
                else
                {
                    if (IsDateMatchesRecurrence(task, date))
                    {
                        tasks.Add(task);
                    }
                }
            }
            
            return tasks;
        }
        
        /// <summary>
        /// Проверяет, попадает ли указанная дата на цикл повторения задания
        /// </summary>
        private bool IsDateMatchesRecurrence(ControlTask task, DateTime date)
        {
            if (!task.IsCyclicTask)
                return false;
            
            if (date.Date < task.CreatedDate.Date)
                return false;
            
            var daysDiff = (date.Date - task.DueDate.Date).Days;
            
            if (daysDiff < 0)
                return false;
            
            switch (task.TaskType)
            {
                case TaskType.Ежедневное:
                    return true;
                
                case TaskType.Еженедельное:
                    return daysDiff % 7 == 0;
                
                case TaskType.Ежемесячное:
                    return date.Day == task.DueDate.Day;
                
                case TaskType.Ежеквартальное:
                    if (date.Day != task.DueDate.Day)
                        return false;
                    
                    var monthsDiffQuarterly = (date.Year - task.DueDate.Year) * 12 + (date.Month - task.DueDate.Month);
                    return monthsDiffQuarterly >= 0 && monthsDiffQuarterly % 3 == 0;
                
                case TaskType.Полугодовое:
                    if (date.Day != task.DueDate.Day)
                        return false;
                    
                    var monthsDiffHalfYear = (date.Year - task.DueDate.Year) * 12 + (date.Month - task.DueDate.Month);
                    return monthsDiffHalfYear >= 0 && monthsDiffHalfYear % 6 == 0;
                
                case TaskType.Годовое:
                    return date.Month == task.DueDate.Month && date.Day == task.DueDate.Day;
                
                default:
                    return false;
            }
        }

        private void ChangeMonth(int months)
        {
            DisplayedMonth = DisplayedMonth.AddMonths(months);
        }

        private void GoToToday()
        {
            DisplayedMonth = DateTime.Today;
            SelectedDate = DateTime.Today;
        }

        private void OnDateClicked(CalendarDay? day)
        {
            if (day == null || !day.HasTasks) return;

            try
            {
                foreach (var task in day.Tasks)
                {
                }
                
                var tasksWindow = new Views.DateTasksWindow();
                var tasksList = new List<ControlTask>(day.Tasks);
                var viewModel = new DateTasksViewModel(day.Date, tasksList);
                tasksWindow.DataContext = viewModel;
                tasksWindow.Owner = Application.Current.MainWindow;
                tasksWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Ошибка при открытии списка заданий: {ex.Message}",
                    "Ошибка",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private void OpenMonthYearPicker()
        {
            var picker = new Views.MonthYearPickerWindow(DisplayedMonth);
            picker.Owner = Application.Current.MainWindow;
            if (picker.ShowDialog() == true)
            {
                DisplayedMonth = picker.SelectedDate;
                OnPropertyChanged(nameof(MonthYearDisplay));
            }
        }
    }

    public class CalendarDay
    {
        private string? _cachedTooltip;

        public CalendarDay(DateTime date, bool isCurrentMonth, List<ControlTask> tasks)
        {
            Date = date;
            IsCurrentMonth = isCurrentMonth;
            Tasks = new List<ControlTask>(tasks ?? new List<ControlTask>());
            
            if (Tasks.Count > 0)
            {
                foreach (var task in Tasks)
                {
                }
            }
        }

        public DateTime Date { get; }
        public bool IsCurrentMonth { get; }
        public List<ControlTask> Tasks { get; }
        
        public int Day => Date.Day;
        public bool IsToday => Date.Date == DateTime.Today;
        public bool HasTasks => Tasks.Any();
        public int TaskCount => Tasks.Count;
        
        /// <summary>
        /// Есть ли просроченные задания на эту дату
        /// </summary>
        public bool HasOverdueTasks => Tasks.Any(t => 
            t.Status != "Исполнено" && 
            (t.IsCyclicTask ? IsCyclicTaskOverdueForDate(t) : t.DueDate.Date < DateTime.Today)
        );
        
        /// <summary>
        /// Проверяет, просрочено ли циклическое задание для данной даты
        /// </summary>
        private bool IsCyclicTaskOverdueForDate(ControlTask task)
        {
            return Date.Date < DateTime.Today && task.Status != "Исполнено";
        }
        
        public string TasksTooltip
        {
            get
            {
                if (_cachedTooltip != null)
                {
                    return _cachedTooltip;
                }

                if (!HasTasks)
                {
                    _cachedTooltip = "на сегодня докладов нет";
                    return _cachedTooltip;
                }
                
                var taskLines = new System.Text.StringBuilder();
                for (int i = 0; i < Tasks.Count; i++)
                {
                    var task = Tasks[i];
                    taskLines.AppendLine($"• {task.Title} ({task.TaskTypeDisplay})");
                    taskLines.AppendLine($"  Срок: {task.DueDate:HH:mm}");
                    if (i < Tasks.Count - 1)
                        taskLines.AppendLine();
                }
                _cachedTooltip = taskLines.ToString().TrimEnd();
                return _cachedTooltip;
            }
        }
    }
}
