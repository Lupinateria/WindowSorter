using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WindowSorter.Core;

namespace WindowSorter.Model.Grouping {
    public static class WindowEnumerator {
        // Process ID -> Process名のキャッシュ
        private static Dictionary<uint, string> _processNameCache = new Dictionary<uint, string>();

        /// <summary>
        /// ウィンドウを列挙して返す
        /// </summary>
        /// <param name="ignoreList">除外条件リスト</param>
        /// <returns></returns>
        public static List<WindowInformation> GetWindows(List<Condition> ignoreList = null) {
            // 戻り値
            List<WindowInformation> windowList = new();

            // 今回走査時のProcessID
            HashSet<uint> pids = new();

            NativeMethods.EnumWindows(
                new NativeMethods.EnumWindowsDelegate((hWnd, lParam) => {
                    if (!NativeMethods.IsWindowVisible(hWnd))
                        return true;

                    int textLen = NativeMethods.GetWindowTextLength(hWnd);
                    if (0 >= textLen)
                        return true;

                    StringBuilder className = new StringBuilder(256);
                    NativeMethods.GetClassName(hWnd, className, className.Capacity);

                    // Progman (プログラムマネージャー/デスクトップ壁紙部分) は除外
                    if (className.ToString().Equals("Progman"))
                        return true;

                    // DWM (Desktop Window Manager) によって隠されている (Cloaked) かどうかを確認
                    // ストアアプリなどがバックグラウンドで非表示状態になっている場合を除外するために必要
                    NativeMethods.DwmGetWindowAttribute(hWnd, NativeMethods.DWMWINDOWATTRIBUTE.Cloaked, out bool isCloaked, Marshal.SizeOf(typeof(bool)));
                    if (isCloaked)
                        return true;

                    StringBuilder windowTitle = new StringBuilder(textLen + 1);
                    NativeMethods.GetWindowText(hWnd, windowTitle, windowTitle.Capacity);

                    // ウィンドウハンドル -> プロセスID取得
                    uint processID;
                    NativeMethods.GetWindowThreadProcessId(hWnd, out processID);

                    // 自分自身（このアプリのウィンドウ）は検索結果から除外
                    if (System.Diagnostics.Process.GetCurrentProcess().Id == processID) {
                        return true;
                    }

                    pids.Add(processID);

                    string processName;
                    if (_processNameCache.ContainsKey(processID)) {
                        // キャッシュヒット
                        processName = _processNameCache[processID];

                    } else {
                        // ミスヒット
                        StringBuilder processFileName = new StringBuilder(1024);

                        // プロセス名取得
                        nint hProcess = NativeMethods.OpenProcess(
                            NativeMethods.ProcessAccessFlags.QueryLimitedInformation,
                            false,
                            (int)processID);

                        int size = processFileName.Capacity;
                        NativeMethods.QueryFullProcessImageName(hProcess, 0, processFileName, ref size);
                        NativeMethods.CloseHandle(hProcess);

                        processName = Path.GetFileName(processFileName.ToString()) ?? "";

                        // キャッシュに積む
                        _processNameCache.Add(processID, processName);
                    }

                    var window = new WindowInformation() {
                        WindowTitle = windowTitle.ToString(),
                        WindowClassName = className.ToString(),
                        Handle = hWnd,
                        ProcessName = processName
                    };

                    // 除外条件に一致するかチェック
                    if (ignoreList != null && ignoreList.Any(c => c.Match(window))) {
                        return true;
                    }

                    windowList.Add(window);

                    return true;
                }),
                nint.Zero);

            // 今回走査時のProcessIDでキャッシュを作り直す
            _processNameCache = _processNameCache
                .Where(x => pids.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value);

            return windowList;
        }
    }
}
