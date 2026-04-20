using System;
using System.Reflection;
using System.Windows;

namespace WindowSorter.View {
    /// <summary>
    /// AboutWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AboutWindow : Window {
        public AboutWindow() {
            InitializeComponent();

            var assembly = Assembly.GetExecutingAssembly();
            // AssemblyTitleAttribute からタイトルを取得
            var title = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
            var version = assembly.GetName().Version?.ToString() ?? "0.0.0.0";

            this.Title = $"{title} について"; // ウィンドウ自体のタイトル
            this.AppNameText.Text = title;    // 画面内のアプリ名
            this.VersionText.Text = $"Version {version}";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }
    }
}
