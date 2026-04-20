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
    public class WindowGroupVM : NotificationObject {
        public string Name { get; set; }

        // 開閉状態
        private bool _isExpanded = true;
        public bool IsExpanded {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
        }

        // ウィンドウの一覧
        private ObservableCollection<WindowInformationVM> _windowList;
        public ObservableCollection<WindowInformationVM> WindowList {
            get {
                return _windowList;
            }
            set {
                if (_windowList != value) {
                    SetProperty(ref this._windowList, value);

                    _windowListView = CollectionViewSource.GetDefaultView(_windowList);
                    _windowListView.Filter = FilterWindow;
                    RaisePropertyChanged(nameof(WindowListView));
                }
            }
        }

        // ウィンドウの一覧 CollectionView
        private ICollectionView _windowListView;
        public ICollectionView WindowListView {
            get {
                if (_windowListView == null && _windowList != null) {
                    _windowListView = CollectionViewSource.GetDefaultView(_windowList);
                    _windowListView.Filter = FilterWindow;
                }
                return _windowListView;
            }
        }

        // 検索文字列
        private string _searchText = string.Empty;
        public string SearchText {
            get => _searchText;
            set {
                if (SetProperty(ref _searchText, value)) {
                    WindowListView?.Refresh();

                    RaisePropertyChanged(nameof(HasVisibleWindows));
                }
            }
        }

        // 表示対象のウィンドウが1つでもあるか
        public bool HasVisibleWindows {
            get {
                // if (string.IsNullOrWhiteSpace(SearchText)) return true;
                return WindowListView?.Cast<object>().Any() ?? false;
            }
        }

        // ウィンドウ検索
        private bool FilterWindow(object item) {
            if (item is WindowInformationVM window) {
                if (string.IsNullOrWhiteSpace(SearchText)) return true;

                return IsMatch(window.WindowTitle, SearchText) ||
                       IsMatch(window.ProcessName, SearchText);
            }
            return false;
        }

        // 検索本体
        private static bool IsMatch(string source, string query) {
            if (string.IsNullOrEmpty(query)) {
                return true;
            }

            if (string.IsNullOrEmpty(source)) {
                return false;
            }

            switch (SettingsService.Current.SearchMethod) {
                case SearchMethod.PartialMatch:
                    return source.Contains(query, StringComparison.OrdinalIgnoreCase);

                case SearchMethod.FuzzySearch:
                    int sourceIdx = 0;
                    int queryIdx = 0;

                    while (sourceIdx < source.Length && queryIdx < query.Length) {
                        if (char.ToLowerInvariant(source[sourceIdx]) == char.ToLowerInvariant(query[queryIdx])) {
                            queryIdx++;
                        }
                        sourceIdx++;
                    }

                    return queryIdx == query.Length;

                default:
                    return source.Contains(query, StringComparison.OrdinalIgnoreCase);
            }
        }

        public WindowGroupVM(WindowGroup windowGroup, Action onWindowExecuted = null) {
            this.Name = windowGroup.Name;

            this.WindowList = new ObservableCollection<WindowInformationVM>(windowGroup.Windows.Select(x => new WindowInformationVM(x, onWindowExecuted)));
        }
    }
}
