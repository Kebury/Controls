using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Controls.ViewModels
{
    /// <summary>
    /// Базовый класс для всех ViewModels с поддержкой освобождения ресурсов
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        private bool _disposed = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Освобождение управляемых ресурсов (переопределяется в дочерних классах)
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
