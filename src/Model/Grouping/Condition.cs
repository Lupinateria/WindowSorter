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
        Regex,
    }

    // 条件式
    public class Condition {
        // 比較対象
        public Target Target { get; set; }  = Target.WindowTitle;
        
        // 比較方法
        public Operator Operator { get; set; } = Operator.Contains;
        
        // 語句
        public string Value { get; set; } = "";

        // 否定フラグ
        public bool IsNegative { get; set; } = false;
        
        // この条件式を評価する
        public bool Match(WindowInformation window) {
            string targetValue = Target switch {
                Target.WindowTitle => window.WindowTitle ?? string.Empty,
                Target.ProcessName => window.ProcessName ?? string.Empty,
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(Value)) return true;

            bool result = Operator switch {
                Operator.Contains => targetValue.Contains(Value, StringComparison.OrdinalIgnoreCase),
                Operator.StartWith => targetValue.StartsWith(Value, StringComparison.OrdinalIgnoreCase),
                Operator.EndWith => targetValue.EndsWith(Value, StringComparison.OrdinalIgnoreCase),
                Operator.Regex => IsRegexMatch(targetValue, Value),
                _ => false
            };

            return IsNegative ? !result : result;
        }

        private bool IsRegexMatch(string input, string pattern) {
            try {
                return System.Text.RegularExpressions.Regex.IsMatch(input, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            } catch (ArgumentException) {
                return false;
            }
        }
    }
}
