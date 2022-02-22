using Mbc.Ads.Mapper;
using Mbc.Pcs.Net.Connection;
using Mbc.Pcs.Net.State;
using NLog;
using System;

namespace AdsMapperCli
{
    public static class Program
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private static PlcAdsConnectionService adsConnectionService;
        private static PlcAdsStateReader<DestinationDataObject> plcAdsTestPlaceStatus;

        public static void Main(string[] args)
        {
            SetupNLog();
            try
            {
                string amsnetid = "204.35.225.246.1.1";
                Logger.Info("Setup & Connect to TwinCat on {0}", amsnetid);
                adsConnectionService = new PlcAdsConnectionService(amsnetid, 851);
                adsConnectionService.ConnectionStateChanged += OnConnectionStateChanged;

                var testPlaceStatusConfig = new PlcAdsStateReaderConfig<DestinationDataObject>
                {
                    VariablePath = $"GVL.stTest",
                    AdsMapperConfiguration = new AdsMapperConfiguration<DestinationDataObject>(
                        cfg => cfg.ForAllSourceMember(opt => opt.RemovePrefix("f", "n", "b", "a", "e", "t", "d", "dt", "s", "ws"))),
                    CycleTime = TimeSpan.FromMilliseconds(2),
                    MaxDelay = TimeSpan.FromMilliseconds(500),
                };

                // Setup state Reader
                plcAdsTestPlaceStatus = new PlcAdsStateReader<DestinationDataObject>(adsConnectionService, testPlaceStatusConfig);
                plcAdsTestPlaceStatus.StatesChanged += OnPlcStatesChange;

                // RocknRoll
                adsConnectionService.Start();

                // Wait for termination
                var keepRunning = true;
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    keepRunning = false;
                };

                while (keepRunning)
                {
                    // endless
                }

                Logger.Info("stopping output");
                plcAdsTestPlaceStatus.StopSampling();
            }
            catch (Exception ex)
            {
                Logger.Info(ex, "houston we have a problem: {0}", ex.Message);
            }
            finally
            {
                plcAdsTestPlaceStatus?.Dispose();
                adsConnectionService?.Dispose();
            }
        }

        private static void SetupNLog()
        {
            var config = new NLog.Config.LoggingConfiguration();
            // Targets where to log to: Console
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            // Rules for mapping loggers to targets
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);
            // Apply config
            NLog.LogManager.Configuration = config;
        }

        private static void OnConnectionStateChanged(object sender, PlcConnectionChangeArgs e)
        {
            if (e.Connected)
            {
                Logger.Info("Connected to TwinCat");
                plcAdsTestPlaceStatus.StartSampling();
            }
            else
            {
                Console.WriteLine("Disconnected to TwinCat");
                plcAdsTestPlaceStatus.StopSampling();
            }
        }

        private static void OnPlcStatesChange(object source, PlcMultiStateChangedEventArgs<DestinationDataObject> testPlaceStatusEvent)
        {
            var config = new System.Text.Json.JsonSerializerOptions() { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            config.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            Logger.Info("New State");
            Logger.Info(System.Text.Json.JsonSerializer.Serialize(testPlaceStatusEvent.State, config));
        }
    }
}
