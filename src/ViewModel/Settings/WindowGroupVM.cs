using System;
using System.Collections.ObjectModel;
using System.Linq;
using WindowSorter.Core;
using WindowSorter.Model.Grouping;

namespace WindowSorter.ViewModel.Settings {
    /// <summary>
    /// ウィンドウグループの設定用ViewModel
    /// </summary>
    public class WindowGroupVM : NotificationObject {
        private readonly WindowGroup _model;

        public string Name {
            get => _model.Name;
            set { _model.Name = value; RaisePropertyChanged(); }
        }

        public string ID => _model.ID;

        /// <summary>
        /// ソートルール一覧（VMでラップして保持）
        /// </summary>
        public ObservableCollection<SortRuleVM> SortRules { get; }

        public WindowGroupVM(WindowGroup model) {
            _model = model;
            SortRules = new ObservableCollection<SortRuleVM>(
                model.SortRules.Select(x => new SortRuleVM(x)));
        }

        /// <summary>
        /// 編集内容をModelに書き戻す
        /// </summary>
        public WindowGroup Pack() {
            _model.SortRules = SortRules.Select(x => x.Pack()).ToList();
            return _model;
        }
    }
}
