using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowSorter.Model.Grouping {

    /// <summary>
    /// すべてに一致 or いずれかに一致
    /// </summary>
    public enum EvalType {
        AND,
        OR,
    }

    /// <summary>
    /// 分類ルール
    /// </summary>
    public class GroupingRule : RuleBase {
        /// <summary>
        /// 分類先となるグループのID
        /// </summary>
        public string MoveTargetGroupID { get; set; } = string.Empty;
    }
}
