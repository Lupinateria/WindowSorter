using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using WindowSorter.Core;
using WindowSorter.Model.HotKey;
using WindowSorter.Model.Settings;
using WindowSorter.View.Settings;
using WindowSorter.ViewModel;
using Point = System.Windows.Point;

namespace WindowSorter.View {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public double ThumbnailWidth => SettingsService.Current.ThumbnailWidth;
        public double ThumbnailHeight => SettingsService.Current.ThumbnailHeight;
        public double WindowItemWidth => ThumbnailWidth + 8; // BorderThickness(2*2) + Padding(2*2)

        private static readonly double SCREEN_SIZE_RATIO = 0.82;

        private List<(WindowInformationVM VM, Border Element, Point Pos)> _thumbnaiInfoList = new ();

        public MainWindow() {
            InitializeComponent();

            ApplyScreenSize();

            // ---------------------------------------------------
            // イベントハンドラ
            // ---------------------------------------------------
            // 閉じるボタン
            this.Closing += (sender, e) => {
                e.Cancel = true;
                this.HideWindow();
            };

            // ウィンドウ外クリックで非表示(Deactivatedを補足)
            this.Deactivated += (sender, e) => {
#if !DEBUG
                this.HideWindow();
#endif
            };

            // LayoutUpdated
            // サムネイルの座標情報を更新
            this.LayoutUpdated += (sender, e) => {
                this.UpdateThumbnailPos();
            };

            // KeyDownイベント
            this.PreviewKeyDown += (s, e) => {
                switch (e.Key) {
                case Key.Escape:
                    if (SearchTextBox.IsKeyboardFocused) {
                        // 検索ボックスにフォーカスが当たっていたら、
                        // フォーカスを外す
                        MainScrollViewer.Focus();
                    } else {
                        // 検索ボックスにフォーカスがない場合は、
                        // 画面を閉じる
                        this.HideWindow();
                    }
                    e.Handled = true;
                    break;

                case Key.Enter: {
                    // Enter
                    if (this.DataContext is MainWindowVM vm) {
                        if (e.Key == Key.Enter) {
                            if (vm.SelectedWindow != null) {
                                vm.SelectedWindow.TopMostCommand.Execute(null);
                                this.HideWindow();
                                e.Handled = true;
                            }
                        }
                    }
                    break;
                }
                case Key.Left:
                case Key.Right:
                case Key.Up:
                case Key.Down: {
                    if (this.DataContext is MainWindowVM vm) {
                        // サムネイルの座標情報
                        if (_thumbnaiInfoList.Count == 0) return;

                        // 今選択状態のサムネイル
                        var current = _thumbnaiInfoList.FirstOrDefault(x => x.VM == vm.SelectedWindow);

                        // 移動先のサムネイル のViewModel
                        object? nextVM = null;

                        if (e.Key == Key.Left || e.Key == Key.Right) {
                            // 左右キー
                            int index = _thumbnaiInfoList.IndexOf(current);

                            int nextIndex = (e.Key == Key.Left) ? index - 1 : index + 1;
                            
                            if (nextIndex >= 0 && nextIndex < _thumbnaiInfoList.Count) {
                                nextVM = _thumbnaiInfoList[nextIndex].VM;
                            }
                        } else {
                            // 上下キー
                            var candidates = _thumbnaiInfoList.Where(x => {
                                if (e.Key == Key.Up) {
                                    // 上：自分より上にあるサムネイル
                                    return x.Pos.Y < current.Pos.Y;
                                } else {
                                    // 下：自分より下にあるサムネイル
                                    return x.Pos.Y > current.Pos.Y;
                                }

                            }).ToList();

                            if (candidates.Count != 0) {
                                // 先にY座標を決める ... Y座標は各行で揃ってレイアウトされている前提
                                double targetY;
                                if (e.Key == Key.Up) {
                                    // 上：自分より上にあるサムネイルの中で、一番大きいY座標が、一番近いサムネイル
                                    targetY = candidates.Max(x => x.Pos.Y);
                                } else {
                                    // 下：自分よ下にあるサムネイルの中で、一番小さいY座標が、一番近いサムネイル
                                    targetY = candidates.Min(x => x.Pos.Y);
                                }

                                // X座標が最も近いものを選択
                                nextVM = candidates
                                    .Where(x => x.Pos.Y == targetY)
                                    .OrderBy(x => Math.Abs(x.Pos.X - current.Pos.X))
                                    .FirstOrDefault().VM;
                            }
                        }

                        if (nextVM is WindowInformationVM target) {
                            vm.SelectedWindow = target;

                            // スクロール
                            var targetElement = _thumbnaiInfoList.First(x => x.VM == target).Element;
                            targetElement.BringIntoView();
                        }
                        e.Handled = true;
                    }

                    break;
                }

                case Key.F:
                    if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control) {
                        SearchTextBox.Focus();
                        e.Handled = true;
                    }
                    break;

                default:
                    break;
                }
            };
        }

        private long _lastHideTime = 0;

        // ウィンドウ表示
        // 白いチラツキを抑止するために対策している
        public void ShowWindow(bool iconClicked = false) {
            // NOTE:
            // アイコンクリックで非表示にならずに再表示されることへの対策
            // 250ms以下なら、反応しない。
            if (iconClicked && System.DateTime.Now.Ticks / 10000 - _lastHideTime < 250) {
                return;
            }

            var helper = new WindowInteropHelper(this);
            IntPtr handle = helper.EnsureHandle();

            // サムネイル非表示 (一瞬サムネイルだけ表示されるのを回避)
            DwmThumbnailView.IsThumbnailReady = false;

            // DWM APIでクローク
            int cloak = 1;
            NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DWMWA_CLOAK, ref cloak, sizeof(int));

            // ウィンドウ表示
            // Cloakされているのでまだ見えない
            if (!this.IsVisible) {
                this.Show();
            }
            this.Activate();

            // ウィンドウ一覧を更新
            if (this.DataContext is MainWindowVM vm) {
                vm.Refresh();
                this.UpdateLayout();
                MainScrollViewer.ScrollToTop();
            }

            // サムネイル表示
            DwmThumbnailView.IsThumbnailReady = true;

            // WPFの描画を待ってからUncloak
            EventHandler? handler = null;
            handler = (s, e) => {
                CompositionTarget.Rendering -= handler;

                _ = this.Dispatcher.InvokeAsync(async () => {
                    // さらに少しだけ待つ
                    await Task.Delay((int)SettingsService.Current.ShowWindowDelay);
                    
                    NativeMethods.DwmFlush();

                    // Uncloak
                    int uncloak = 0;
                    NativeMethods.DwmSetWindowAttribute(handle, NativeMethods.DWMWA_CLOAK, ref uncloak, sizeof(int));

                    // 最前面、フォーカスを当てて、マウスホイールを確実に反応させる
                    NativeMethods.SetForegroundWindow(new WindowInteropHelper(this).Handle);
                    SearchTextBox.Focus();

                    // サムネイル座標を取得
                    UpdateThumbnailPos();

                }, DispatcherPriority.ApplicationIdle);
            };
            CompositionTarget.Rendering += handler;
        }

        // ウィンドウ非表示
        public void HideWindow() {
            // 設定画面などの「子ウィンドウ」を開いている間は、メインウィンドウを隠さないようにする
            if (this.OwnedWindows.Count == 0) {
                DwmThumbnailView.IsThumbnailReady = false;
                this.Hide();
                _lastHideTime = System.DateTime.Now.Ticks / 10000;
            }
        }

        // ウィンドウ表示・非表示をトグルする
        public void ToggleWindowVisible() {
            if (IsVisible && IsActive) {
                HideWindow();
            } else {
                ShowWindow();
            }
        }

        // ウィンドウサムネイルの座標情報を取得
        private void UpdateThumbnailPos() {
            var results = new List<(WindowInformationVM VM, Border Element, Point Pos)>();

            foreach (var border in FindVisualChildren<Border>(GroupList)) {
                if (border.Name != "WindowItemBorder")
                    continue;

                if (border.DataContext is not WindowInformationVM vm)
                    continue;

                try {
                    var pos = border.TransformToAncestor(MainScrollViewer).Transform(new Point(0, 0));
                    results.Add((vm, border, pos));
                } catch {

                }
            }
            this._thumbnaiInfoList = results;
        }


        // 指定した要素から、指定した型の子要素を再帰的に探す
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj) where T : DependencyObject {
            if (depObj != null) {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T) {
                        yield return (T)child;
                    }
                    foreach (T childOfChild in FindVisualChildren<T>(child)) {
                        yield return childOfChild;
                    }
                }
            }
        }

        private Point _lastMousePosition;

        // マウス移動で選択状態を更新
        private void WindowItem_MouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            // ウィンドウ全体からの相対座標を取得
            Point currentPos = e.GetPosition(this);

            // マウスが物理的に動いていない（UIが下で動いただけ）なら無視
            if (currentPos == _lastMousePosition)
                return;

            _lastMousePosition = currentPos;

            if (sender is FrameworkElement element && element.DataContext is WindowInformationVM windowVM) {
                if (this.DataContext is MainWindowVM vm) {
                    if (vm.SelectedWindow == windowVM)
                        return;

                    vm.SelectedWindow = windowVM;
                }
            }
        }

        // ウィンドウ全体の位置、大きさ調整
        private void ApplyScreenSize() {
            // サイズはプライマリウィンドウのサイズに対する割合にする
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            this.Width = screenWidth * SCREEN_SIZE_RATIO;
            this.Height = screenHeight * SCREEN_SIZE_RATIO;
            this.Left = (screenWidth - this.Width) / 2;
            this.Top = (screenHeight - this.Height) / 2;
        }

        private void MenuSettings_Click(object sender, RoutedEventArgs e) {
            if (WindowService.Instance.ShowDialog<SettingsMainWindow>(this) == true) {
                // 設定保存後にホットキーを再登録
                HotKeyService.Register(SettingsService.Current.HotKeyModifier, SettingsService.Current.HotKey);
            }
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e) {
            WindowService.Instance.ShowDialog<AboutWindow>(this);
        }
    }
}