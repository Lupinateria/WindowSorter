using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WindowSorter.Core;

namespace WindowSorter.Model.Grouping {
    public class WindowInformation {
        // メイン情報
        public string WindowTitle { get; set; } = "";
        public string WindowClassName { get; set; } = "";
        public nint Handle { get; set; } = nint.Zero;
        public string ProcessName { get; set; } = "";

        public bool BringWindowToTop() {
            if (NativeMethods.IsIconic(Handle)) {
                NativeMethods.ShowWindowAsync(Handle, NativeMethods.SW_RESTORE);
            }
            NativeMethods.BringWindowToTop(Handle);
            return true;
        }

        public bool PostSCCLOSE() {
            return NativeMethods.PostMessage(
                new HandleRef(this, Handle),
                0x0112,
                0xF060,
                nint.Zero);
        }
    }
}
