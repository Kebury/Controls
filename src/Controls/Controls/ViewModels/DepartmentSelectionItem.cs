using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using Controls.Models;

namespace Controls.ViewModels
{
    /// <summary>
    /// Элемент для множественного выбора отделов с поддержкой
    /// привязки документов (исходящих и входящих) к каждому отделу.
    /// </summary>
    public class DepartmentSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        private bool _isCompleted;
        private int? _departmentTaskDepartmentId;
        private string _outgoingDocumentNumber = string.Empty;
        private bool _isDetailsExpanded;

        public DepartmentSelectionItem()
        {
            AddOutgoingFileCommand    = new RelayCommand(_ => AddFiles(OutgoingFiles));
            RemoveOutgoingFileCommand = new RelayCommand(f => RemoveFile(OutgoingFiles, f as string));
            AddIncomingFileCommand    = new RelayCommand(_ => AddFiles(IncomingFiles));
            RemoveIncomingFileCommand = new RelayCommand(f => RemoveFile(IncomingFiles, f as string));
            ToggleDetailsCommand      = new RelayCommand(_ => IsDetailsExpanded = !IsDetailsExpanded);
        }

        // ── Основные свойства ─────────────────────────────────────────────────

        public Department Department { get; set; } = null!;

        /// <summary>Выбран ли отдел для данного задания.</summary>
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                    // При снятии выбора сворачиваем детали
                    if (!value) IsDetailsExpanded = false;
                }
            }
        }

        /// <summary>Отметка об исполнении данным отделом.</summary>
        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                if (_isCompleted != value) { _isCompleted = value; OnPropertyChanged(); }
            }
        }

        /// <summary>ID связи DepartmentTaskDepartment (для редактирования существующих заданий).</summary>
        public int? DepartmentTaskDepartmentId
        {
            get => _departmentTaskDepartmentId;
            set
            {
                if (_departmentTaskDepartmentId != value) { _departmentTaskDepartmentId = value; OnPropertyChanged(); }
            }
        }

        // ── Поля для привязки документов ─────────────────────────────────────

        /// <summary>Номер направленного документа в данную организацию.</summary>
        public string OutgoingDocumentNumber
        {
            get => _outgoingDocumentNumber;
            set
            {
                if (_outgoingDocumentNumber != value) { _outgoingDocumentNumber = value; OnPropertyChanged(); }
            }
        }

        /// <summary>Направленные (исходящие) файлы для данной организации.</summary>
        public ObservableCollection<string> OutgoingFiles { get; } = new();

        /// <summary>Поступившие (входящие) файлы от данной организации.</summary>
        public ObservableCollection<string> IncomingFiles { get; } = new();

        /// <summary>Развёрнута ли секция с документами организации в UI.</summary>
        public bool IsDetailsExpanded
        {
            get => _isDetailsExpanded;
            set
            {
                if (_isDetailsExpanded != value) { _isDetailsExpanded = value; OnPropertyChanged(); }
            }
        }

        // ── Команды ──────────────────────────────────────────────────────────

        public ICommand AddOutgoingFileCommand    { get; }
        public ICommand RemoveOutgoingFileCommand { get; }
        public ICommand AddIncomingFileCommand    { get; }
        public ICommand RemoveIncomingFileCommand { get; }
        public ICommand ToggleDetailsCommand      { get; }

        // ── Внутренние helpers ────────────────────────────────────────────────

        private static void AddFiles(ObservableCollection<string> target)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл(ы)",
                Filter = "Все файлы (*.*)|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog() == true)
            {
                foreach (var path in dialog.FileNames)
                    if (!target.Contains(path))
                        target.Add(path);
            }
        }

        private static void RemoveFile(ObservableCollection<string> target, string? file)
        {
            if (file != null) target.Remove(file);
        }

        // ── INotifyPropertyChanged ────────────────────────────────────────────

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
