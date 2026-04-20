using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WindowSorter.Core {
    /// <summary>
    /// Windowのオープン・クローズをまとめたサービスクラス
    /// </summary>
    public class WindowService : IWindowService {
        enum OpenMode {
            Normal,
            Dialog,
        };

        public static WindowService Instance { get; private set; } = new WindowService();

        private readonly Dictionary<Type, Tuple<Window, OpenMode>> _openWindowDict = new Dictionary<Type, Tuple<Window, OpenMode>>();

        private WindowService() { }

        /// <summary>
        /// Tで渡したWindowを開く。重複して開かない。
        /// </summary>
        /// <typeparam name="T">開きたいWindowの型</typeparam>
        /// <param name="vm">Windowに紐づけるViewModel(DataContext)<br>外部からViewModelを注入する必要がないなら、null。つまり、View自身がコードビハインドでViewModelを生成する場合。</param>
        public void ShowWindow<T>(object? vm = null) where T : Window, new() {
            Type windowType;

            // すでに開いているウィンドウがあれば表示
            if (ActivateExistingWindow<T>(out windowType)) {
                return;
            }

            // ウィンドウを新しく表示
            T newWindow = CreateNewWindow<T>(vm, windowType, OpenMode.Normal);

            newWindow.Show();
        }

        /// <summary>
        /// Windowをダイアログで開く
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="owner"></param>
        /// <param name="vm"></param>
        /// <returns></returns>
        public bool? ShowDialog<T>(Window owner, object? vm = null) where T : Window, new() {
            Type windowType;

            // すでに開いているウィンドウがあれば表示
            if (ActivateExistingWindow<T>(out windowType)) {
                return true;
            }

            // ウィンドウを新しく表示
            T newWindow = CreateNewWindow<T>(vm, windowType, OpenMode.Dialog);

            // オーナー設定
            newWindow.Owner = owner;

            return newWindow.ShowDialog();
        }

        /// <summary>
        /// 指定されたウィンドウを閉じる
        /// </summary>
        /// <param name="window"></param>
        public void Close(Window window, bool? dialogResult) {

            var tuple = _openWindowDict.Values.FirstOrDefault(x => x.Item1 == window);
            
            if (tuple == null) {
                return;
            }

            CloseWindow(tuple.Item1, tuple.Item2, dialogResult);
        }

        /// <summary>
        /// ViewModelを渡して紐づくウィンドウを閉じる
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="windowType"></param>
        /// <returns></returns>
        public void Close (object vm, bool? dialogResult) {
            var tuple = _openWindowDict.Values.FirstOrDefault(x => x.Item1.DataContext == vm);

            if (tuple == null) {
                return;
            }

            CloseWindow(tuple.Item1, tuple.Item2, dialogResult);
        }

        private void CloseWindow (Window window, OpenMode openMode , bool? dialogResult) {
            switch (openMode) {
                case OpenMode.Normal:
                    window.Close();
                    break;

                case OpenMode.Dialog:
                    window.DialogResult = dialogResult;
                    break;
            }
        }


        private bool ActivateExistingWindow<T>(out Type windowType) where T : Window, new() {
            windowType = typeof(T);

            // 既に開いているウィンドウがあれば、単に再表示してreturn
            if (_openWindowDict.TryGetValue(windowType, out var existWindowInfo)) {
                if (existWindowInfo.Item1.WindowState == WindowState.Minimized) {
                    existWindowInfo.Item1.WindowState = WindowState.Normal;
                }
                existWindowInfo.Item1.Activate();
                return true;
            }

            return false;
        }

        private T CreateNewWindow<T>(object? vm, Type windowType, OpenMode mode) where T : Window, new() {
            T newWindow = new T();

            if (vm != null) {
                newWindow.DataContext = vm;
            }

            _openWindowDict.Add(windowType, new Tuple<Window, OpenMode>(newWindow, mode));
            newWindow.Closed += (s, e) => _openWindowDict.Remove(windowType);
            return newWindow;
        }
    }
}
