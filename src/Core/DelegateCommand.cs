using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WindowSorter.Core {
    public class DelegateCommand : ICommand {
        private Action<object> _execute;

        private Func<object, bool> _canExecute;

        #region コンストラクタ
        public DelegateCommand(Action<object> execute) : this(execute, null) {
        }

        public DelegateCommand(Action<object> execute, Func<object, bool> canExecute) {
            this._execute = execute;
            this._canExecute = canExecute;
        }
        #endregion

        #region ICommandインターフェースの実装
        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged() {
            var h = this.CanExecuteChanged;
            if (h != null) {
                h(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object parameter) {
            return (this._canExecute != null) ? this._canExecute(parameter) : true;
        }

        public void Execute(object parameter) {
            if (this._execute != null) {
                this._execute(parameter);
            }
        }
        #endregion
    }
}
