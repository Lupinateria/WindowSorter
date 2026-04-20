using System.Collections.ObjectModel;
using System.Linq;
using WindowSorter.Core;
using WindowSorter.Model.Grouping;

namespace WindowSorter.ViewModel.Settings {
    /// <summary>
    /// 並び替えルールの設定用ViewModel
    /// </summary>
    public class SortRuleVM : NotificationObject {
        private readonly SortRule _model;

        public string Name {
            get => _model.Name;
            set { _model.Name = value; RaisePropertyChanged(); }
        }

        public EvalType EvalType {
            get => _model.EvalType;
            set { _model.EvalType = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 条件一覧（画面バインド用）
        /// </summary>
        public ObservableCollection<Condition> Conditions { get; }

        public SortRuleVM(SortRule model) {
            _model = model;
            Conditions = new ObservableCollection<Condition>(model.Conditions);
        }

        /// <summary>
        /// 編集内容をModelに書き戻す
        /// </summary>
        public SortRule Pack() {
            _model.Conditions = Conditions.ToList();
            return _model;
        }
    }
}
