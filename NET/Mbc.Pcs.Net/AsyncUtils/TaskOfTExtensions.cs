using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mbc.Pcs.Net.AsyncUtils
{
    /// <summary>
    /// Hilfsmethoden für Tasks.
    /// </summary>
    public static class TaskOfTExtensions
    {
        /// <summary>
        /// Liefert eine Task, die entweder normal beendet wird oder über
        /// einen Timeout.
        /// </summary>
        public static Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout)
        {
            return task.TimeoutAfter(timeout, CancellationToken.None);
        }

        /// <summary>
        /// Liefert eine Task, die entweder normal beendet wird oder über
        /// einen Timeout.
        /// </summary>
        public static Task<T> TimeoutAfter<T>(this Task<T> task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // Implementation from https://blogs.msdn.microsoft.com/pfxteam/2011/11/10/crafting-a-task-timeoutafter-method/

            // Short-circuit #1: infinite timeout or task already completed
            if (task == null || task.IsCompleted || timeout == TimeSpan.MaxValue)
            {
                // Either the task has already completed or timeout will never occur.
                // No proxy necessary.
                return task;
            }

            // Short-circuit #2: zero timeout
            if (timeout == TimeSpan.Zero)
            {
                // We've already timed out.
                return Task.FromException<T>(new TimeoutException());
            }

            // tcs.Task will be returned as a proxy to the caller
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();

            // Set up a timer to complete after the specified timeout period
            var timer = new Timer(
                state => ((TaskCompletionSource<T>)state).TrySetException(new TimeoutException()),
                tcs,
                timeout,
                TimeSpan.FromMilliseconds(-1));

            // Register cancellation callback
            var cancellationTokenRegistration = cancellationToken.Register(
                (state) => ((TaskCompletionSource<T>)state).TrySetCanceled(),
                tcs);

            // Wire up the logic for what happens when source task completes
            task.ContinueWith(
                (antecedent, state) => MarshalTaskResults(antecedent, (TaskCompletionSource<T>)state),
                tcs,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            var proxyTask = tcs.Task;
            proxyTask.ContinueWith(
                (antecedent, state) =>
                {
                    // Recover our state data
                    var tuple = (Tuple<Timer, CancellationTokenRegistration>)state;

                    // Cancel the Timer
                    tuple.Item1.Dispose();

                    // Unregister cancel callback
                    tuple.Item2.Dispose();
                },
                Tuple.Create(timer, cancellationTokenRegistration),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return proxyTask;
        }

        private static void MarshalTaskResults<TResult>(Task<TResult> source, TaskCompletionSource<TResult> proxy)
        {
            switch (source.Status)
            {
                case TaskStatus.Faulted:
                    proxy.TrySetException(source.Exception.InnerException);
                    break;
                case TaskStatus.Canceled:
                    proxy.TrySetCanceled();
                    break;
                case TaskStatus.RanToCompletion:
                    proxy.TrySetResult(source.Result);
                    break;
            }
        }
    }
}
