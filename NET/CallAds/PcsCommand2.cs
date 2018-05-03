using CallAds;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TwinCAT.Ads;

namespace AtomizerUI.link
{
    // ToDo: Add Progress
    public class PcsCommand2<Tout> where Tout : new()
    {
        private const int DEFAULT_TIME_OUT_SECOND = 8;
        private readonly TcAdsClient _adsClient;
        /// <summary>
        /// Path to the PLC Command variable
        /// </summary>
        private readonly string _adsCommandVar;
        private IDictionary<string, ITcAdsDataType> _commandSymbols;

        public PcsCommand2(TcAdsClient adsClient, string adsCommandVar)
        {
            if (!adsClient.IsConnected)
            {
                throw new AdsException($"The ADS client use in the {nameof(PcsCommand2<Tout>)} is not connected.");
            }

            _adsClient = adsClient;                        
            _adsCommandVar = adsCommandVar;

            Timeout = TimeSpan.FromSeconds(DEFAULT_TIME_OUT_SECOND);
        }

        public PcsCommand2(TcAdsClient adsClient, string commandVar, TimeSpan timeout) : this(adsClient, commandVar)
        {
            Timeout = timeout;
        }

        /// <summary>
        /// Maximale time to wait for command completion
        /// </summary>
        public TimeSpan Timeout { get; set; }

        //public Task<object> ExecuteAsync<Tin>(Tin argsIn, CancellationToken cancelToken)
        //{
        //    return Task.Run<object>(() => {
        //        var argsInHndl = _adsClient.CreateVariableHandle(_commandVar + ".stArgsIn");
        //        try
        //        {
        //            _adsClient.WriteAny(argsInHndl, argsIn);
        //        }
        //        finally
        //        {
        //            _adsClient.DeleteVariableHandle(argsInHndl);
        //        }

        //        return Execute(cancelToken);
        //    });
        //}

        public Tout Execute<Tin>(Tin argsIn)
        {
            ReadSymbols();

            // Write all input param
            var symbolsToWrite = new PcsSymbolCollection();
            foreach (var prop in typeof(Tin).GetProperties())
            {
                if (_commandSymbols.ContainsKey($"{_adsCommandVar}.{prop.Name}"))
                {
                    string symbolFullPath = $"{_adsCommandVar}.{prop.Name}";
                    symbolsToWrite.Add(
                        new PcsSymbol()
                        {
                            FullPath = symbolFullPath,
                            Value = prop.GetValue(argsIn),
                            TcAdsDataType = _commandSymbols[symbolFullPath]
                        });
                }      
            }

            _adsClient.WriteSumVariables(symbolsToWrite);

            _adsClient.WriteObjectVariables(argsIn, _adsCommandVar, _commandSymbols);

            return Execute();
        }      

        public Tout Execute()
        {
            return Execute(CancellationToken.None);
        }

        public Tout Execute(CancellationToken cancelToken)
        {
            _adsClient.AdsNotificationEx += OnAdsnotificationEx;
            try
            {
                SetExecuteFlag();
                                
                var dataExch = new DataExchange();
                var cmdHandle = _adsClient.AddDeviceNotificationEx($"{_adsCommandVar}.stHandshake", AdsTransMode.OnChange,
                    TimeSpan.Zero, TimeSpan.Zero, dataExch, typeof(CommandControlData));

                cancelToken.Register(ResetExecuteFlag);
                try
                {
                    var watch = Stopwatch.StartNew();
                    while (true)
                    {
                        try
                        {
                            var controlData =
                                (CommandControlData)
                                    dataExch.GetOrWait(Timeout - TimeSpan.FromMilliseconds(watch.ElapsedMilliseconds));

                            if (!controlData.Execute && !controlData.Busy)
                            {
                                CheckResultCode(controlData.ResultCode);
                                break;
                            }
                        }
                        catch (TimeoutException)
                        {
                            // ToDo: Use Symbolic and SumreadCommand
                            // Workaround für ein ADS-Problem, falls kein Event empfangen wurde
                            var cmdHndl = _adsClient.CreateVariableHandle(_adsCommandVar + ".stHandshake");
                            try
                            {
                                var ccd = (CommandControlData) _adsClient.ReadAny(cmdHndl, typeof (CommandControlData));
                                if (!ccd.Execute && !ccd.Busy)
                                {
                                    break;
                                }
                            }
                            finally
                            {
                                _adsClient.DeleteVariableHandle(cmdHndl);
                            }                                    

                            throw new PcsCommandTimeoutException();
                        }
                    }
                }
                finally
                {
                    _adsClient.DeleteDeviceNotification(cmdHandle);
                }
            } 
            finally
            {
                _adsClient.AdsNotificationEx -= OnAdsnotificationEx;
            }

            // Read Result values
            return ReadResult();            
        }

        private void ReadSymbols()
        {
            var symbols = new Dictionary<string, ITcAdsDataType>();

            var symbolInfo = _adsClient.ReadSymbolInfo(_adsCommandVar);
            if (symbolInfo is ITcAdsSymbol5 symbol5Info)
            {
                symbols.AddSymbolsFlatedRecursive(symbol5Info.DataType, _adsCommandVar);

                _commandSymbols = symbols;
            }
        }

        private Tout ReadResult() 
        {
            // Read all output values
            var test = new Tout();
            Tout result = _adsClient.ReadObjectVariables(new Tout(), _adsCommandVar, _commandSymbols);

            return result;            
        }

        private void SetExecuteFlag()
        {
            _adsClient.WriteVariable(_adsCommandVar + ".stHandshake.bExecute", true);            
        }

        private void ResetExecuteFlag()
        {
            _adsClient.WriteVariable(_adsCommandVar + ".stHandshake.bExecute", false);
        }

        void OnAdsnotificationEx(object sender, AdsNotificationExEventArgs e)
        {
            if (e.UserData is DataExchange)
            {
                ((DataExchange) e.UserData).Set(e.Value);
            }
        }

        void CheckResultCode(ushort resultCode)
        {
            string errorMsg = string.Empty;

            switch (resultCode)
            {
                case 0:
                    // Init state
                    return;                    
                case 1:
                    errorMsg = "Running";
                    break;
                case 2:
                    // Done
                    return;
                case 3:
                    errorMsg = "Cancelled";
                    break;
                default:
                    errorMsg = $"Unknown error: {resultCode}";
                    break;
            }

            throw new PcsCommandErrorException(resultCode, errorMsg);
        }
    }
}
