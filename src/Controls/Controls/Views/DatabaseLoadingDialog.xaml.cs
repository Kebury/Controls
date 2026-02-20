using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace Controls.Views
{
    /// <summary>
    /// Окно индикатора загрузки при ожидании доступа к БД
    /// </summary>
    public partial class DatabaseLoadingDialog : Window
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isCancelled;

        public bool IsCancelled => _isCancelled;
        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public DatabaseLoadingDialog()
        {
            InitializeComponent();
            _cancellationTokenSource = new CancellationTokenSource();
            _isCancelled = false;
        }

        /// <summary>
        /// Обновить текст статуса
        /// </summary>
        public void UpdateStatus(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = message;
            });
        }

        /// <summary>
        /// Показать успешное подключение
        /// </summary>
        public async Task ShowSuccess()
        {
            await Dispatcher.InvokeAsync(async () =>
            {
                StatusTextBlock.Text = "✅ Подключение установлено";
                
                var animatedDots = this.FindName("AnimatedDots") as System.Windows.Controls.TextBlock;
                if (animatedDots != null)
                {
                    animatedDots.Visibility = Visibility.Collapsed;
                }
                
                var loadingEllipse = this.FindName("LoadingEllipse") as System.Windows.Shapes.Ellipse;
                if (loadingEllipse != null)
                {
                    loadingEllipse.Stroke = System.Windows.Media.Brushes.Green;
                }
                
                await Task.Delay(1000);
                DialogResult = true;
                Close();
            });
        }

        /// <summary>
        /// Показать ошибку
        /// </summary>
        public void ShowError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = $"❌ {message}";
                
                var animatedDots = this.FindName("AnimatedDots") as System.Windows.Controls.TextBlock;
                if (animatedDots != null)
                {
                    animatedDots.Visibility = Visibility.Collapsed;
                }
                
                var loadingEllipse = this.FindName("LoadingEllipse") as System.Windows.Shapes.Ellipse;
                if (loadingEllipse != null)
                {
                    loadingEllipse.Stroke = System.Windows.Media.Brushes.Red;
                }
            });
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _isCancelled = true;
            _cancellationTokenSource.Cancel();
            StatusTextBlock.Text = "Отмена...";
            DialogResult = false;
            Close();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isCancelled && DialogResult != true)
            {
                _isCancelled = true;
                _cancellationTokenSource.Cancel();
            }
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            _cancellationTokenSource?.Dispose();
            base.OnClosed(e);
        }
    }
}
