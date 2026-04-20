using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowSorter.Model.Grouping {
    /// <summary>
    /// 条件評価を行うルールの基底クラス
    /// </summary>
    public abstract class RuleBase {
        /// <summary>
        /// ルール名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 評価方法 (AND / OR)
        /// </summary>
        public EvalType EvalType { get; set; } = EvalType.AND;

        /// <summary>
        /// 条件式のリスト
        /// </summary>
        public List<Condition> Conditions { get; set; } = new List<Condition>();

        /// <summary>
        /// このルールを評価し、ウィンドウが条件に一致するかを評価
        /// </summary>
        /// <param name="window">評価対象のウィンドウ</param>
        /// <returns></returns>
        public bool Match(WindowInformation window) {
            if (Conditions == null || Conditions.Count == 0) {
                return false;
            }

            if (EvalType == EvalType.AND) {
                return Conditions.All(c => c.Match(window));
            } else {
                return Conditions.Any(c => c.Match(window));
            }
        }
    }
}
