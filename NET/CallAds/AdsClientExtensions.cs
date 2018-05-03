using System;
using System.Collections.Generic;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;

namespace CallAds
{
    // ToDo: desicion: Generic type musst be new interface???
    public static class AdsClientExtensions
    {
        public static T ReadObjectVariables<T>(this TcAdsClient adsClient, T subject, string adsCommandVariablePrefix, IDictionary<string, ITcAdsDataType> plcSymbols)
        {
            PcsSymbolCollection pcsSymbols = subject.GetSymbols(adsCommandVariablePrefix, plcSymbols);

            pcsSymbols = adsClient.ReadSumVariables(pcsSymbols);

            subject.SetSymbols(pcsSymbols);

            return subject;
        }

        public static PcsSymbolCollection ReadSumVariables(this TcAdsClient adsClient, PcsSymbolCollection symbolsToRead)
        {
            if (symbolsToRead == null)
            {
                throw new ArgumentNullException(nameof(symbolsToRead));
            }

            SumCreateHandles createHandlesCommand = new SumCreateHandles(adsClient, symbolsToRead.Select(s => s.FullPath).ToArray());
            uint[] handles = createHandlesCommand.CreateHandles();

            var readCommand = new SumHandleRead(adsClient, handles, symbolsToRead.Select(s => s.ManagedType).ToArray());

            object[] result = readCommand.Read();

            SumReleaseHandles releaseHandlesCommand = new SumReleaseHandles(adsClient, handles);
            releaseHandlesCommand.ReleaseHandles();

            int resultIdx = 0;
            foreach (var item in symbolsToRead)
            {
                item.Value = result[resultIdx];
                resultIdx += 1;
            }

            return symbolsToRead;
        }

        /// <summary>
        /// Write all Properties of a object <paramref name="subject"/> if the symbol exists in the list <paramref name="plcSymbols"/>
        /// </summary>
        /// <typeparam name="T">Type of Object to write</typeparam>
        /// <param name="adsClient">To connect with plc</param>
        /// <param name="subject">The Object to write</param>
        /// <param name="adsCommandVariablePrefix">prefix of the plc symbol to combine with <paramref name="subject"/></param>
        /// <param name="plcSymbols">existing plcSymbols</param>
        public static void WriteObjectVariables<T>(this TcAdsClient adsClient, T subject, string adsCommandVariablePrefix, IDictionary<string, ITcAdsDataType> plcSymbols)
        {
            PcsSymbolCollection pcsSymbols = subject.GetSymbols(adsCommandVariablePrefix, plcSymbols);

            adsClient.WriteSumVariables(pcsSymbols);
        }

        public static void WriteSumVariables(this TcAdsClient adsClient, PcsSymbolCollection symbolsToWrite)
        {
            if (symbolsToWrite == null)
            {
                throw new ArgumentNullException(nameof(symbolsToWrite));
            }

            SumCreateHandles createHandlesCommand = new SumCreateHandles(adsClient, symbolsToWrite.Select(s => s.FullPath).ToArray());
            uint[] handles = createHandlesCommand.CreateHandles();

            var writeCommand = new SumHandleWrite(adsClient, handles, symbolsToWrite.Select(s => s.ManagedType).ToArray());

            writeCommand.Write(symbolsToWrite.Select(s => s.Value).ToArray());

            SumReleaseHandles releaseHandlesCommand = new SumReleaseHandles(adsClient, handles);
            releaseHandlesCommand.ReleaseHandles();
        }

        public static void WriteVariable(this TcAdsClient adsClient, string symbolName, object value)
        {
            var argsInHndl = adsClient.CreateVariableHandle(symbolName);
            try
            {
                // ToDo: Fix possible mismatch of datatype!!!
                adsClient.WriteAny(argsInHndl, value);
            }
            finally
            {
                adsClient.DeleteVariableHandle(argsInHndl);
            }
        }
    }
}
