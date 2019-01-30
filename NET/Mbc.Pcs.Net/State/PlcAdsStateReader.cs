//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using Mbc.Ads.Mapper;
using Mbc.AsyncUtils;
using Mbc.Pcs.Net.Connection;
using NLog;
using System;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.State
{
    public class PlcAdsStateReader<TStatus> : IPlcStateSampler<TStatus>
        where TStatus : new()
    {
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private readonly IPlcAdsConnectionService _adsConnectionService;
        private readonly PlcAdsStateReaderConfig<TStatus> _config;
        private AsyncSerializedTaskExecutor _notificationExecutor;
        private IAdsSymbolInfo _adsSymbolInfo;
        private int _statusNotificationHandle;
        private AdsMapper<TStatus> _adsMapper;
        private bool _queueOverflowLogging;
        private int _maxOverflow;

        public event EventHandler<PlcStateChangedEventArgs<TStatus>> StateChanged;

        public PlcAdsStateReader(IPlcAdsConnectionService adsConnectionService, PlcAdsStateReaderConfig<TStatus> config)
        {
            // Momentane Einschränkungen der Cycle-Time: Ganzzahlig darstellbar und max. 1000 Hz
            EnsureArg.IsInRange(config.CycleTime, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1000), nameof(config), opts => opts.WithMessage("Cycle time must be in range 1s to 1ms(1Hz-1000Hz)."));
            EnsureArg.Is(Math.IEEERemainder(1000, config.CycleTime.TotalMilliseconds), 0, nameof(config), opts => opts.WithMessage("Cycle time must be a divisible integer."));
            Ensure.Any.IsNotNull(adsConnectionService, nameof(adsConnectionService));

            _adsConnectionService = adsConnectionService;
            _config = config;
        }

        public uint SampleRate => (uint)(1000 / _config.CycleTime.TotalMilliseconds);

        public bool SamplingActive { get; private set; }

        public TStatus CurrentSample { get; private set; } = new TStatus();

        public void StartSampling()
        {
            if (SamplingActive)
            {
                StopSampling();
            }

            _notificationExecutor = new AsyncSerializedTaskExecutor();
            _notificationExecutor.UnhandledException = (s, e) =>
            {
                _logger.Error(e, "Error in notification event handler.");
            };

            _adsConnectionService.Connection.AdsNotification += OnAdsNotification;
            _adsConnectionService.Connection.AdsNotificationError += OnAdsNotificationError;

            ReadAdsSymbolInfo(_adsConnectionService.Connection);

            _adsMapper = _config.AdsMapperConfiguration.CreateAdsMapper(_adsSymbolInfo);

            _statusNotificationHandle = _adsConnectionService.Connection.AddDeviceNotification(
                _config.VariablePath,
                new AdsStream(_adsSymbolInfo.SymbolsSize),
                AdsTransMode.Cyclic,
                (int)_config.CycleTime.TotalMilliseconds,  // if TimeSpan.Zero sampled by the shortest PLC Task
                (int)_config.MaxDelay.TotalMilliseconds,
                this);

            SamplingActive = true;

            _logger.Info("Registered Device Notification for '{variable_path}'.", _config.VariablePath);
        }

        public void StopSampling()
        {
            if (_adsConnectionService.IsConnected)
            {
                _adsConnectionService.Connection.DeleteDeviceNotification(_statusNotificationHandle);
            }

            _adsConnectionService.Connection.AdsNotificationError -= OnAdsNotificationError;
            _adsConnectionService.Connection.AdsNotification -= OnAdsNotification;

            if (_notificationExecutor != null)
            {
                _notificationExecutor.WaitForExecution();
                _notificationExecutor.Dispose();
                _notificationExecutor = null;
            }

            SamplingActive = false;

            _logger.Info("Deregistered Device Notification for '{variable_path}'.", _config.VariablePath);
        }

        protected virtual void ReadAdsSymbolInfo(IAdsConnection connection)
        {
            _adsSymbolInfo = AdsSymbolReader.Read(connection, _config.VariablePath);
        }

        private void OnAdsNotificationError(object sender, AdsNotificationErrorEventArgs e)
        {
            _logger.Error(e.Exception, "ADS Notification error.");
        }

        protected virtual void OnAdsNotification(object sender, AdsNotificationEventArgs e)
        {
            if (e.UserData != this)
                return;

            LogNotificationQueueOverflow();

            try
            {
                TStatus status = _adsMapper.MapStream(e.DataStream);
                var timestamp = DateTime.FromFileTime(e.TimeStamp);

                CurrentSample = status;

                _notificationExecutor.ExecuteAsync(() => OnStateChanged(status, timestamp));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in notification event handler.");
            }
        }

        protected virtual void OnStateChanged(TStatus status, DateTime dateTime)
        {
            StateChanged?.Invoke(this, new PlcStateChangedEventArgs<TStatus>(status, dateTime));
        }

        private void LogNotificationQueueOverflow()
        {
            _maxOverflow = Math.Max(_notificationExecutor.ExecutionQueueLength, _maxOverflow);
            if (_notificationExecutor.ExecutionQueueLength > (_config.MaxDelay.TotalMilliseconds / _config.CycleTime.TotalMilliseconds * 2) && !_queueOverflowLogging)
            {
                _logger.Trace("ADS Notification queue started to overflow.");
                _queueOverflowLogging = true;
            }
            else if (_notificationExecutor.ExecutionQueueLength == 0 && _queueOverflowLogging)
            {
                _logger.Warn("Ads Notification queue overflow max {maxOverflow}.", _maxOverflow);
                _queueOverflowLogging = false;
                _maxOverflow = 0;
            }
        }
    }
}
