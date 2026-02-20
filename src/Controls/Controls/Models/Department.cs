namespace Controls.Models
{
    public class Department
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        
        /// <summary>
        /// Отображаемое имя для списков
        /// </summary>
        public string DisplayName => ShortName;
        
        /// <summary>
        /// Подсказка с полным наименованием
        /// </summary>
        public string ToolTipText => FullName;
    }
}
