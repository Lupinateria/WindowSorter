using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WindowSorter.Core {
    public interface IWindowService {
        public void ShowWindow<T>(object? vm = null) where T : Window, new();
        public bool? ShowDialog<T>(Window owner, object? vm = null) where T : Window, new();
    }
}
