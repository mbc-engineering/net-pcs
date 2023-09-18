//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using Mbc.Pcs.Net.Connection;
using System;
using System.Threading;

namespace Mbc.Pcs.Net.State
{
    /// <summary>
    /// Provides a Heart beat event and also a died event
    /// based on the recived <see cref="IPlcStateSampler{TState}.StatesChanged"/> events
    /// The heart can only beat if the <see cref="IPlcStateSampler{TState}"/> sends new states,
    /// this is happen when the <see cref="IPlcAdsConnectionService.IsConnected"/> is true
    /// otherwise the there are no heart beats.
    /// the event <see cref="HeartDied"/> fires after missing states after <see cref="TimeUntilDie"/>
    /// </summary>
    /// <typeparam name="TState">Object of state</typeparam>
    public class PlcStateHeartBeatGenerator<TState> : IHeartBeat, IDisposable
        where TState : IPlcState
    {
        private readonly IPlcAdsConnectionService _adsConnection;
        private readonly IPlcStateSampler<TState> _plcStateSampler;
        private bool _awakening;
        private Timer _lostHearBeatTimer;
        private DateTime _lastHeartBeat;
        private DateTime _lastSampleTimestamp;
        private TimeSpan _timeUntilDie;
        private TimeSpan _heartBeatIntervall;

        /// <summary>
        /// Will be called every <see cref="HeartBeatInterval"/> if the connection is still alive.
        /// </summary>
        public event EventHandler<HeartBeatEventArgs> HeartBeats;

        /// <summary>
        /// Will be called <see cref="TimeUntilDie"/> after the last sample if the connection died.
        /// </summary>
        public event EventHandler<HeartBeatDiedEventArgs> HeartDied;

        public PlcStateHeartBeatGenerator(TimeSpan beatInterval, IPlcAdsConnectionService adsConnection, IPlcStateSampler<TState> plcStateSampler)
        {
            _adsConnection = Ensure.Any.IsNotNull(adsConnection, nameof(adsConnection));
            _plcStateSampler = Ensure.Any.IsNotNull(plcStateSampler, nameof(plcStateSampler));
            HeartBeatInterval = beatInterval;

            // set default timeout of factor 2
            TimeUntilDie = TimeSpan.FromMilliseconds(HeartBeatInterval.TotalMilliseconds * 2);

            _adsConnection.ConnectionStateChanged += OnAdsConnectionOnConnectionStateChanged;
        }

        public void Dispose()
        {
            _adsConnection.ConnectionStateChanged -= OnAdsConnectionOnConnectionStateChanged;
            StopStateObservation();
        }

        public TimeSpan HeartBeatInterval
        {
            get => _heartBeatIntervall;
            set
            {
                double minimalValue = 10.0;
                if (_plcStateSampler.SampleRate > 0U)
                {
                    minimalValue = 1000 / _plcStateSampler.SampleRate;
                }

                if (value < TimeSpan.FromMilliseconds(minimalValue))
                {
                    throw new ArgumentOutOfRangeException(nameof(HeartBeatInterval), value, $"The Value should be greater then the coresponding source SampleRate of {_plcStateSampler.SampleRate} Hz");
                }

                _heartBeatIntervall = value;
            }
        }

        public TimeSpan TimeUntilDie
        {
            get => _timeUntilDie;
            set
            {
                _timeUntilDie = value;

                // set new time-out time
                _lostHearBeatTimer?.Change(TimeUntilDie, Timeout.InfiniteTimeSpan);
            }
        }

        public DateTime LastHeartBeat
        {
            get => _lastHeartBeat;
            private set
            {
                _lastHeartBeat = value;
            }
        }

        protected virtual void NotifyHeartBeats(DateTime beatTime)
        {
            HeartBeats.Invoke(this, new HeartBeatEventArgs(beatTime));
        }

        protected virtual void NotifyHeartDied()
        {
            var args = new HeartBeatDiedEventArgs
            {
                LastHeartBeat = LastHeartBeat,
                DiedTime = DateTime.Now,
                LastSampleTime = _lastSampleTimestamp < DateTime.FromFileTime(0) ? SampleTime.FromRawValue(0) : new SampleTime(_lastSampleTimestamp, _plcStateSampler.SampleRate),
            };
            HeartDied?.Invoke(this, args);
        }

        private void OnAdsConnectionOnConnectionStateChanged(object sender, PlcConnectionChangeArgs e)
        {
            if (e.Connected)
            {
                StartStateObservation();
            }
            else
            {
                StopStateObservation();

                // If not conneced no beat possible
                NotifyHeartDied();
            }
        }

        private void StartStateObservation()
        {
            _awakening = true;
            _lostHearBeatTimer = new Timer(_ => NotifyHeartDied(), null, TimeUntilDie, Timeout.InfiniteTimeSpan);

            _plcStateSampler.StatesChanged += OnPlcStatesChanged;
        }

        private void StopStateObservation()
        {
            _awakening = false;
            _lostHearBeatTimer?.Dispose();
            _lostHearBeatTimer = null;

            _plcStateSampler.StatesChanged -= OnPlcStatesChanged;
        }

        private void OnPlcStatesChanged(object sender, PlcMultiStateChangedEventArgs<TState> e)
        {
            if (_awakening)
            {
                // awaken
                LastHeartBeat = e.State.PlcTimeStamp;
                NotifyHeartBeats(LastHeartBeat);
                _awakening = false;
            }

            _lastSampleTimestamp = e.State.PlcTimeStamp;

            // Reset time-out
            _lostHearBeatTimer.Change(TimeUntilDie, Timeout.InfiniteTimeSpan);

            // invervall has pass
            if (e.State.PlcTimeStamp >= LastHeartBeat.Add(HeartBeatInterval))
            {
                LastHeartBeat = e.State.PlcTimeStamp;
                NotifyHeartBeats(LastHeartBeat);
            }
        }
    }
}
