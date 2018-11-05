using EnsureThat;
using Mbc.Ads.Mapper;
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
        private IAdsSymbolInfo _adsSymbolInfo;
        private int _statusNotificationHandle;
        private AdsMapper<TStatus> _adsMapper;

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

        public (DateTime TimeStamp, TStatus State) CurrentSample { get; private set; } = (DateTime.FromFileTime(0), new TStatus());

        public void StartSampling()
        {
            if (SamplingActive)
            {
                StopSampling();
            }

            _adsConnectionService.Connection.AdsNotification += OnAdsNotification;

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

            _adsConnectionService.Connection.AdsNotification -= OnAdsNotification;

            SamplingActive = false;

            _logger.Info("Deregistered Device Notification for '{variable_path}'.", _config.VariablePath);
        }

        protected virtual void ReadAdsSymbolInfo(IAdsConnection connection)
        {
            _adsSymbolInfo = AdsSymbolReader.Read(connection, _config.VariablePath);
        }

        private void OnAdsNotification(object sender, AdsNotificationEventArgs e)
        {
            if (e.UserData != this)
                return;

            try
            {
                TStatus status = _adsMapper.MapStream(e.DataStream);
                CurrentSample = (DateTime.FromFileTime(e.TimeStamp), status);

                StateChanged?.Invoke(this, new PlcStateChangedEventArgs<TStatus>(status, DateTime.FromFileTime(e.TimeStamp)));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in notification event handler.");
            }
        }
    }
}
