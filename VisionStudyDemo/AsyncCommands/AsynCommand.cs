using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace VisionStudyDemo.AsyncCommands
{
    public static class AsyncCommand
    {
        public static AsyncCommand<object> Create(Func<Task> command)
        {
            return new AsyncCommand<object>(async _ => { await command(); return null; });
        }

        public static AsyncCommand<TResult> Create<TResult>(Func<Task<TResult>> command)
        {
            return new AsyncCommand<TResult>(_ => command());
        }

        public static AsyncCommand<object> Create(Func<CancellationToken, Task> command)
        {
            return new AsyncCommand<object>(async token => { await command(token); return null; });
        }

        public static AsyncCommand<TResult> Create<TResult>(Func<CancellationToken, Task<TResult>> command)
        {
            return new AsyncCommand<TResult>(command);
        }

        public static AsyncCommand<object> Create(Func<object, CancellationToken, Task> command)
        {
            return new AsyncCommand<object>(async (parameter, token) => { await command(parameter, token); return null; });
        }
    }


    public abstract class CommandBase : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public virtual bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter) == false)
                return;

            OnExecute(parameter);
        }

        protected abstract void OnExecute(object parameter);

        protected void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
    {
        #region property

        public string ErrorMessage => InnerException?.InnerException.Message;
        public AggregateException Exception => Task.Exception;
        public Exception InnerException => Exception?.InnerException;
        public bool IsCanceled => Task.IsCanceled;
        public bool IsCompleted => Task.IsCompleted;
        public bool IsFaulted => Task.IsFaulted;
        public bool IsNotCompleted => !Task.IsCompleted;
        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;

        public TResult Result => (Task.Status == TaskStatus.RanToCompletion) ?
            Task.Result : default(TResult);

        public TaskStatus Status => Task.Status;
        public Task<TResult> Task { get; private set; }

        public Task TaskCompletion { get; private set; }

        #endregion property

        public NotifyTaskCompletion(Task<TResult> task)
        {
            Task = task;
            TaskCompletion = WatchTaskAsync(task);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NotifyPropertiesChanged(Task task)
        {
            var handle = PropertyChanged;
            if (handle == null)
            {
                return;
            }

            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(IsCompleted));
            OnPropertyChanged(nameof(IsNotCompleted));
            if (task.IsCanceled)
            {
                OnPropertyChanged(nameof(IsCanceled));
            }
            else if (task.IsFaulted)
            {
                OnPropertyChanged(nameof(IsFaulted));
                OnPropertyChanged(nameof(Exception));
                OnPropertyChanged(nameof(InnerException));
                OnPropertyChanged(nameof(ErrorMessage));
            }
            else
            {
                OnPropertyChanged(nameof(IsSuccessfullyCompleted));
                OnPropertyChanged(nameof(Result));
            }
        }

        private async Task WatchTaskAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                NotifyPropertiesChanged(task);
            }
        }
    }

    internal class CancelAsyncCommand : CommandBase
    {
        private bool _commandExecuting;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        public CancellationToken Token => _cts.Token;

        public override bool CanExecute(object parameter)
        {
            return _commandExecuting && !_cts.IsCancellationRequested;
        }

        public void NotifyCommandFinished()
        {
            _commandExecuting = false;
            RaiseCanExecuteChanged();
        }

        public void NotifyCommandStarting()
        {
            _commandExecuting = true;
            if (!_cts.IsCancellationRequested)
                return;
            _cts = new CancellationTokenSource();
            RaiseCanExecuteChanged();
        }

        protected override void OnExecute(object parameter)
        {
            _cts.Cancel();
            RaiseCanExecuteChanged();
        }
    }
    public class AsyncCommand<TResult> : CommandBase, INotifyPropertyChanged
    {
        #region fields

        private readonly CancelAsyncCommand _cancelCommand;
        private readonly Func<object, CancellationToken, Task<TResult>> _command;
        private NotifyTaskCompletion<TResult> _execution;

        #endregion fields

        #region properties

        public ICommand CancelCommand => _cancelCommand;

        public NotifyTaskCompletion<TResult> Execution
        {
            get { return _execution; }
            private set
            {
                _execution = value;
                OnPropertyChanged();
            }
        }

        #endregion properties

        public AsyncCommand(Func<CancellationToken, Task<TResult>> command)
        {
            _command = (arg1, token) => command(token);
            _cancelCommand = new CancelAsyncCommand();
        }

        public AsyncCommand(Func<object, CancellationToken, Task<TResult>> command)
        {
            _command = command;
            _cancelCommand = new CancelAsyncCommand();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public override bool CanExecute(object parameter)
        {
            return Execution == null || Execution.IsCompleted;
        }

        public async Task ExecuteAsync(object parameter)
        {
            _cancelCommand.NotifyCommandStarting();
            Execution = new NotifyTaskCompletion<TResult>(_command(parameter, _cancelCommand.Token));
            OnPropertyChanged();
            await Execution.TaskCompletion;
            _cancelCommand.NotifyCommandFinished();
            OnPropertyChanged();
        }

        protected override async void OnExecute(object parameter)
        {
            await ExecuteAsync(parameter);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
