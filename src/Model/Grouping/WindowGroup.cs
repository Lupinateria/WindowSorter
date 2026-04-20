using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WindowSorter.Model.Grouping {
    public class WindowGroup {
        /// <summary>
        /// グループを一意に識別するID (UUID)
        /// </summary>
        public string ID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// グループ 名称 
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// このグループ内でのウィンドウ並び替えルール
        /// </summary>
        public List<SortRule> SortRules { get; set; } = new List<SortRule>();
        
        /// <summary>
        /// このグループに分けられるウィンドウのリスト (JSONには含めない)
        /// </summary>
        [JsonIgnore]
        public List<WindowInformation> Windows { get; set; } = new List<WindowInformation>();
    }
}
