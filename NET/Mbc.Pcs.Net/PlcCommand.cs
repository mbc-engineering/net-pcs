using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;
using System.Collections.ObjectModel;
using System.Threading;
using TwinCAT.TypeSystem;
using System.Threading.Tasks;

namespace Mbc.Pcs.Net
{
    /// <summary>
    /// Kapselt ein PLC-Kommando.
    /// </summary>
    public class PlcCommand : IPlcCommand
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Occurs when the state of a command is changed.
        /// </summary>
        public event EventHandler<PlcCommandEventArgs> StateChanged;

        private readonly IAdsConnection _adsConnection;
        private readonly string _adsCommandFbPath;

        public PlcCommand(IAdsConnection adsConnection, string adsCommandFbPath)
        {
            // needs not be connected yet
            _adsConnection = adsConnection;
            _adsCommandFbPath = adsCommandFbPath;
        }

        /// <summary>
        /// Maximale time to wait for command completion.
        /// </summary>
        public TimeSpan Timeout { get; set; } = DefaultTimeout;

        /// <summary>
        /// The PLC Variable 
        /// </summary>
        public string AdsCommandFbPath => _adsCommandFbPath;

        /// <summary>
        /// Executes a PLC command.
        /// </summary>
        /// <seealso cref="Execute(CancellationToken, ICommandInput, ICommandOutput)"/>
        public void Execute(ICommandInput input = null, ICommandOutput output = null)
        {
            Execute(CancellationToken.None, input, output);
        }

        /// <summary>
        /// Executes a PLC command asynchronously.
        /// </summary>
        /// <seealso cref="Execute(CancellationToken, ICommandInput, ICommandOutput)"/>
        public Task ExecuteAsync(ICommandInput input = null, ICommandOutput output = null)
        {
            return Task.Run(() => Execute(CancellationToken.None, input, output));
        }

        /// <summary>
        /// Executes a PLC command asynchronously.
        /// </summary>
        /// <seealso cref="Execute(CancellationToken, ICommandInput, ICommandOutput)"/>
        public Task ExecuteAsync(CancellationToken cancellationToken, ICommandInput input = null, ICommandOutput output = null)
        {
            return Task.Run(() => Execute(cancellationToken, input, output));
        }

        /// <summary>
        /// Executes a PLC command.
        /// </summary>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> which allows to
        /// cancel the running command. The cancel request is sent to the PLC and the 
        /// execution will still wait for the PLC to end to command.</param>
        /// <exception cref="InvalidOperationException">The ADS-Client given at construction
        /// time is not connected.</exception>
        /// <example
        protected void Execute(CancellationToken cancellationToken, ICommandInput input = null, ICommandOutput output = null)
        {
            using (var commandLock = PlcCommandLock.AcquireLock($"{_adsCommandFbPath}-on-{_adsConnection.Address.ToString()}"))
            {
                if (!_adsConnection.IsConnected)
                    throw new InvalidOperationException("ADS-Client is not connect.");

                if (input != null)
                    WriteInputData(input);

                _adsConnection.AdsNotificationEx += OnAdsNotification;
                try
                {
                    SetExecuteFlag();

                    using (var cancellationRegistration = cancellationToken.Register(ResetExecuteFlag))
                    {
                        var handshakeExchange = new DataExchange<CommandHandshakeStruct>();
                        var cmdHandle = _adsConnection.AddDeviceNotificationEx(
                            $"{_adsCommandFbPath}.stHandshake",
                            AdsTransMode.OnChange,
                            50, // 50 statt 0 als Workaround für ein hängiges ADS-Problem mit Initial-Events
                            0,
                            Tuple.Create(this, handshakeExchange),
                            typeof(CommandHandshakeStruct));
                        try
                        {
                            WaitForExecution(handshakeExchange);
                        }
                        catch (PlcCommandErrorException ex) when (ex.ResultCode == 3 && cancellationToken.IsCancellationRequested)
                        {
                            // Update state
                            StateChanged?.Invoke(this, new PlcCommandEventArgs(handshakeExchange.Data.Progress, handshakeExchange.Data.SubTask, false, true));

                            // Im Falle eines Abbruch durch einen User-Request (CancellationToken) wird
                            // die Framework-Exception zurückgegeben.
                            throw new OperationCanceledException("The command was cancelled by user request.", ex, cancellationToken);
                        }
                        finally
                        {
                            _adsConnection.DeleteDeviceNotification(cmdHandle);
                        }
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
                    _adsConnection.AdsNotificationEx -= OnAdsNotification;
                }
            }
        }

        private void ReadOutputData(ICommandOutput output)
        {
            // read symbols with attribute flags for output data
            var fbSymbols = ReadFbSymbols(PlcAttributeNames.PlcCommandOutput);

            // ToList => deterministische Reihenfolge notwendig
            var outputNames = output.GetOutputNames().ToList();

            var missingOutputVariables = outputNames.Where(x => !fbSymbols.ContainsKey(x)).ToArray();
            if (missingOutputVariables.Length > 0)
            {
                throw new PlcCommandException(_adsCommandFbPath,
                    $"Missing output variables on the PLC implementation: '{string.Join(",", missingOutputVariables)}'");
            }

            var symbols = new List<string>();
            var types = new List<Type>();
            foreach (var name in outputNames)
            {
                var symbolInfo = fbSymbols[name];
                symbols.Add(symbolInfo.variablePath);
                types.Add(symbolInfo.type);
            }

            var handleCreator = new SumCreateHandles(_adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumReader = new SumHandleRead(_adsConnection, handles, types.ToArray());
                var values = sumReader.Read();

                for (int i = 0; i < values.Length; i++)
                {
                    output.SetOutputData(outputNames[i], values[i]);
                }
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(_adsConnection, handles);
                handleReleaser.ReleaseHandles();
            }

        }

        private void WriteInputData(ICommandInput input)
        {
            // read symbols with attribute flags for input data
            var fbSymbols = ReadFbSymbols(PlcAttributeNames.PlcCommandInput);
            
            var inputData = input.GetInputData();

            var missingInputVariables = inputData.Keys.Where(x => !fbSymbols.ContainsKey(x)).ToArray();
            if (missingInputVariables.Length > 0)
            {
                throw new PlcCommandException(_adsCommandFbPath,
                    $"Missing input variables on the PLC implementation: '{string.Join(",", missingInputVariables)}'");
            }

            var symbols = new List<string>();
            var types = new List<Type>();
            var values = new List<object>();
            foreach (var name in inputData.Keys)
            {
                var symbolInfo = fbSymbols[name];
                symbols.Add(symbolInfo.variablePath);
                types.Add(symbolInfo.type);
                values.Add(Convert.ChangeType(inputData[name], symbolInfo.type));
            }

            var handleCreator = new SumCreateHandles(_adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumWriter = new SumHandleWrite(_adsConnection, handles, types.ToArray());
                sumWriter.Write(values.ToArray());
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(_adsConnection, handles);
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
                catch (TimeoutException ex)
                {
                    throw new PlcCommandTimeoutException(_adsCommandFbPath, $"The command timed out after {Timeout.Seconds} [s].", ex);
                }
            }
        }

        /// <summary>
        /// Reads all symbols in the same hierarchy as the function block they are flaged with the Attribute named in <para>attributeName</para>
        /// </summary>
        /// <param name="attributeName">The attribute flag to filter (Case Insensitive)</param>
        /// <returns></returns>
        private IReadOnlyDictionary<string, (string variablePath, Type type, int byteSize)> ReadFbSymbols(string attributeName)
        {
            var fbSymbolNames = ((ITcAdsSymbol5)_adsConnection.ReadSymbolInfo(_adsCommandFbPath))
                .DataType.SubItems
                .Where(item => 
                    new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String }.Contains(item.BaseType.Category)
                    && item.Attributes.Any(a => string.Equals(a.Name, attributeName, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(
                    item => item.SubItemName, 
                    item => (variablePath: _adsCommandFbPath + "." + item.SubItemName, type: GetManagedTypeForSubItem(item), byteSize: item.ByteSize));

            return new ReadOnlyDictionary<string, (string variablePath, Type type, int byteSize)>(fbSymbolNames);
        }

        private Type GetManagedTypeForSubItem(ITcAdsSubItem subitem)
        {
            if (subitem.BaseType.ManagedType != null)
                return subitem.BaseType.ManagedType;

            switch (subitem.BaseType.DataTypeId)
            {
                case AdsDatatypeId.ADST_INT8:
                    return typeof(sbyte);
                case AdsDatatypeId.ADST_INT16:
                    return typeof(short);
                case AdsDatatypeId.ADST_INT32:
                    return typeof(int);
                case AdsDatatypeId.ADST_UINT8:
                    return typeof(byte);
                case AdsDatatypeId.ADST_UINT16:
                    return typeof(ushort);
                case AdsDatatypeId.ADST_UINT32:
                    return typeof(uint);
                default:
                    throw new InvalidOperationException($"Unhandled ADS-Datatype '{subitem.BaseType}'. Please extend implementation!");
            }
        }

        private void CheckResultCode(ushort resultCode)
        {
            string errorMsg = string.Empty;

            switch (resultCode)
            {
                case (ushort)CommandResultCode.Initialized:
                case (ushort)CommandResultCode.Running:
                case (ushort)CommandResultCode.Done:
                    return;

                case (ushort)CommandResultCode.Cancelled:
                    errorMsg = "The command was cancelled.";
                    break;

                default:
                    errorMsg = "The command execution failed.";
                    break;
            }

            throw new PlcCommandErrorException(_adsCommandFbPath, resultCode, errorMsg);
        }

        private void OnAdsNotification(object sender, AdsNotificationExEventArgs e)
        {
            var userDataTuple = e.UserData as Tuple<PlcCommand, DataExchange<CommandHandshakeStruct>>;
            if (userDataTuple == null || userDataTuple.Item1 != this) return;

            userDataTuple.Item2.Set((CommandHandshakeStruct) e.Value);
        }

        private void SetExecuteFlag()
        {
            WriteVariable(_adsCommandFbPath + ".stHandshake.bExecute", true);
        }

        private void ResetExecuteFlag()
        {
            WriteVariable(_adsCommandFbPath + ".stHandshake.bExecute", false);
        }

        protected void WriteVariable(string symbolName, object value)
        {
            var varHandle = _adsConnection.CreateVariableHandle(symbolName);
            try
            {
                // ToDo: Fix possible mismatch of datatype!!!
                _adsConnection.WriteAny(varHandle, value);
            }
            finally
            {
                _adsConnection.DeleteVariableHandle(varHandle);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        internal struct CommandHandshakeStruct
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
