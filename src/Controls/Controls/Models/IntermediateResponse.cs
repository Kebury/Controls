using System;

namespace Controls.Models
{
    /// <summary>
    /// Класс для представления промежуточного ответа по заданию
    /// </summary>
    public class IntermediateResponse
    {
        /// <summary>
        /// Дата действия (когда было направлено донесение/отмечено исполнение)
        /// </summary>
        public DateTime Date { get; set; }
        
        public string? DocumentPath { get; set; }
        
        /// <summary>
        /// Тип промежуточного действия
        /// </summary>
        public string ActionType { get; set; } = string.Empty;
        
        /// <summary>
        /// Номер исходящего документа (если применимо)
        /// </summary>
        public string? OutgoingNumber { get; set; }
        
        /// <summary>
        /// Срок исполнения на момент создания записи
        /// </summary>
        public DateTime? OriginalDueDate { get; set; }
    }
}
