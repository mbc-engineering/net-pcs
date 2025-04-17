using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mbc.Pcs.Net.AsyncUtils
{
    /// <summary>
    /// Hilfsmethoden für Tasks.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Liefert eine Task, die entweder normal beendet wird oder über einen Timeout.
        /// Das Time-Out darf maximal 49 Tage sein und muss grösser als 0 ms sein.
        /// </summary>
        public static Task TimeoutAfter(this Task task, TimeSpan timeout)
        {
            return task.TimeoutAfter(timeout, CancellationToken.None);
        }

        /// <summary>
        /// Liefert eine Task, die entweder normal beendet wird oder über einen Timeout oder über ein cancelatinToken
        /// Das Time-Out darf maximal 49 Tage sein und muss grösser als 0 ms sein.
        /// </summary>
        public static Task TimeoutAfter(this Task task, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // Implementation from https://blogs.msdn.microsoft.com/pfxteam/2011/11/10/crafting-a-task-timeoutafter-method/
            //
            // Short-circuit #1: Check valid timeouts (we must be in range of the Timer)
            if (timeout > TimeSpan.FromDays(49))
            {
                throw new ArgumentOutOfRangeException("Time-out interval must be less than 49 days.");
            }

            if (timeout <= TimeSpan.FromMilliseconds(0))
            {
                throw new ArgumentOutOfRangeException("Time-out interval must be more than 0 milliseconds.");
            }

            // Short-circuit #2: task already completed
            if (task == null || task.IsCompleted)
            {
                // Either the task has already completed or timeout will never occur.
                // No proxy necessary.
                return task;
            }

            // tcs.Task will be returned as a proxy to the caller
            TaskCompletionSource<object> tcs = new TaskCompletionSource<object>();

            // Set up a timer to complete after the specified timeout period
            var timer = new Timer(
                state => ((TaskCompletionSource<object>)state).TrySetException(new TimeoutException()),
                tcs,
                timeout,
                TimeSpan.FromMilliseconds(-1));

            // Register cancellation callback
            var cancellationTokenRegistration = cancellationToken.Register(
                (state) => ((TaskCompletionSource<object>)state).TrySetCanceled(),
                tcs);

            // Wire up the logic for what happens when source task completes
            task.ContinueWith(
                (antecedent, state) => MarshalTaskResults(antecedent, (TaskCompletionSource<object>)state),
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

        private static void MarshalTaskResults(Task source, TaskCompletionSource<object> proxy)
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
                    proxy.TrySetResult(null);
                    break;
            }
        }
    }
}
