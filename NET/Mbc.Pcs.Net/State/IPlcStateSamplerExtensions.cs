//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.AsyncUtils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mbc.Pcs.Net.State
{
    public static class IPlcStateSamplerExtensions
    {
        /// <summary>
        /// Wartet asynchron auf einen bestimmten Zustand eines <see cref="IPlcStateSampler{TState}"/>
        /// </summary>
        /// <typeparam name="TState">The State class</typeparam>
        /// <param name="plcState">represent the registred state listener</param>
        /// <param name="stateCondition">the condition to wait for</param>
        /// <param name="timeout">timeout time for stop waiting</param>
        /// <param name="cancellationToken">posibility to stop waiting</param>
        /// <returns>the PLC Timestamp the condition has reached</returns>
        public static async Task<DateTime> WaitForStateAsync<TState>(this IPlcStateSampler<TState> plcState, Func<TState, bool> stateCondition, TimeSpan timeout, CancellationToken cancellationToken)
            where TState : IPlcState
        {
            // shortcut
            var sample = plcState.CurrentSample;
            if (stateCondition(sample))
                return sample.PlcTimeStamp;

            var taskSource = new TaskCompletionSource<DateTime>(TaskCreationOptions.RunContinuationsAsynchronously);

            void StateChangedHandler(object s, PlcMultiStateChangedEventArgs<TState> e)
            {
                try
                {
                    foreach (var status in e.States)
                    {
                        if (stateCondition(status))
                        {
                            taskSource.TrySetResult(status.PlcTimeStamp);
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    taskSource.TrySetCanceled();
                }
                catch (Exception ex)
                {
                    taskSource.TrySetException(ex);
                }
            }

            plcState.StatesChanged += StateChangedHandler;
            try
            {
                var stateTimestamp = await taskSource.Task.TimeoutAfter(timeout, cancellationToken);
                return stateTimestamp;
            }
            finally
            {
                plcState.StatesChanged -= StateChangedHandler;
            }
        }

        /// <summary>
        /// Stellt sicher, dass eine Bedingung während der vorgegebenen Zeit anliegt.
        /// </summary>
        /// <typeparam name="TState">Der Typ des Status.</typeparam>
        /// <param name="plcState">Eine Instanz auf einen State-Sampler.</param>
        /// <param name="ensureCondition">Die Bedingung, die eingehalten werden muss.</param>
        /// <param name="ensureTime">Die Zeit, in welcher die Bedingung eingehalten werden muss.</param>
        /// <param name="cancellationToken">Ein Abbruch-Token.</param>
        /// <returns><c>true</c> wenn die Bedingung während der ganzen Zeit eingehalten wurde, sonst
        /// <c>false</c>.</returns>
        public static async Task<bool> EnsureStateAsync<TState>(this IPlcStateSampler<TState> plcState, Func<TState, bool> ensureCondition, TimeSpan ensureTime, CancellationToken cancellationToken)
            where TState : IPlcState
        {
            var handler = new EnsureStateHandler<TState>(ensureCondition, ensureTime);
            plcState.StatesChanged += handler.StateChangedHandler;
            try
            {
                return await handler.RunAsync(cancellationToken);
            }
            finally
            {
                plcState.StatesChanged -= handler.StateChangedHandler;
            }
        }

        private class EnsureStateHandler<TState>
            where TState : IPlcState
        {
            private readonly TaskCompletionSource<bool> _taskSource = new TaskCompletionSource<bool>();
            private readonly Func<TState, bool> _ensureCondition;
            private readonly TimeSpan _time;
            private bool _first = true;
            private DateTime _startTime;

            public EnsureStateHandler(Func<TState, bool> ensureCondition, TimeSpan time)
            {
                _ensureCondition = ensureCondition;
                _time = time;
            }

            public async Task<bool> RunAsync(CancellationToken cancellationToken)
            {
                if (!_taskSource.Task.IsCompleted)
                {
                    using (var cancelRegistration = cancellationToken.Register(() => _taskSource.TrySetCanceled()))
                    {
                        return await _taskSource.Task;
                    }
                }

                return await _taskSource.Task;
            }

            public void StateChangedHandler(object s, PlcMultiStateChangedEventArgs<TState> e)
            {
                foreach (var state in e.States)
                {
                    if (_taskSource.Task.IsCompleted)
                        return;

                    if (!_ensureCondition(state))
                    {
                        // nicht erfolgreich
                        _taskSource.TrySetResult(false);
                        return;
                    }

                    if (_first)
                    {
                        _startTime = state.PlcTimeStamp;
                        _first = false;
                    }
                    else
                    {
                        if ((state.PlcTimeStamp - _startTime) >= _time)
                        {
                            // erfolgreich
                            _taskSource.TrySetResult(true);
                            return;
                        }
                    }
                }
            }
        }
    }
}
