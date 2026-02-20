using System.ComponentModel;
using System.Runtime.CompilerServices;
using Controls.Models;

namespace Controls.ViewModels
{
    /// <summary>
    /// Элемент для множественного выбора отделов
    /// </summary>
    public class DepartmentSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isCompleted;
        private int? _departmentTaskDepartmentId;

        public Department Department { get; set; } = null!;

        /// <summary>
        /// Выбран ли отдел для данного задания
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Отметка об исполнении данным отделом
        /// </summary>
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value)
                {
                    _isCompleted = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// ID связи DepartmentTaskDepartment (для редактирования существующих заданий)
        /// </summary>
        public int? DepartmentTaskDepartmentId
        {
            get => _departmentTaskDepartmentId;
            set
            {
                if (_departmentTaskDepartmentId != value)
                {
                    _departmentTaskDepartmentId = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
