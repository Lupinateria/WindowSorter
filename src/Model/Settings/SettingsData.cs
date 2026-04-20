using System.Collections.Generic;
using System.Windows.Input;
using WindowSorter.Model.Grouping;

namespace WindowSorter.Model {
    /// <summary>
    /// 検索方法
    /// </summary>
    public enum SearchMethod {
        /// <summary>部分一致</summary>
        PartialMatch,
        /// <summary>曖昧検索</summary>
        FuzzySearch
    }

    /// <summary>
    /// 設定
    /// </summary>
    public class SettingsData {
        public List<WindowGroup> GroupList { get; set; } = new List<WindowGroup>();
        public List<GroupingRule> GroupingRuleList { get; set; } = new List<GroupingRule>();
        public ModifierKeys HotKeyModifier { get; set; } = ModifierKeys.Alt;
        public Key HotKey { get; set; } = Key.Q;
        public uint ShowWindowDelay { get; set; } = 10;
        public bool ClearSearchOnWindowSelect { get; set; } = true;
        public double ThumbnailWidth { get; set; } = 284;
        public double ThumbnailHeight { get; set; } = 160;
        public SearchMethod SearchMethod { get; set; } = SearchMethod.PartialMatch;
        /// <summary>
        /// 現在の設定のクローンインスタンス取得
        /// </summary>
        public SettingsData GetClone() {
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            return System.Text.Json.JsonSerializer.Deserialize<SettingsData>(json)!;
        }
    }
}
