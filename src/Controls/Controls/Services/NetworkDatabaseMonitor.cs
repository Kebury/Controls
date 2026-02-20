using System;
using System.Threading;
using System.Threading.Tasks;
using Controls.Data;
using Controls.Helpers;

namespace Controls.Services
{
    /// <summary>
    /// Фоновый монитор доступности базы данных.
    /// Запускается один раз при старте приложения и непрерывно проверяет,
    /// доступен ли файл БД. При потере и восстановлении соединения
    /// поднимаются события ConnectionLost / ConnectionRestored.
    /// </summary>
    public sealed class NetworkDatabaseMonitor : IDisposable
    {
        private static readonly Lazy<NetworkDatabaseMonitor> _instance =
            new Lazy<NetworkDatabaseMonitor>(() => new NetworkDatabaseMonitor());

        public static NetworkDatabaseMonitor Instance => _instance.Value;

        /// <summary>Соединение с БД потеряно</summary>
        public event Action? ConnectionLost;

        /// <summary>Соединение с БД восстановлено</summary>
        public event Action? ConnectionRestored;

        /// <summary>Текущее состояние доступности БД</summary>
        public bool IsConnected { get; private set; } = true;

        private bool _wasConnected = true;
        private CancellationTokenSource? _cts;

        private const int StartupDelayMs  = 15000;
        private const int PollingIntervalMs = 5000;
        private const int CheckTimeoutMs   = 4000;

        private NetworkDatabaseMonitor() { }

        /// <summary>Запустить фоновый мониторинг</summary>
        public void Start()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
                return;

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => MonitorLoopAsync(_cts.Token));
        }

        /// <summary>Остановить фоновый мониторинг</summary>
        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task MonitorLoopAsync(CancellationToken ct)
        {
            try
            {
                await Task.Delay(StartupDelayMs, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var dbPath = DatabaseConfiguration.GetDatabasePath();
                    var accessible = await DatabaseErrorHandler.CheckDatabaseAccessAsync(
                        dbPath, CheckTimeoutMs, ct);

                    if (!accessible && _wasConnected)
                    {
                        _wasConnected = false;
                        IsConnected   = false;
                        ConnectionLost?.Invoke();
                    }
                    else if (accessible && !_wasConnected)
                    {
                        _wasConnected = true;
                        IsConnected   = true;
                        ConnectionRestored?.Invoke();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    // транзитные ошибки — игнорируем, продолжаем мониторинг
                }

                try
                {
                    await Task.Delay(PollingIntervalMs, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
