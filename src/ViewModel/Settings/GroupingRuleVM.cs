using System.Collections.ObjectModel;
using System.Linq;
using WindowSorter.Core;
using WindowSorter.Model.Grouping;

namespace WindowSorter.ViewModel.Settings {
    /// <summary>
    /// ウィンドウ振り分けルールの設定用ViewModel
    /// </summary>
    public class GroupingRuleVM : NotificationObject {
        private readonly GroupingRule _model;

        public string Name {
            get => _model.Name;
            set { _model.Name = value; RaisePropertyChanged(); }
        }

        public string MoveTargetGroupID {
            get => _model.MoveTargetGroupID;
            set { _model.MoveTargetGroupID = value; RaisePropertyChanged(); }
        }

        public EvalType EvalType {
            get => _model.EvalType;
            set { _model.EvalType = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 振り分け条件の一覧（画面バインド用）
        /// </summary>
        public ObservableCollection<Condition> Conditions { get; }

        public GroupingRuleVM(GroupingRule model) {
            _model = model;
            Conditions = new ObservableCollection<Condition>(model.Conditions);
        }

        /// <summary>
        /// 編集内容をModelに書き戻す
        /// </summary>
        public GroupingRule Pack() {
            _model.Conditions = Conditions.ToList();
            return _model;
        }
    }
}
