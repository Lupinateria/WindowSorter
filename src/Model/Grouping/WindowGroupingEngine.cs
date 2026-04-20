using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowSorter.Model.Grouping {
    /// <summary>
    /// ウィンドウグルーピングの本体
    /// </summary>
    public static class WindowGroupingEngine {
        /// <summary>
        /// 表示中のウィンドウを分類して返す
        /// </summary>
        public static List<WindowGroup> GetWindowGroupList(IEnumerable<GroupingRule> rules, IEnumerable<WindowGroup> groups) {
            // ウィンドウを列挙
            List<WindowInformation> windows = WindowEnumerator.GetWindows();

            // 定義済みのグループからグループ生成
            Dictionary<string, WindowGroup> groupMap = groups.ToDictionary(
                g => g.ID,
                g => new WindowGroup { ID = g.ID, Name = g.Name }
            );

            WindowGroup otherGroup = new WindowGroup { ID = "OTHER", Name = "未分類" };

            // 各ウィンドウに対して、ルールを順番に適用する
            foreach (WindowInformation w in windows) {
                string matchedGroupId = string.Empty;
                string matchedRuleName = "なし";

                // ルールを順番に評価 (定義した順番)
                foreach (GroupingRule rule in rules) {
                    if (rule.Match(w)) {
                        // 最初にマッチしたルールで抜ける
                        matchedGroupId = rule.MoveTargetGroupID;
                        matchedRuleName = rule.Name;
                        break;
                    }
                }

                // 振り分け
                if (!string.IsNullOrEmpty(matchedGroupId) && groupMap.TryGetValue(matchedGroupId, out WindowGroup group)) {
                    group.Windows.Add(w);
                } else {
                    // ヒットしない場合は未分類へ
                    otherGroup.Windows.Add(w);
                }
            }

            // グループを返す
            // ウィンドウが空でも返す
            List<WindowGroup> result = new List<WindowGroup>();
            foreach (WindowGroup originalGroup in groups) {
                WindowGroup group = groupMap[originalGroup.ID];

                // ウィンドウの並び替え
                group.Windows = group.Windows
                    .OrderBy(w => {
                        // 最初にマッチする SortRule のインデックスを取得（先に定義したルールにヒットがより強い）
                        int ruleIndex = originalGroup.SortRules.FindIndex(r => r.Match(w));
                        // マッチしない場合は最大値（最後の方へ）
                        return ruleIndex == -1 ? int.MaxValue : ruleIndex;
                    })
                    .ThenBy(w => w.WindowTitle) // デフォルトの並び替え
                    .ToList();

                result.Add(group);
            }

            if (otherGroup.Windows.Any()) {
                // 未分類はタイトル昇順のみ（あとで変更できるようにするかも）
                otherGroup.Windows = otherGroup.Windows.OrderBy(w => w.WindowTitle).ToList();
                result.Add(otherGroup);
            }

            return result;
        }
    }
}
