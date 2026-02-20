namespace Controls.Models
{
    public class Executor
    {
        public int Id { get; set; }
        public string Position { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        
        /// <summary>
        /// Фамилия + инициалы для отображения в списках
        /// </summary>
        public string ShortName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(FullName))
                    return string.Empty;
                
                var parts = FullName.Trim().Split(' ');
                if (parts.Length < 2)
                    return FullName;
                
                var lastName = parts[0];
                var initials = string.Empty;
                
                for (int i = 1; i < parts.Length && i < 3; i++)
                {
                    if (!string.IsNullOrWhiteSpace(parts[i]))
                    {
                        initials += parts[i][0] + ".";
                    }
                }
                
                return $"{lastName} {initials}";
            }
        }
        
        /// <summary>
        /// Полная информация для tooltip (отображаются только заполненные поля)
        /// </summary>
        public string FullInfo
        {
            get
            {
                var parts = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(Position))
                    parts.Add(Position);
                    
                if (!string.IsNullOrWhiteSpace(FullName))
                    parts.Add(FullName);
                
                return string.Join("\n", parts);
            }
        }
    }
}
