using Optional;
using System;
using System.Threading;

namespace Mbc.Pcs.Net.AsyncUtils
{
    /// <summary>
    /// Hilfsklasse um die Ausführung von Code zu überwachen, z.B. bei
    /// Event-Handlern beim Schliessen.
    /// <para>Diese Klasse stellt eine Möglichkeit zur Verfügung um
    /// die Ausführung von Code zu deaktivieren. Beim Deaktivieren wird
    /// geprüft ob gerade eine Ausführung stattfindet.</para>
    /// </summary>
    public class MonitoredExecution
    {
        private readonly object _lock = new object();
        private long _runningExecution;
        private volatile bool _disabled;

        public bool Execute(Action act)
        {
            return Execute<object, object, object>((a1, a2) => { act(); return null; }, null, null).HasValue;
        }

        public bool Execute<T1>(Action<T1> act, T1 arg1)
        {
            return Execute<T1, object, object>((a1, a2) => { act(a1); return null; }, arg1, null).HasValue;
        }

        public bool Execute<T1, T2>(Action<T1, T2> act, T1 arg1, T2 arg2)
        {
            return Execute<T1, T2, object>((a1, a2) => { act(a1, a2); return null; }, arg1, arg2).HasValue;
        }

        public Option<TResult> Execute<TResult>(Func<TResult> func)
        {
            return Execute<object, object, TResult>((a1, a2) => func(), null, null);
        }

        public Option<TResult> Execute<T1, TResult>(Func<T1, TResult> func, T1 arg1)
        {
            return Execute<T1, object, TResult>((a1, a2) => func(a1), arg1, null);
        }

        public Option<TResult> Execute<T1, T2, TResult>(Func<T1, T2, TResult> func, T1 arg1, T2 arg2)
        {
            // short-cut
            if (_disabled)
                return Option.None<TResult>();

            Interlocked.Increment(ref _runningExecution);
            try
            {
                if (!_disabled)
                    return func(arg1, arg2).Some();
                else
                    return Option.None<TResult>();
            }
            finally
            {
                if (Interlocked.Decrement(ref _runningExecution) == 0)
                {
                    if (_disabled)
                    {
                        lock (_lock)
                        {
                            Monitor.PulseAll(_lock);
                        }
                    }
                }
            }
        }

        public void DisableAndWait()
        {
            DisableAndWait(TimeSpan.FromMilliseconds(-1));
        }

        public bool DisableAndWait(TimeSpan timeout)
        {
            _disabled = true;
            if (_runningExecution == 0)
                return true;

            lock (_lock)
            {
                while (_runningExecution > 0)
                {
                    if (!Monitor.Wait(_lock, timeout))
                        return false;
                }
            }

            return true;
        }
    }
}
