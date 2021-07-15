using SharedLibrary.Provider;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SharedLibrary.Commands
{
    public class AsyncCommand : ICommand
    {
        private readonly Func<object, Task> _execute;
        private readonly Predicate<object> _canExecute;
        private bool isExecuting;

        public AsyncCommand(Func<object, Task> execute) : this(execute, null) { }

        public AsyncCommand(Func<object, Task> execute, Predicate<object> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (!isExecuting && _canExecute == null) return true;
            return (!isExecuting && _canExecute(parameter));
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public async void Execute(object parameter)
        {
            isExecuting = true;
            try
            {
                SharedProvider.SetActiveTask(_execute);
                await _execute(parameter);
            }
            finally
            {
                SharedProvider.RemoveActiveTask(_execute);
                isExecuting = false;
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}