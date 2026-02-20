using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;
using Controls.Data;

namespace Controls.Views
{
    public partial class DatabaseReconnectingWindow : Window
    {
        private readonly DispatcherTimer _elapsedTimer = new();
        private readonly DispatcherTimer _attemptTimer = new();
        private readonly DateTime _startTime = DateTime.Now;
        private int _attemptNumber = 1;
        private bool _isShuttingDown = false;

        /// <summary>
        /// true когда окно скрыто (свёрнуто в трей)
        /// </summary>
        public bool IsMinimizedToTray { get; private set; } = false;

        public DatabaseReconnectingWindow()
        {
            InitializeComponent();

            DbPathTextBlock.Text = DatabaseConfiguration.GetDatabasePath();

            _elapsedTimer.Interval = TimeSpan.FromSeconds(1);
            _elapsedTimer.Tick += ElapsedTimer_Tick;
            _elapsedTimer.Start();

            _attemptTimer.Interval = TimeSpan.FromSeconds(5);
            _attemptTimer.Tick += AttemptTimer_Tick;
            _attemptTimer.Start();
        }

        private void ElapsedTimer_Tick(object? sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _startTime;
            TimerTextBlock.Text = $"Время ожидания: {(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}";
        }

        private void AttemptTimer_Tick(object? sender, EventArgs e)
        {
            _attemptNumber++;
            AttemptTextBlock.Text = $"Попытка {_attemptNumber}...";
        }

        /// <summary>
        /// Вызывается когда соединение восстановлено.
        /// Показывает сообщение об успехе и закрывает окно.
        /// </summary>
        public void CloseAfterReconnect()
        {
            Dispatcher.Invoke(() =>
            {
                _isShuttingDown = true;
                _elapsedTimer.Stop();
                _attemptTimer.Stop();

                if (!IsVisible)
                {
                    Show();
                    Topmost = true;
                }

                StatusTextBlock.Text = "✅ Соединение с базой данных восстановлено!";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                SpinnerEllipse.Stroke = System.Windows.Media.Brushes.Green;
                MinimizeButton.IsEnabled = false;
                CloseAppButton.IsEnabled = false;

                var closeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                closeTimer.Tick += (s, ev) =>
                {
                    closeTimer.Stop();
                    Close();
                };
                closeTimer.Start();
            });
        }

        private void MinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            IsMinimizedToTray = true;
            Hide();
        }

        private void CloseApp_Click(object sender, RoutedEventArgs e)
        {
            _isShuttingDown = true;
            _elapsedTimer.Stop();
            _attemptTimer.Stop();
            Application.Current.Shutdown();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isShuttingDown)
            {
                e.Cancel = true;
                IsMinimizedToTray = true;
                Hide();
                return;
            }

            _elapsedTimer.Stop();
            _attemptTimer.Stop();
            base.OnClosing(e);
        }
    }
}
