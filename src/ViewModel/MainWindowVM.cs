using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using WindowSorter.Core;
using WindowSorter.Model;
using WindowSorter.Model.Grouping;

namespace WindowSorter.ViewModel {
    public class MainWindowVM : NotificationObject {

        // グルーピングのリスト
        private ObservableCollection<WindowGroupVM> _windowGroupList;
        public ObservableCollection<WindowGroupVM> WindowGroupList {
            get {
                return _windowGroupList;
            }
            set {
                if (_windowGroupList != value) {
                    SetProperty(ref this._windowGroupList, value);
                    
                    _windowGroupListView = CollectionViewSource.GetDefaultView(_windowGroupList);
                    _windowGroupListView.Filter = FilterGroup;
                    RaisePropertyChanged(nameof(WindowGroupListView));
                }
            }
        }

        // グルーピングのリスト CollectionView
        private ICollectionView _windowGroupListView;
        public ICollectionView WindowGroupListView {
            get {
                if (_windowGroupListView == null && _windowGroupList != null) {
                    _windowGroupListView = CollectionViewSource.GetDefaultView(_windowGroupList);
                    _windowGroupListView.Filter = FilterGroup;
                }
                return _windowGroupListView;
            }
        }

        // 検索文字列
        private string _searchText = string.Empty;
        public string SearchText {
            get => _searchText;
            set {
                if (SetProperty(ref _searchText, value)) {
                    // 各グループの検索文字を更新
                    if (_windowGroupList != null) {
                        foreach (var group in _windowGroupList) {
                            group.SearchText = value;
                        }
                    }
                    // グループ自体の表示・非表示を再評価
                    WindowGroupListView?.Refresh();

                    // 検索結果の1番目を選択
                    SelectFirstVisible();
                }
            }
        }

        // 選択状態のウィンドウ
        private WindowInformationVM _selectedWindow;
        public WindowInformationVM SelectedWindow {
            get => _selectedWindow;
            set {
                if (_selectedWindow != value) {
                    if (_selectedWindow != null) {
                        _selectedWindow.IsSelected = false;
                    }

                    _selectedWindow = value;

                    if (_selectedWindow != null) {
                        _selectedWindow.IsSelected = true;
                    }
                    RaisePropertyChanged();
                }
            }
        }

        // 表示されているウィンドウの中から最初を選択
        private void SelectFirstVisible() {
            var first = GetVisibleWindows().FirstOrDefault();
            SelectedWindow = first;
        }

        // 表示されているウィンドウを列挙
        private IEnumerable<WindowInformationVM> GetVisibleWindows() {
            if (_windowGroupList == null) 
                return Enumerable.Empty<WindowInformationVM>();

            return _windowGroupList
                .Where(x => x.HasVisibleWindows)
                .SelectMany(x => x.WindowListView.Cast<WindowInformationVM>());
        }

        // ウィンドウ 選択位置を移動
        public void MoveSelection(int deltaX, int deltaY, int columns) {
            var visibleWindows = GetVisibleWindows().ToList();
            if (!visibleWindows.Any()) return;

            int currentIndex = visibleWindows.IndexOf(SelectedWindow);
            int nextIndex;

            if (currentIndex == -1) {
                nextIndex = 0;
            } else {
                // deltaYがある場合はcolumns分移動
                int delta = deltaX + (deltaY * columns);
                nextIndex = currentIndex + delta;
            }

            if (nextIndex < 0) nextIndex = 0;
            if (nextIndex >= visibleWindows.Count) nextIndex = visibleWindows.Count - 1;

            SelectedWindow = visibleWindows[nextIndex];
        }

        private bool FilterGroup(object item) {
            if (item is WindowGroupVM group) {
                // グループ内に表示対象のウィンドウが1つでもあればグループを表示
                return group.HasVisibleWindows;
            }
            return false;
        }

        // ウィンドウ一覧の更新
        public void Refresh() {
            // 現在の開閉状態をバックアップ
            var currentStates = _windowGroupList?.ToDictionary(g => g.Name, g => g.IsExpanded);

            List<WindowGroup> groups = WindowGroupingEngine.GetWindowGroupList(
                SettingsService.Current.GroupingRuleList, 
                SettingsService.Current.GroupList,
                SettingsService.Current.IgnoreList
            );
            var newGroups = groups.Select(x => {
                var vm = new WindowGroupVM(x, () => {
                    if (SettingsService.Current.ClearSearchOnWindowSelect) {
                        this.SearchText = string.Empty;
                    }
                }) { SearchText = this.SearchText };
                // 以前の状態があれば引き継ぐ
                if (currentStates != null && currentStates.TryGetValue(vm.Name, out bool isExpanded)) {
                    vm.IsExpanded = isExpanded;
                }
                return vm;
            });

            this.WindowGroupList = new ObservableCollection<WindowGroupVM>(newGroups);

            // 最初の1つを選択
            SelectFirstVisible();
        }
    }
}
