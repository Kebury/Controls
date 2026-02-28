using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using Controls.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Controls.Services
{
    /// <summary>
    /// Сервис для управления иконкой в системном трее
    /// </summary>
    public class TrayIconService : IDisposable
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private readonly TaskbarIcon _taskbarIcon;
        private readonly ControlsDbContext _context;
        private Icon? _iconWithBadge;
        private Icon? _defaultIcon;

        public TrayIconService(TaskbarIcon taskbarIcon, ControlsDbContext context)
        {
            _taskbarIcon = taskbarIcon;
            _context = context;
            
            _defaultIcon = LoadDefaultIcon();
        }

        /// <summary>
        /// Обновление badge на иконке с количеством неотработанных уведомлений
        /// </summary>
        public async System.Threading.Tasks.Task UpdateBadgeAsync()
        {
            try
            {
                using var freshContext = new ControlsDbContext();
                var allNotifications = await freshContext.Notifications.ToListAsync();
                var unprocessedCount = allNotifications.Count(n => !n.IsProcessed);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (unprocessedCount > 0)
                    {
                        if (_iconWithBadge != null)
                        {
                            var oldHandle = _iconWithBadge.Handle;
                            _iconWithBadge.Dispose();
                            DestroyIcon(oldHandle);
                            _iconWithBadge = null;
                        }
                        
                        _iconWithBadge = CreateIconWithBadge(unprocessedCount);
                        _taskbarIcon.Icon = _iconWithBadge;
                        _taskbarIcon.ToolTipText = $"Задачи - {unprocessedCount} уведомлений";
                    }
                    else
                    {
                        if (_iconWithBadge != null)
                        {
                            var oldHandle = _iconWithBadge.Handle;
                            _iconWithBadge.Dispose();
                            DestroyIcon(oldHandle);
                            _iconWithBadge = null;
                        }
                        
                        _taskbarIcon.Icon = _defaultIcon;
                        _taskbarIcon.ToolTipText = "Задачи";
                    }
                });
            }
            catch
            {
            }
        }

        /// <summary>
        /// Создание иконки с badge
        /// </summary>
        private Icon CreateIconWithBadge(int count)
        {
            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                if (_defaultIcon != null)
                {
                    graphics.DrawIcon(_defaultIcon, new Rectangle(0, 0, 32, 32));
                }

                var badgeText = count > 99 ? "99+" : count.ToString();
                var badgeSize = Math.Min(20, 32);
                var badgeRect = new Rectangle(32 - badgeSize, 32 - badgeSize, badgeSize, badgeSize);

                using (var brush = new SolidBrush(Color.FromArgb(220, 255, 0, 0)))
                {
                    graphics.FillEllipse(brush, badgeRect);
                }

                using (var font = new Font(new FontFamily("Segoe UI"), badgeSize / 3.5f, System.Drawing.FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.White))
                {
                    var format = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    graphics.DrawString(badgeText, font, textBrush, badgeRect, format);
                }

                IntPtr hIcon = bitmap.GetHicon();
                try
                {
                    Icon icon = Icon.FromHandle(hIcon);
                    Icon clonedIcon = (Icon)icon.Clone();
                    return clonedIcon;
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }

        /// <summary>
        /// Загрузка иконки по умолчанию
        /// </summary>
        private Icon LoadDefaultIcon()
        {
            try
            {
                var iconUri = new Uri("pack://application:,,,/Resources/app.ico", UriKind.Absolute);
                var streamInfo = Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    using (var tempIcon = new Icon(streamInfo.Stream))
                    {
                        return (Icon)tempIcon.Clone();
                    }
                }
            }
            catch
            {
            }

            using (var bitmap = new Bitmap(32, 32))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                using (var brush = new SolidBrush(Color.FromArgb(0, 120, 215)))
                {
                    graphics.FillEllipse(brush, 4, 4, 24, 24);
                }
                
                IntPtr hIcon = bitmap.GetHicon();
                try
                {
                    Icon icon = Icon.FromHandle(hIcon);
                    Icon clonedIcon = (Icon)icon.Clone();
                    return clonedIcon;
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }

        /// <summary>
        /// Показать balloon-уведомление в системном трее
        /// </summary>
        public void ShowBalloonTip(string title, string message)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _taskbarIcon.ShowBalloonTip(title, message,
                        Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
                });
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (_iconWithBadge != null)
            {
                var handle = _iconWithBadge.Handle;
                _iconWithBadge.Dispose();
                DestroyIcon(handle);
                _iconWithBadge = null;
            }
            
            if (_defaultIcon != null)
            {
                var handle = _defaultIcon.Handle;
                _defaultIcon.Dispose();
                DestroyIcon(handle);
                _defaultIcon = null;
            }
        }
    }
}
