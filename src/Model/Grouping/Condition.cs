namespace WindowSorter.Model.Grouping {
    // 比較対象
    public enum Target {
        WindowTitle,
        ProcessName,
    }

    // 比較方法
    public enum Operator {
        Contains,
        StartWith,
        EndWith,
    }

    /// <summary>
    /// 条件式
    /// </summary>
    public class Condition {
        /// <summary>
        /// 比較対象
        /// </summary>
        public Target Target { get; set; }  = Target.WindowTitle;
        
        /// <summary>
        /// 比較方法
        /// </summary>
        public Operator Operator { get; set; } = Operator.Contains;
        
        /// <summary>
        /// 語句
        /// </summary>
        public string Value { get; set; } = "";
        
        /// <summary>
        /// この条件式を評価する
        /// </summary>
        /// <returns></returns>
        public bool Match(WindowInformation window) {
            string targetValue = Target switch {
                Target.WindowTitle => window.WindowTitle ?? string.Empty,
                Target.ProcessName => window.ProcessName ?? string.Empty,
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(Value)) return true;

            return Operator switch {
                Operator.Contains => targetValue.Contains(Value, StringComparison.OrdinalIgnoreCase),
                Operator.StartWith => targetValue.StartsWith(Value, StringComparison.OrdinalIgnoreCase),
                Operator.EndWith => targetValue.EndsWith(Value, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
