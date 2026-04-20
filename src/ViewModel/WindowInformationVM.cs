using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowSorter.Core;
using WindowSorter.Model.Grouping;

namespace WindowSorter.ViewModel {
    public class WindowInformationVM : NotificationObject {
        private WindowInformation _windowInformation;
        public WindowInformation WindowInformation { get { return _windowInformation; } }

        public string WindowTitle => _windowInformation.WindowTitle;
        public string ProcessName => _windowInformation.ProcessName;
        public IntPtr Handle => _windowInformation.Handle;

        private bool _isSelected;
        public bool IsSelected {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        // 最前面に表示
        public DelegateCommand TopMostCommand { get; }

        private readonly Action _onTopMostCommandExecuted;

        public WindowInformationVM(WindowInformation windowInformation, Action onTopMostCommandExecuted = null) {
            _windowInformation = windowInformation;
            _onTopMostCommandExecuted = onTopMostCommandExecuted;

            this.TopMostCommand = new DelegateCommand((e) => {
                _windowInformation.BringWindowToTop();
                _onTopMostCommandExecuted?.Invoke();
            });
        }
    }
}
