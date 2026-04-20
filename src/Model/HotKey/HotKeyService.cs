using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;
using WindowSorter.Core;

namespace WindowSorter.Model.HotKey {
    public static class HotKeyService {
        private static IntPtr _handle;
        private static HwndSource _hwndSource;
        private static bool _isInitialized = false;

        private static event EventHandler HotKeyPressed;

        // 初期化
        public static void Initialize(IntPtr handle, EventHandler eventHandler) {
            if (_isInitialized) return;

            _handle = handle;
            HotKeyPressed = eventHandler;

            // メッセージループ Hook
            _hwndSource = HwndSource.FromHwnd(handle);
            _hwndSource.AddHook(HwndHook);

            _isInitialized = true;
        }

        // ホットキー 登録
        public static void Register(ModifierKeys modifierKeys, Key key) {
            UnRegister();

            // キーが未設定の場合は何もしない
            if (key == Key.None) return;

            int mod = (int)modifierKeys;
            int vk = KeyInterop.VirtualKeyFromKey(key);

            NativeMethods.RegisterHotKey(
                _handle,
                (int)NativeMethods.WM_HOTKEY,
                mod,
                vk
            );
        }

        // ホットキー 解除
        public static void UnRegister() {
            if (_handle != IntPtr.Zero) {
                NativeMethods.UnregisterHotKey(_handle, NativeMethods.WM_HOTKEY);
            }
        }

        // メッセージループ 本体
        private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            if (msg == NativeMethods.WM_HOTKEY) {
                HotKeyPressed?.Invoke(null, EventArgs.Empty);
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
