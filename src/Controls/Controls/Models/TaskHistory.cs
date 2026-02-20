using System;

namespace Controls.Models
{
    /// <summary>
    /// Класс для представления истории задания
    /// </summary>
    public class TaskHistoryEntry
    {
        public DateTime Date { get; set; }
        public string Event { get; set; } = string.Empty;
        public string? AttachedDocument { get; set; }
        public bool IsFinalCompletion { get; set; }
    }
}
