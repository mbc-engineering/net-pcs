//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using Mbc.Ads.Mapper;
using Mbc.AsyncUtils;
using Mbc.Pcs.Net.Connection;
using NLog;
using System;
using System.Collections.Generic;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.State
{
    public class PlcAdsStateReader<TStatus> : IPlcStateSampler<TStatus>, IDisposable
        where TStatus : IPlcState, new()
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly IPlcAdsConnectionService _adsConnectionService;
        private readonly PlcAdsStateReaderConfig<TStatus> _config;
        private readonly int _notificationBlockSize;
        private List<TStatus> _notificationBlockBuffer;
        private AsyncSerializedTaskExecutor _notificationExecutor;
        private IAdsSymbolInfo _adsSymbolInfo;
        private uint _statusNotificationHandle;
        private AdsMapper<TStatus> _adsMapper;
        private bool _queueOverflowLogging;
        private int _maxOverflow;
        private SampleTime _lastSampleTime;
        private bool _firstNotificationSample = true;

        public event EventHandler<PlcStateChangedEventArgs<TStatus>> StateChanged;

        public event EventHandler<PlcMultiStateChangedEventArgs<TStatus>> StatesChanged;

        public PlcAdsStateReader(IPlcAdsConnectionService adsConnectionService, PlcAdsStateReaderConfig<TStatus> config)
        {
            // Momentane Einschränkungen der Cycle-Time: Ganzzahlig darstellbar und max. 1000 Hz
            EnsureArg.IsInRange(config.CycleTime, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1000), nameof(config), opts => opts.WithMessage("Cycle time must be in range 1s to 1ms(1Hz-1000Hz)."));
            EnsureArg.Is(Math.IEEERemainder(1000, config.CycleTime.TotalMilliseconds), 0, nameof(config), opts => opts.WithMessage("Cycle time must be a divisible integer."));
            EnsureArg.IsGte(config.MaxDelay, config.CycleTime, nameof(config), opts => opts.WithMessage("MaxDelay must be greater/equal to CycleTime"));
            EnsureArg.Is(Math.IEEERemainder(config.MaxDelay.TotalMilliseconds, config.CycleTime.TotalMilliseconds), 0, nameof(config), opts => opts.WithMessage("MayDelay must be integer divisible with CycleTime"));
            Ensure.Any.IsNotNull(adsConnectionService, nameof(adsConnectionService));

            _adsConnectionService = adsConnectionService;
            _config = config;
            _notificationBlockSize = (int)(config.MaxDelay.TotalMilliseconds / config.CycleTime.TotalMilliseconds);
            _notificationBlockBuffer = new List<TStatus>(_notificationBlockSize);
        }

        public uint SampleRate => (uint)(1000 / _config.CycleTime.TotalMilliseconds);

        public bool SamplingActive { get; private set; }

        public TStatus CurrentSample { get; private set; } = new TStatus();

        public void Dispose()
        {
            _notificationExecutor?.Dispose();
        }

        public void StartSampling()
        {
            if (SamplingActive)
            {
                StopSampling();
            }

            _notificationExecutor = new AsyncSerializedTaskExecutor();
            _notificationExecutor.UnhandledException = (s, e) =>
            {
                Logger.Error(e, "Error in notification event handler.");
            };

            _notificationBlockBuffer.Clear();

            // TODO use _adsConnectionService.Connection.AdsSumNotification
            _adsConnectionService.Connection.AdsNotification += OnAdsNotification;
            _adsConnectionService.Connection.AdsNotificationError += OnAdsNotificationError;

            ReadAdsSymbolInfo(_adsConnectionService.Connection);

            _adsMapper = _config.AdsMapperConfiguration.CreateAdsMapper(_adsSymbolInfo);

            // TODO maybe use CycleInContext with ContextMask
            _statusNotificationHandle = _adsConnectionService.Connection.AddDeviceNotification(
                _config.VariablePath,
                _adsSymbolInfo.Symbol.ByteSize,
                new NotificationSettings(AdsTransMode.Cyclic, (int)_config.CycleTime.TotalMilliseconds, (int)_config.MaxDelay.TotalMilliseconds),
                this);

            SamplingActive = true;

            Logger.Info("Registered Device Notification for '{variable_path}'.", _config.VariablePath);
        }

        public void StopSampling()
        {
            if (_adsConnectionService.IsConnected)
            {
                _adsConnectionService.Connection.DeleteDeviceNotification(_statusNotificationHandle);
            }

            _adsConnectionService.Connection.AdsNotificationError -= OnAdsNotificationError;
            _adsConnectionService.Connection.AdsNotification -= OnAdsNotification;

            _firstNotificationSample = true;

            if (_notificationExecutor != null)
            {
                _notificationExecutor.WaitForExecution();
                _notificationExecutor.Dispose();
                _notificationExecutor = null;
            }

            SamplingActive = false;

            Logger.Info("Deregistered Device Notification for '{variable_path}'.", _config.VariablePath);
        }

        protected virtual void ReadAdsSymbolInfo(IAdsConnection connection)
        {
            _adsSymbolInfo = AdsSymbolReader.Read(connection, _config.VariablePath);
        }

        private void OnAdsNotificationError(object sender, AdsNotificationErrorEventArgs e)
        {
            Logger.Error(e.Exception, "ADS Notification error.");

            // Error: Neues Sample erstellen mit letzten Wert
            CurrentSample.PlcDataQuality = PlcDataQuality.Lost;
            _notificationBlockBuffer.Add(CurrentSample);

            // Event auf jedenfall auslösen
            OnStateChanged(CurrentSample);
            OnStatesChanged(_notificationBlockBuffer);
            _notificationBlockBuffer = new List<TStatus>(_notificationBlockSize);
        }

        protected virtual void OnAdsNotification(object sender, AdsNotificationEventArgs e)
        {
            if (e.UserData != this)
                return;

            LogNotificationQueueOverflow();

            try
            {
                ReadOnlyMemory<byte> data = e.Data;
                TStatus status = _adsMapper.MapData(data.Span);
                DateTimeOffset timestamp = e.TimeStamp;
                status.PlcTimeStamp = timestamp.UtcDateTime;
                var currentSampleTime = new SampleTime(status.PlcTimeStamp, SampleRate);

                // Bei bestimmten Situationen können verluste an Samples auftreten (z.B. Breakpoints) -> wird hier geloggt
                if (_firstNotificationSample)
                {
                    _firstNotificationSample = false;

                    // First notification can not be checked
                    status.PlcDataQuality = PlcDataQuality.Good;
                }
                else
                {
                    bool missingSample = currentSampleTime - _lastSampleTime > 1;

                    status.PlcDataQuality = missingSample ? PlcDataQuality.Skipped : PlcDataQuality.Good;

                    if (missingSample)
                    {
                        Logger.Warn("Missing sample between last sample on {lastTimeStamp} and new sample at {currentTimeStamp}", _lastSampleTime.ToString(), currentSampleTime.ToString());
                    }
                }

                _lastSampleTime = currentSampleTime;

                CurrentSample = status;

                _notificationBlockBuffer.Add(status);

                OnStateChanged(status);

                if (_notificationBlockBuffer.Count == _notificationBlockSize)
                {
                    OnStatesChanged(_notificationBlockBuffer);
                    _notificationBlockBuffer = new List<TStatus>(_notificationBlockSize);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error in notification event handler.");
            }
        }

        protected virtual void OnStateChanged(TStatus status)
        {
            var args = new PlcStateChangedEventArgs<TStatus>(status);
            _notificationExecutor.ExecuteAsync(() => StateChanged?.Invoke(this, args));
        }

        protected virtual void OnStatesChanged(List<TStatus> statusBlockBuffer)
        {
            var args = new PlcMultiStateChangedEventArgs<TStatus>(statusBlockBuffer);
            _notificationExecutor.ExecuteAsync(() => StatesChanged?.Invoke(this, args));
        }

        private void LogNotificationQueueOverflow()
        {
            _maxOverflow = Math.Max(_notificationExecutor.ExecutionQueueLength, _maxOverflow);
            if (_notificationExecutor.ExecutionQueueLength > (_config.MaxDelay.TotalMilliseconds / _config.CycleTime.TotalMilliseconds * 2) && !_queueOverflowLogging)
            {
                Logger.Trace("ADS Notification queue started to overflow.");
                _queueOverflowLogging = true;
            }
            else if (_notificationExecutor.ExecutionQueueLength == 0 && _queueOverflowLogging)
            {
                Logger.Warn("Ads Notification queue overflow max {maxOverflow}.", _maxOverflow);
                _queueOverflowLogging = false;
                _maxOverflow = 0;
            }
        }
    }
}
