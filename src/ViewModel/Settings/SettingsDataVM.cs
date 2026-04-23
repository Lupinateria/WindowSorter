using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WindowSorter.Core;
using WindowSorter.Model;
using WindowSorter.Model.Settings;
using WindowSorter.Model.Grouping;

namespace WindowSorter.ViewModel.Settings {
    /// <summary>
    /// 設定データ全体を管理するルートViewModel
    /// </summary>
    public class SettingsDataVM : NotificationObject {
        private readonly SettingsData _model;

        // -----------------------------------------------------------------
        // データリスト
        // -----------------------------------------------------------------
        /// <summary>グループ</summary>
        public ObservableCollection<WindowGroupVM> GroupList { get; }

        /// <summary>振り分けルール</summary>
        public ObservableCollection<GroupingRuleVM> GroupingRuleList { get; }

        /// <summary>除外リスト</summary>
        public ObservableCollection<Condition> IgnoreList { get; }

        // -----------------------------------------------------------------
        // ホットキー設定
        // -----------------------------------------------------------------
        private ModifierKeys _selectedModifier;
        public ModifierKeys SelectedModifier {
            get => _selectedModifier;
            set {
                if (SetProperty(ref _selectedModifier, value)) {
                    RaisePropertyChanged(nameof(HotKeyText));
                }
            }
        }
        
        private Key _selectedKey;
        public Key SelectedKey {
            get => _selectedKey;
            set {
                if (SetProperty(ref _selectedKey, value)) {
                    RaisePropertyChanged(nameof(HotKeyText));
                }
            }
        }

        public string HotKeyText {
            get {
                if (SelectedKey == Key.None) return "未登録";

                var sb = new System.Text.StringBuilder();
                if (SelectedModifier.HasFlag(ModifierKeys.Control)) sb.Append("Ctrl + ");
                if (SelectedModifier.HasFlag(ModifierKeys.Alt)) sb.Append("Alt + ");
                if (SelectedModifier.HasFlag(ModifierKeys.Shift)) sb.Append("Shift + ");
                if (SelectedModifier.HasFlag(ModifierKeys.Windows)) sb.Append("Win + ");

                sb.Append(SelectedKey.ToString());
                return sb.ToString();
            }
        }

        // -----------------------------------------------------------------
        // サムネイルサイズ
        // -----------------------------------------------------------------
        private double _thumbnailWidth;
        public double ThumbnailWidth {
            get => _thumbnailWidth;
            set => SetProperty(ref _thumbnailWidth, value);
        }

        private double _thumbnailHeight;
        public double ThumbnailHeight {
            get => _thumbnailHeight;
            set => SetProperty(ref _thumbnailHeight, value);
        }

        // -----------------------------------------------------------------
        // ウィンドウ表示までのディレイ
        // -----------------------------------------------------------------
        private uint _showWindowDelay;
        public uint ShowWindowDelay {
            get => _showWindowDelay;
            set {
                if (SetProperty(ref _showWindowDelay, value)) {
                    RaisePropertyChanged(nameof(ShowWindowDelay));
                }
            }
        }

        // -----------------------------------------------------------------
        // 検索ボックスをクリアするか
        // -----------------------------------------------------------------
        private bool _clearSearchOnWindowSelect;
        public bool ClearSearchOnWindowSelect {
            get => _clearSearchOnWindowSelect;
            set {
                if (SetProperty(ref _clearSearchOnWindowSelect, value)) {
                    RaisePropertyChanged(nameof(ClearSearchOnWindowSelect));
                }
            }
        }

        // -----------------------------------------------------------------
        // 検索設定
        // -----------------------------------------------------------------
        public bool IsSearchPartialMatch {
            get => SearchMethod == SearchMethod.PartialMatch;
            set {
                if (value) SearchMethod = SearchMethod.PartialMatch;
            }
        }

        public bool IsSearchFuzzy {
            get => SearchMethod == SearchMethod.FuzzySearch;
            set {
                if (value) SearchMethod = SearchMethod.FuzzySearch;
            }
        }

        private SearchMethod _searchMethod;
        public SearchMethod SearchMethod {
            get => _searchMethod;
            set {
                if (SetProperty(ref _searchMethod, value)) {
                    RaisePropertyChanged(nameof(IsSearchPartialMatch));
                    RaisePropertyChanged(nameof(IsSearchFuzzy));
                }
            }
        }

        // -----------------------------------------------------------------
        // 色設定
        // -----------------------------------------------------------------
        private string _selectedColor;
        public string SelectedColor {
            get => _selectedColor;
            set => SetProperty(ref _selectedColor, value);
        }

        private string _backgroundColor;
        public string BackgroundColor {
            get => _backgroundColor;
            set => SetProperty(ref _backgroundColor, value);
        }

        // -----------------------------------------------------------------
        // 選択状態の管理
        // -----------------------------------------------------------------
        private WindowGroupVM _selectedGroup;
        public WindowGroupVM SelectedGroup {
            get => _selectedGroup;
            set {
                if (SetProperty(ref _selectedGroup, value)) {
                    // グループが変わったら、その中のソートルール選択もリセット
                    SelectedSortRule = value?.SortRules.FirstOrDefault();
                }
            }
        }

        private SortRuleVM _selectedSortRule;
        public SortRuleVM SelectedSortRule {
            get => _selectedSortRule;
            set => SetProperty(ref _selectedSortRule, value);
        }

        private GroupingRuleVM _selectedGroupingRule;
        public GroupingRuleVM SelectedGroupingRule {
            get => _selectedGroupingRule;
            set => SetProperty(ref _selectedGroupingRule, value);
        }

        /// <summary>
        /// 移動先グループの選択肢（ComboBox用。VMのリストをそのまま提供）
        /// </summary>
        public IEnumerable<WindowGroupVM> GroupOptions => GroupList;

        // -----------------------------------------------------------------
        // コマンド
        // -----------------------------------------------------------------
        // グループ設定 - グループ一覧
        public DelegateCommand AddGroupCommand { get; }
        public DelegateCommand RemoveGroupCommand { get; }
        public DelegateCommand MoveUpGroupCommand { get; }
        public DelegateCommand MoveDownGroupCommand { get; }

        // グループ設定 - 並び替えルール
        public DelegateCommand AddSortRuleCommand { get; }
        public DelegateCommand RemoveSortRuleCommand { get; }
        public DelegateCommand MoveUpSortRuleCommand { get; }
        public DelegateCommand MoveDownSortRuleCommand { get; }
        public DelegateCommand AddSortRuleConditionCommand { get; }
        public DelegateCommand RemoveSortRuleConditionCommand { get; }

        public DelegateCommand AddGroupingRuleCommand { get; }
        public DelegateCommand RemoveGroupingRuleCommand { get; }
        public DelegateCommand MoveUpGroupingRuleCommand { get; }
        public DelegateCommand MoveDownGroupingRuleCommand { get; }

        public DelegateCommand AddGroupingRuleConditionCommand { get; }
        public DelegateCommand RemoveGroupingRuleConditionCommand { get; }

        // 除外設定
        public DelegateCommand AddIgnoreConditionCommand { get; }
        public DelegateCommand RemoveIgnoreConditionCommand { get; }

        public DelegateCommand ClearHotKeyCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        // -----------------------------------------------------------------
        // コンストラクタ
        // -----------------------------------------------------------------
        public SettingsDataVM() {
            // Modelを取得（クローンを編集）
            _model = SettingsService.Current.GetClone();

            #region 設定を追加したら、ここでModel -> VMに移し替える
            // 全般の設定
            SelectedModifier = _model.HotKeyModifier;
            SelectedKey = _model.HotKey;
            ShowWindowDelay = _model.ShowWindowDelay;
            ClearSearchOnWindowSelect = _model.ClearSearchOnWindowSelect;
            ThumbnailWidth = _model.ThumbnailWidth;
            ThumbnailHeight = _model.ThumbnailHeight;
            SearchMethod = _model.SearchMethod;
            SelectedColor = _model.SelectedColor;
            BackgroundColor = _model.BackgroundColor;

            // グループ一覧とルールのViewModel
            GroupList = new ObservableCollection<WindowGroupVM>(
                _model.GroupList.Select(x => new WindowGroupVM(x)));

            GroupingRuleList = new ObservableCollection<GroupingRuleVM>(
                _model.GroupingRuleList.Select(x => new GroupingRuleVM(x)));

            // 除外設定のViewModel
            IgnoreList = new ObservableCollection<Condition>(_model.IgnoreList);

            #endregion

            // デフォルト選択
            SelectedGroup = GroupList.FirstOrDefault();
            SelectedGroupingRule = GroupingRuleList.FirstOrDefault();

            #region Command
            // -----------------------------------------------------------------
            // ホットキー設定
            // -----------------------------------------------------------------
            ClearHotKeyCommand = new DelegateCommand(_ => {
                SelectedKey = Key.None;
                SelectedModifier = ModifierKeys.None;
            });

            // -----------------------------------------------------------------
            // グループ設定 - グループ一覧
            // -----------------------------------------------------------------
            // 追加
            AddGroupCommand = new DelegateCommand(_ => {
                var newGroup = new WindowGroup { Name = "新規グループ", ID = Guid.NewGuid().ToString() };
                var vm = new WindowGroupVM(newGroup);
                GroupList.Add(vm);
                SelectedGroup = vm;
            });

            // 削除
            RemoveGroupCommand = new DelegateCommand(obj => {
                if (obj is WindowGroupVM vm) GroupList.Remove(vm);
            });

            // 上
            MoveUpGroupCommand = new DelegateCommand(obj => {
                if (obj is WindowGroupVM vm) {
                    var index = GroupList.IndexOf(vm);
                    if (index > 0) GroupList.Move(index, index - 1);
                }
            });

            // 下
            MoveDownGroupCommand = new DelegateCommand(obj => {
                if (obj is WindowGroupVM vm) {
                    var index = GroupList.IndexOf(vm);
                    if (index < GroupList.Count - 1) GroupList.Move(index, index + 1);
                }
            });

            // -----------------------------------------------------------------
            // グループ設定 - ソートルール一覧
            // -----------------------------------------------------------------
            // 新規
            AddSortRuleCommand = new DelegateCommand(_ => {
                if (SelectedGroup != null) {
                    var newSortRule = new SortRule { Name = "新規並び替えルール" };
                    var vm = new SortRuleVM(newSortRule);
                    SelectedGroup.SortRules.Add(vm);
                    SelectedSortRule = vm;
                }
            });

            // 削除
            RemoveSortRuleCommand = new DelegateCommand(obj => {
                if (SelectedGroup != null && obj is SortRuleVM vm) {
                    SelectedGroup.SortRules.Remove(vm);
                }
            });

            // 上
            MoveUpSortRuleCommand = new DelegateCommand(obj => {
                if (SelectedGroup != null && obj is SortRuleVM vm) {
                    var index = SelectedGroup.SortRules.IndexOf(vm);
                    if (index > 0) SelectedGroup.SortRules.Move(index, index - 1);
                }
            });

            // 下
            MoveDownSortRuleCommand = new DelegateCommand(obj => {
                if (SelectedGroup != null && obj is SortRuleVM vm) {
                    var index = SelectedGroup.SortRules.IndexOf(vm);
                    if (index < SelectedGroup.SortRules.Count - 1) SelectedGroup.SortRules.Move(index, index + 1);
                }
            });

            // -----------------------------------------------------------------
            // グループ設定 - 並び替えルールの条件
            // -----------------------------------------------------------------
            // 追加
            AddSortRuleConditionCommand = new DelegateCommand(_ => {
                SelectedSortRule.Conditions.Add(new Condition());
            });

            // 削除
            RemoveSortRuleConditionCommand = new DelegateCommand(obj => {
                if (obj is Condition cond) SelectedSortRule?.Conditions.Remove(cond);
            });

            // -----------------------------------------------------------------
            // ルール設定 - ルール一覧
            // -----------------------------------------------------------------
            // 新規
            AddGroupingRuleCommand = new DelegateCommand(_ => {
                var newRule = new GroupingRule { Name = "新規ルール" };
                var vm = new GroupingRuleVM(newRule);
                GroupingRuleList.Add(vm);
                SelectedGroupingRule = vm;
            });

            // 削除
            RemoveGroupingRuleCommand = new DelegateCommand(obj => {
                if (obj is GroupingRuleVM vm) GroupingRuleList.Remove(vm);
            });

            // 上
            MoveUpGroupingRuleCommand = new DelegateCommand(obj => {
                if (obj is GroupingRuleVM vm) {
                    var index = GroupingRuleList.IndexOf(vm);
                    if (index > 0) GroupingRuleList.Move(index, index - 1);
                }
            });

            // 下
            MoveDownGroupingRuleCommand = new DelegateCommand(obj => {
                if (obj is GroupingRuleVM vm) {
                    var index = GroupingRuleList.IndexOf(vm);
                    if (index < GroupingRuleList.Count - 1) GroupingRuleList.Move(index, index + 1);
                }
            });

            // -----------------------------------------------------------------
            // ルール設定 - 判定条件
            // -----------------------------------------------------------------
            // 条件追加
            AddGroupingRuleConditionCommand = new DelegateCommand(_ => {
                SelectedGroupingRule?.Conditions.Add(new Condition());
            });

            // 条件削除
            RemoveGroupingRuleConditionCommand = new DelegateCommand(obj => {
                if (obj is Condition cond) {
                    SelectedGroupingRule?.Conditions.Remove(cond);
                }
            });

            // -----------------------------------------------------------------
            // 除外設定
            // -----------------------------------------------------------------
            AddIgnoreConditionCommand = new DelegateCommand(_ => {
                IgnoreList.Add(new Condition());
            });

            RemoveIgnoreConditionCommand = new DelegateCommand(obj => {
                if (obj is Condition cond) IgnoreList.Remove(cond);
            });

            #endregion

            // -----------------------------------------------------------------
            // 保存・キャンセル
            // -----------------------------------------------------------------
            // 保存
            SaveCommand = new DelegateCommand(_ => {
                #region 設定を追加したら、ここでVM -> Modelに移し替える

                // 全般
                _model.HotKeyModifier = SelectedModifier;
                _model.HotKey = SelectedKey;
                _model.ShowWindowDelay = ShowWindowDelay;
                _model.ClearSearchOnWindowSelect = ClearSearchOnWindowSelect;
                _model.ThumbnailWidth = ThumbnailWidth;
                _model.ThumbnailHeight = ThumbnailHeight;
                _model.SearchMethod = SearchMethod;
                _model.SelectedColor = SelectedColor;
                _model.BackgroundColor = BackgroundColor;

                // グループとルールと除外リスト
                _model.GroupList = GroupList.Select(x => x.Pack()).ToList();
                _model.GroupingRuleList = GroupingRuleList.Select(x => x.Pack()).ToList();
                _model.IgnoreList = IgnoreList.ToList();
                
                #endregion

                // 設定を適用する
                SettingsService.Save(_model);
                ColorManager.Apply(SettingsService.Current.SelectedColor, SettingsService.Current.BackgroundColor);

                // 設定画面を閉じる
                WindowService.Instance.Close(this, true);

            });

            // キャンセル
            CancelCommand = new DelegateCommand(_ => {
                WindowService.Instance.Close(this, false);
            });
        }

        /// <summary>
        /// ホットキーをセットする（Viewから呼ばれる）
        /// </summary>
        public void SetHotKey(ModifierKeys modifier, Key key) {
            _selectedModifier = modifier;
            _selectedKey = key;
            RaisePropertyChanged(nameof(HotKeyText));
        }
    }
}
