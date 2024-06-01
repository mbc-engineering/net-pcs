using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mbc.Pcs.Net.AsyncUtils
{
    /// <summary>
    /// Führt Code asynchron, serialisiert aus.
    /// </summary>
    public class AsyncSerializedTaskExecutor : IDisposable
    {
        [ThreadStatic]
        private static AsyncSerializedTaskExecutor _executingExecutor;

        private readonly BlockingCollection<Action> _executionQueue = new BlockingCollection<Action>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private int _queuedTasks;
        private Task _executingTask;

        #region IDisposable
        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _executingTask?.Wait();
            _executionQueue.Dispose();
        }
        #endregion

        public EventHandler<Exception> UnhandledException { get; set; }

        public bool IsCalledFromExecutor => _executingExecutor == this;

        public int ExecutionQueueLength => _executionQueue.Count;

        public void Execute(Action act)
        {
            _executionQueue.Add(act);

            if (Interlocked.Increment(ref _queuedTasks) == 1)
            {
                _executingTask = Task.Run(() => ExecutionMain(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            }
        }

        public Task ExecuteAsync(Action action)
        {
            var taskSource = new TaskCompletionSource<object>();

            Action wrapperAction = () =>
            {
                try
                {
                    action();
                    taskSource.SetResult(null);
                }
                catch (Exception e)
                {
                    taskSource.SetException(e);
                }
            };

            Execute(wrapperAction);

            return taskSource.Task;
        }

        public Task<TResult> ExecuteAsync<TResult>(Func<TResult> func)
        {
            var taskSource = new TaskCompletionSource<TResult>();

            Action wrapperAction = () =>
            {
                try
                {
                    var result = func();
                    taskSource.SetResult(result);
                }
                catch (Exception e)
                {
                    taskSource.SetException(e);
                }
            };

            Execute(wrapperAction);

            return taskSource.Task;
        }

        public void WaitForExecution()
        {
            _executingTask?.Wait();
        }

        private void ExecutionMain(CancellationToken cancellationToken)
        {
            var last = false;
            Action action;

            while (!last && !cancellationToken.IsCancellationRequested && _executionQueue.TryTake(out action))
            {
                _executingExecutor = this;

                try
                {
                    action();
                }
                catch (Exception e)
                {
                    UnhandledException?.Invoke(this, e);
                }
                finally
                {
                    _executingExecutor = null;
                    if (Interlocked.Decrement(ref _queuedTasks) == 0)
                    {
                        last = true;
                    }
                }
            }
        }
    }
}
