//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using Mbc.Pcs.Net.Connection;
using NLog;
using System;
using System.Threading;

namespace Mbc.Pcs.Net.State
{
    /// <summary>
    /// Provides a Heart beat event and also a died event
    /// based on the recived <see cref="IPlcStateSampler{TState}.StateChanged"/> events
    /// The heart can only beat if the <see cref="IPlcStateSampler{TState}"/> sends new states, 
    /// this is happen when the <see cref="IPlcAdsConnectionService.IsConnected"/> is true
    /// otherwise the there are no heart beats.
    /// the event <see cref="HeartDied"/> fires after the first <see cref="HeartBeats"/> has fired
    /// </summary>
    /// <typeparam name="TState">Object of state</typeparam>
    public class PlcStateHeartBeatGenerator<TState> : IHeartBeat, IDisposable
        where TState : IPlcState
    {
        public static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IPlcAdsConnectionService _adsConnection;
        private readonly IPlcStateSampler<TState> _plcStateSampler;
        private bool _awakening;
        private Timer _lostHearBeatTimer;
        private DateTime _lastHeartBeat;
        private TimeSpan _timeUntilDie;
        private TimeSpan _heartBeatIntervall;

        public event EventHandler<HeartBeatEventArgs> HeartBeats;

        public event EventHandler<HeartBeatDiedEventArgs> HeartDied;

        public PlcStateHeartBeatGenerator(TimeSpan beatInterval, IPlcAdsConnectionService adsConnection, IPlcStateSampler<TState> plcStateSampler)
        {
            _adsConnection = Ensure.Any.IsNotNull(adsConnection, nameof(adsConnection));
            _plcStateSampler = Ensure.Any.IsNotNull(plcStateSampler, nameof(plcStateSampler));
            HeartBeatInterval = beatInterval;

            // set default timeout
            TimeUntilDie = TimeSpan.FromMilliseconds(HeartBeatInterval.TotalMilliseconds * 1.5);

            _adsConnection.ConnectionStateChanged += AdsConnectionOnConnectionStateChanged;
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

                // Reset time-out
                _lostHearBeatTimer.Change(TimeUntilDie, Timeout.InfiniteTimeSpan);
            }
        }

        public DateTime StartTime { get; set; }

        public void Dispose()
        {
            _adsConnection.ConnectionStateChanged -= AdsConnectionOnConnectionStateChanged;
        }

        protected virtual void OnHeartBeats(DateTime beatTime)
        {
            LastHeartBeat = beatTime;

            HeartBeats.Invoke(this, new HeartBeatEventArgs(beatTime));
        }

        protected virtual void OnHeartDied(DateTime diedTime)
        {
            HeartDied?.Invoke(this, new HeartBeatDiedEventArgs() { LastHeartBeat = LastHeartBeat, DiedTime = diedTime });
        }

        private void AdsConnectionOnConnectionStateChanged(object sender, PlcConnectionChangeArgs e)
        {
            if (e.Connected)
            {
                StartStateObservation();
            }
            else
            {
                StopStateObservation();
            }
        }

        private void StateSamplerOnStateChanged(object sender, PlcMultiStateChangedEventArgs<TState> e)
        {
            if (_awakening)
            {
                StartTime = e.State.PlcTimeStamp;
                // awaken
                OnHeartBeats(e.State.PlcTimeStamp);
                _awakening = false;
            }

            // invervall has pass
            if (e.State.PlcTimeStamp >= StartTime.Add(HeartBeatInterval))
            {
                StartTime = e.State.PlcTimeStamp;

                OnHeartBeats(e.State.PlcTimeStamp);
            }
        }

        private void LostHearBeatTimerOnElapsed(object state)
        {
            if (state is PlcStateHeartBeatGenerator<TState> g)
            {
                OnHeartDied(g.LastHeartBeat.Add(g.TimeUntilDie));
            }
        }

        private void StartStateObservation()
        {
            _awakening = true;
            _lostHearBeatTimer = new Timer(LostHearBeatTimerOnElapsed, this, TimeUntilDie, Timeout.InfiniteTimeSpan);

            _plcStateSampler.StatesChanged += StateSamplerOnStateChanged;
        }

        private void StopStateObservation()
        {
            _awakening = false;
            _lostHearBeatTimer?.Dispose();
            _lostHearBeatTimer = null;

            _plcStateSampler.StatesChanged -= StateSamplerOnStateChanged;
        }
    }
}
