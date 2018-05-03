using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TwinCAT.Ads;

namespace MbcAdcCommand
{
    /// <summary>
    /// Kapselt ein PLC-Kommando.
    /// </summary>
    public class PlcCommand
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        private readonly TcAdsClient _adsClient;
        private readonly string _adsCommandFb;

        public PlcCommand(TcAdsClient adsClient, string adsCommandFb)
        {
            // needs not be connected yet
            _adsClient = adsClient;
            _adsCommandFb = adsCommandFb;
        }

        /// <summary>
        /// Maximale time to wait for command completion.
        /// </summary>
        public TimeSpan Timeout { get; set; } = DefaultTimeout;

        /// <summary>
        /// Executes a PLC command.
        /// </summary>
        /// <exception cref="InvalidOperationException">The ADS-Client given at construction
        /// time is not connected.</exception>
        public void Execute(ICommandInput input = null)
        {
            if (!_adsClient.IsConnected)
                throw new InvalidOperationException("ADS-Client is not connect.");

            _adsClient.AdsNotificationEx += OnAdsNotification;
            try
            {
                SetExecuteFlag();

                var handshakeExchange = new DataExchange<CommandHandshakeStruct>();
                var cmdHandle = _adsClient.AddDeviceNotificationEx(
                    $"{_adsCommandFb}.stHandshake", 
                    AdsTransMode.OnChange,
                    TimeSpan.FromMilliseconds(50), // 50 statt 0 als Workaround für ein hängiges ADS-Problem mit Initial-Events
                    TimeSpan.Zero,
                    Tuple.Create(this, handshakeExchange), 
                    typeof(CommandHandshakeStruct));
                try
                {
                    WaitForExecution(handshakeExchange);
                }
                finally
                {
                    _adsClient.DeleteDeviceNotification(cmdHandle);
                }

            }
            catch (Exception ex)
            {
                // Bei Fehlern zur Sicherheit die Ausführung abbrechen
                try
                {
                    ResetExecuteFlag();
                }
                catch (Exception resetEx)
                {
                    ex.Data.Add("ResetExecuteFlagException", resetEx);
                }
                throw ex;
            }
            finally
            {
                _adsClient.AdsNotificationEx -= OnAdsNotification;
            }

        }

        private void WaitForExecution(DataExchange<CommandHandshakeStruct> dataExchange)
        {
            var timeoutStopWatch = Stopwatch.StartNew();
            while (true)
            {
                try
                {
                    var remainingTimeout = Timeout - timeoutStopWatch.Elapsed;
                    var handshakeData = dataExchange.GetOrWait(remainingTimeout);

                    if (handshakeData.IsCommandFinished)
                    {
                        CheckResultCode(handshakeData.ResultCode);
                        break;
                    }
                }
                catch (TimeoutException e)
                {
                    throw new PlcCommandTimeoutException(_adsCommandFb, $"The command timed out after {Timeout.Seconds} [s].");
                }
            }
        }

        void CheckResultCode(ushort resultCode)
        {
            string errorMsg = string.Empty;

            switch (resultCode)
            {
                case 0: // Init
                case 1: // Running
                case 2: // Done
                    return;

                case 3: // Cancelled
                    errorMsg = "The command was cancelled.";
                    break;

                default:
                    errorMsg = "The command execution failed.";
                    break;
            }

            throw new PlcCommandErrorException(_adsCommandFb, resultCode, errorMsg);
        }

        private void OnAdsNotification(object sender, AdsNotificationExEventArgs e)
        {
            var userDataTuple = e.UserData as Tuple<PlcCommand, DataExchange<CommandHandshakeStruct>>;
            if (userDataTuple == null || userDataTuple.Item1 != this) return;

            userDataTuple.Item2.Set((CommandHandshakeStruct) e.Value);
        }

        private void SetExecuteFlag()
        {
            WriteVariable(_adsCommandFb + ".stHandshake.bExecute", true);
        }

        private void ResetExecuteFlag()
        {
            WriteVariable(_adsCommandFb + ".stHandshake.bExecute", false);
        }

        public void WriteVariable(string symbolName, object value)
        {
            var varHandle = _adsClient.CreateVariableHandle(symbolName);
            try
            {
                // ToDo: Fix possible mismatch of datatype!!!
                _adsClient.WriteAny(varHandle, value);
            }
            finally
            {
                _adsClient.DeleteVariableHandle(varHandle);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        struct CommandHandshakeStruct
        {
            [MarshalAs(UnmanagedType.I1)]
            public bool Execute;
            [MarshalAs(UnmanagedType.I1)]
            public bool Busy;
            public ushort ResultCode;
            public byte Progress;
            public ushort SubTask;

            public bool IsCommandFinished => !Execute && !Busy;

            public override string ToString() 
                => $"Execute={Execute} Busy={Busy} ResultCode={ResultCode} Progress={Progress} SubTask={SubTask}";
        }
    }
}
