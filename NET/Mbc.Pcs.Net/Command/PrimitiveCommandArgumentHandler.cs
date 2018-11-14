using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;
using TwinCAT.TypeSystem;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// A simple implementation of <see cref="CommandArgumentHandler"/> which
    /// marshalls only primitive types but needs no further configuration.
    /// </summary>
    public class PrimitiveCommandArgumentHandler : CommandArgumentHandler
    {
        public override void ReadOutputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandOutput output)
        {
            // read symbols with attribute flags for output data
            IReadOnlyDictionary<string, (string variablePath, Type type, int byteSize)> fbSymbols = ReadFbSymbols(adsConnection, adsCommandFbPath, PlcAttributeNames.PlcCommandOutput);

            // ToList => deterministische Reihenfolge notwendig
            List<string> outputNames = output.GetOutputNames().ToList();

            var missingOutputVariables = outputNames.Where(x => !fbSymbols.ContainsKey(x)).ToArray();
            if (missingOutputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath,
                    string.Format(CommandResources.ERR_OutputVariablesMissing, string.Join(",", missingOutputVariables)));
            }

            var symbols = new List<string>();
            var types = new List<Type>();
            foreach (var name in outputNames)
            {
                var symbolInfo = fbSymbols[name];
                symbols.Add(symbolInfo.variablePath);
                types.Add(symbolInfo.type);
            }

            var handleCreator = new SumCreateHandles(adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumReader = new SumHandleRead(adsConnection, handles, types.ToArray());
                var values = sumReader.Read();

                for (int i = 0; i < values.Length; i++)
                {
                    output.SetOutputData(outputNames[i], values[i]);
                }
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(adsConnection, handles);
                handleReleaser.ReleaseHandles();
            }
        }

        public override void WriteInputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandInput input)
        {
            // read symbols with attribute flags for input data
            IReadOnlyDictionary<string, (string variablePath, Type type, int byteSize)> fbSymbols = ReadFbSymbols(adsConnection, adsCommandFbPath, PlcAttributeNames.PlcCommandInput);

            IDictionary<string, object> inputData = input.GetInputData();

            var missingInputVariables = inputData.Keys.Where(x => !fbSymbols.ContainsKey(x)).ToArray();
            if (missingInputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath,
                    string.Format(CommandResources.ERR_InputVariablesMissing, string.Join(",", missingInputVariables)));
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

            var handleCreator = new SumCreateHandles(adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumWriter = new SumHandleWrite(adsConnection, handles, types.ToArray());
                sumWriter.Write(values.ToArray());
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(adsConnection, handles);
                handleReleaser.ReleaseHandles();
            }
        }

        /// <summary>
        /// Reads all symbols in the same hierarchy as the function block they are flaged with the Attribute 
        /// named in <para>attributeName</para>.
        /// </summary>
        /// <param name="attributeName">The attribute flag to filter (Case Insensitive)</param>
        private static IReadOnlyDictionary<string, (string variablePath, Type type, int byteSize)> ReadFbSymbols(IAdsConnection adsConnection, string adsCommandFbPath, string attributeName)
        {
            ITcAdsSymbol commandSymbol = adsConnection.ReadSymbolInfo(adsCommandFbPath);
            if (commandSymbol == null)
            {
                // command Symbol not found
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_CommandNotFound, adsCommandFbPath));
            }

            var validTypeCategories = new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String };

            var fbSymbolNames = ((ITcAdsSymbol5)commandSymbol)
                .DataType.SubItems
                .Where(item =>
                    validTypeCategories.Contains(item.BaseType.Category)
                    && item.Attributes.Any(a => string.Equals(a.Name, attributeName, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(
                    item => item.SubItemName,
                    item => (variablePath: adsCommandFbPath + "." + item.SubItemName, type: GetManagedTypeForSubItem(item), byteSize: item.ByteSize));

            return new ReadOnlyDictionary<string, (string variablePath, Type type, int byteSize)>(fbSymbolNames);
        }

        private static Type GetManagedTypeForSubItem(ITcAdsSubItem subitem)
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
                    throw new InvalidOperationException(string.Format(CommandResources.ERR_UnknownAdsType, subitem.BaseType));
            }
        }
    }
}
