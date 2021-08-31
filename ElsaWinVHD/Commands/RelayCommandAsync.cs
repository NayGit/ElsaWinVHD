using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ElsaWinVHD.Commands
{
    public class RelayCommandAsync : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Predicate<object> _canExecute;
        private bool isExecuting;

        public RelayCommandAsync(Func<Task> execute) : this(execute, null) { }

        public RelayCommandAsync(Func<Task> execute, Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            if (!isExecuting && _canExecute == null) return true;
            return !isExecuting && _canExecute(parameter);
        }

        public async void Execute(object parameter)
        {
            isExecuting = true;
            try { await _execute(); }
            finally 
            { 
                isExecuting = false; 
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public class RelayCommandAsync<T> : ICommand
    {
        private readonly Func<T, Task> _execute;
        private readonly Predicate<T> _canExecute;
        private bool isExecuting;

        public RelayCommandAsync(Func<T, Task> execute) : this(execute, null) { }

        public RelayCommandAsync(Func<T, Task> execute, Predicate<T> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            if (!isExecuting && _canExecute == null) return true;
            return !isExecuting && _canExecute((T)parameter);
        }

        public async void Execute(object parameter)
        {
            isExecuting = true;
            try { await _execute((T)parameter); }
            finally
            {
                isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }

        
    }
}
