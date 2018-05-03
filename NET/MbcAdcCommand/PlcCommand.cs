using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;
using TwinCAT.Ads.SumCommand;
using System.Collections.ObjectModel;

namespace MbcAdcCommand
{
    /// <summary>
    /// Kapselt ein PLC-Kommando.
    /// </summary>
    public class PlcCommand
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Occurs when the state of a command is changed.
        /// </summary>
        public event EventHandler<PlcCommandEventArgs> StateChanged;

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
        public void Execute(ICommandInput input = null, ICommandOutput output = null)
        {
            if (!_adsClient.IsConnected)
                throw new InvalidOperationException("ADS-Client is not connect.");

            if (input != null)
                WriteInputData(input);

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

                if (output != null)
                    ReadOutputData(output);
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

        private void ReadOutputData(ICommandOutput output)
        {
            // TODO nur temporär -> muss überarbeitet werden siehe Gitlab Issue #3

            InitFbSymbols();

            // ToList => deterministische Reihenfolge notwendig
            var outputNames = output.GetOutputNames().ToList();

            var symbols = new List<string>();
            var types = new List<Type>();
            foreach (var name in outputNames)
            {
                var symbolInfo = _fbSymbols[name];
                symbols.Add(symbolInfo.Item1);
                types.Add(symbolInfo.Item2);
            }

            var handleCreator = new SumCreateHandles(_adsClient, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumReader = new SumHandleRead(_adsClient, handles, types.ToArray());
                var values = sumReader.Read();

                for (int i = 0; i < values.Length; i++)
                {
                    output.SetOutputData(outputNames[i], values[i]);
                }
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(_adsClient, handles);
                handleReleaser.ReleaseHandles();
            }

        }

        private void WriteInputData(ICommandInput input)
        {
            // TODO nur temporär -> muss überarbeitet werden siehe Gitlab Issue #3

            InitFbSymbols();

            var inputData = input.GetInputData();

            var symbols = new List<string>();
            var types = new List<Type>();
            var values = new List<object>();
            foreach (var name in inputData.Keys)
            {
                var symbolInfo = _fbSymbols[name];
                symbols.Add(symbolInfo.Item1);
                types.Add(symbolInfo.Item2);
                values.Add(Convert.ChangeType(inputData[name], symbolInfo.Item2));
            }

            var handleCreator = new SumCreateHandles(_adsClient, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumWriter = new SumHandleWrite(_adsClient, handles, types.ToArray());
                sumWriter.Write(values.ToArray());
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(_adsClient, handles);
                handleReleaser.ReleaseHandles();
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

                    StateChanged?.Invoke(this, new PlcCommandEventArgs(handshakeData.Progress, handshakeData.SubTask, 
                        handshakeData.IsCommandFinished, handshakeData.IsCommandCancelled));

                    if (handshakeData.IsCommandFinished)
                    {
                        CheckResultCode(handshakeData.ResultCode);
                        break;
                    }
                }
                catch (TimeoutException)
                {
                    throw new PlcCommandTimeoutException(_adsCommandFb, $"The command timed out after {Timeout.Seconds} [s].");
                }
            }
        }

        private IReadOnlyDictionary<string, Tuple<string, Type>> _fbSymbols;

        private void InitFbSymbols()
        {
            if (_fbSymbols != null)
                return;

            var fbSymbolNames = ((ITcAdsSymbol5)_adsClient.ReadSymbolInfo(_adsCommandFb))
                .DataType.SubItems
                .Where(item => item.BaseType.IsPrimitive)
                .ToDictionary(
                    item => item.SubItemName, 
                    item => Tuple.Create(_adsCommandFb + "." + item.SubItemName, item.BaseType.ManagedType));

            _fbSymbols = new ReadOnlyDictionary<string, Tuple<string, Type>>(fbSymbolNames);
        }

        private void CheckResultCode(ushort resultCode)
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

            public bool IsCommandCancelled => !Execute && Busy;

            public override string ToString() 
                => $"Execute={Execute} Busy={Busy} ResultCode={ResultCode} Progress={Progress} SubTask={SubTask}";
        }
    }
}
