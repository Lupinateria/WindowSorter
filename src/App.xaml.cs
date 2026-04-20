using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WindowSorter.Core;
using WindowSorter.Model;
using WindowSorter.Model.HotKey;
using WindowSorter.View;
using WindowSorter.View.Settings;
using Application = System.Windows.Application;
using Forms = System.Windows.Forms;

namespace WindowSorter {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        // メインウィンドウ
        public MainWindow mainWindow;

        // タスクトレイ用
        private NotifyIcon notifyIcon;

        ~App() {
            this.notifyIcon.Dispose();
            HotKeyService.UnRegister();
        }

        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            // --------------------------------------------------------------------------
            // NotifyIcon 作成
            // --------------------------------------------------------------------------
            InitializeNotifyIcon();

            // --------------------------------------------------------------------------
            // メインウィンドウ 作成
            // --------------------------------------------------------------------------
            this.mainWindow = new MainWindow();

            var handle = new WindowInteropHelper(mainWindow).EnsureHandle();

            // --------------------------------------------------------------------------
            // 設定ファイル 読み込み
            // --------------------------------------------------------------------------
            SettingsService.Load();

            // --------------------------------------------------------------------------
            // ホットキー登録
            // --------------------------------------------------------------------------
            HotKeyService.Initialize(
                handle,
                (sender, e2) => {
                    // ウィンドウ 表示
                    this.mainWindow.ToggleWindowVisible();
                }
            );

            HotKeyService.Register(SettingsService.Current.HotKeyModifier, SettingsService.Current.HotKey);
        }

        // NotifyIcon初期化
        private void InitializeNotifyIcon() {
            // ----------------------------------------------------
            // 初期化
            // ----------------------------------------------------
            this.notifyIcon = new NotifyIcon();
            notifyIcon.Visible = true;
            notifyIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/icon/icon.ico")).Stream);
            this.notifyIcon.Text = "Window Sorter";

            // アイコンクリックで表示
            this.notifyIcon.Click += (sender, e) => {
                if (e is Forms.MouseEventArgs mouseEvent && mouseEvent.Button == Forms.MouseButtons.Left) {
                    // NOTE:
                    // 画面外クリックで非表示 -> マウスアップでイベント発火 -> 表示の順番になるので、
                    // 再表示されることがある。
                    // 直すのが面倒なので放置。
                    this.mainWindow.ShowWindow(true);
                }

            };

            // ----------------------------------------------------
            // NotifyIcon コンテキストメニュー
            // ----------------------------------------------------
            Forms.ContextMenuStrip menu = new Forms.ContextMenuStrip();

            // 設定
            Forms.ToolStripMenuItem openSettingsMenuItem = new Forms.ToolStripMenuItem();
            openSettingsMenuItem.Text = "&設定";
            openSettingsMenuItem.Click += (s, e) => {
                if (WindowService.Instance.ShowDialog<SettingsMainWindow>(mainWindow) == true) {
                    // 設定保存後にホットキーを再登録
                    HotKeyService.Register(SettingsService.Current.HotKeyModifier, SettingsService.Current.HotKey);
                }
            };
            menu.Items.Add(openSettingsMenuItem);

            // About
            Forms.ToolStripMenuItem aboutMenuItem = new Forms.ToolStripMenuItem();
            aboutMenuItem.Text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyTitleAttribute>()?.Title + "について(&A)";
            aboutMenuItem.Click += (s, e) => {
                WindowService.Instance.ShowDialog<AboutWindow>(mainWindow);
            };
            menu.Items.Add(aboutMenuItem);

            // 終了
            Forms.ToolStripMenuItem exitMenuItem = new Forms.ToolStripMenuItem();
            exitMenuItem.Text = "&終了";
            exitMenuItem.Click += (s, e) => {
                this.notifyIcon.Dispose();
                Application.Current.Shutdown();
            };
            menu.Items.Add(exitMenuItem);
            this.notifyIcon.ContextMenuStrip = menu;
        }
    }
}
