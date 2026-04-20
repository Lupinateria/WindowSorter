using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WindowSorter.Core;
using WindowSorter.Model.Grouping;
using WindowSorter.ViewModel.Settings;

namespace WindowSorter.View.Settings {
    /// <summary>
    /// SettingsMainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingsMainWindow : Window {
        public SettingsMainWindow() {
            InitializeComponent();
        }

        private void HotKeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            e.Handled = true;

            // 修飾キーの状態を取得
            ModifierKeys modifier = Keyboard.Modifiers;

            // 実際に押されたキーを取得
            Key key = (e.Key == Key.System) ? e.SystemKey : e.Key;

            // 修飾キー自体がメインキーとして押された場合は無視
            if (key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LWin || key == Key.RWin) {
                return;
            }

            if (DataContext is SettingsDataVM vm) {
                vm.SetHotKey(modifier, key);
            }
        }
    }
}
