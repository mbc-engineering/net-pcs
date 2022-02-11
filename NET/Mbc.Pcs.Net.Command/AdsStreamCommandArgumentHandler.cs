//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils.SumCommand;
using System;
using System.Collections.Generic;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;
using TwinCAT.TypeSystem;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// A  implementation of <see cref="CommandArgumentHandler"/> which
    /// writes and reads <see cref="AdsStream"/> for arguments. This implementation
    /// also allows primimtive type mapping for reading and writing.
    /// </summary>
    public class AdsStreamCommandArgumentHandler : CommandArgumentHandler
    {
        public static readonly object ReadAsPrimitiveMarker = new object();

        public override void ReadOutputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandOutput output)
        {
            // read symbols with attribute flags for output data
            IDictionary<string, IMember> fbItems = ReadFbSymbols(adsConnection, adsCommandFbPath, new string[] { PlcAttributeNames.PlcCommandOutput, PlcAttributeNames.PlcCommandOutputOptional });
            IDictionary<string, IMember> requiredfbItems = fbItems
                .Where(x => x.Value.Attributes.Any(a => string.Equals(a.Name, PlcAttributeNames.PlcCommandOutput, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(x => x.Key, x => x.Value);

            // ToList => deterministische Reihenfolge notwendig
            List<string> outputNames = output.GetOutputNames().ToList();

            var missingOutputVariables = outputNames.Where(x => !fbItems.ContainsKey(x)).ToArray();
            if (missingOutputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_OutputVariablesMissing, string.Join(",", missingOutputVariables)));
            }

            var missingRequiredOutputVariables = requiredfbItems.Where(x => !outputNames.Contains(x.Key)).ToArray();
            if (missingOutputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_RequiredOutputVariablesMissing, string.Join(",", missingOutputVariables)));
            }

            var symbols = new List<string>();
            var symbolSizes = new List<int>();
            foreach (string name in outputNames)
            {
                IMember item = fbItems[name];
                symbols.Add(adsCommandFbPath + "." + item.InstanceName);
                symbolSizes.Add(item.ByteSize);
            }

            IList<ReadOnlyMemory<byte>> readData;
            var handleCreator = new SumCreateHandles(adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumReader = new SumHandleReadData(adsConnection, handles, symbolSizes.ToArray());
                readData = sumReader.Read();
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(adsConnection, handles);
                handleReleaser.ReleaseHandles();
            }

            // Daten auf Output-Objekt übertragen
            for (var i = 0; i < outputNames.Count; i++)
            {
                string name = outputNames[i];
                IMember item = fbItems[name];
                // TODO Marker nur 2. Wahl, siehe MR !11 und Issue #28
                if (output.GetOutputData<object>(name) == ReadAsPrimitiveMarker)
                {
                    PrimitiveTypeMarshaler.Default.Unmarshal(item.DataType, readData[i].Span, (Type)null, out object value);
                    output.SetOutputData(name, value);
                }
                else
                {
                    output.SetOutputData(name, readData[i]);
                }
            }
        }

        public override void WriteInputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandInput input)
        {
            IDictionary<string, object> inputData = input.GetInputData();

            // read symbols with attribute flags for input data
            IDictionary<string, IMember> fbItems = ReadFbSymbols(adsConnection, adsCommandFbPath, new string[] { PlcAttributeNames.PlcCommandInput, PlcAttributeNames.PlcCommandInputOptional });
            IDictionary<string, IMember> requiredfbItems = fbItems
                .Where(x => x.Value.Attributes.Any(a => string.Equals(a.Name, PlcAttributeNames.PlcCommandInput, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(x => x.Key, x => x.Value);

            // Existing CommandInput must be exist on fb
            var missingInputVariables = inputData.Keys.Where(x => !fbItems.ContainsKey(x)).ToArray();
            if (missingInputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_InputVariablesMissing, string.Join(",", missingInputVariables)));
            }

            // fb required flaged arguments must be in the CommandInput
            var missingReqInputVariables = requiredfbItems.Where(x => !inputData.ContainsKey(x.Key)).ToArray();
            if (missingReqInputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_RequiredInputVariablesMissing, string.Join(",", missingReqInputVariables)));
            }

            var writeDataSize = fbItems.Values.Select(x => x.ByteSize).Sum();
            var writeData = new byte[writeDataSize];
            var symbols = new List<string>();
            int offset = 0;

            // Based on fbItems, write the values from  ICommandInput data to fb
            foreach (KeyValuePair<string, IMember> fbItem in fbItems)
            {
                var item = fbItem.Value;
                symbols.Add(adsCommandFbPath + "." + item.InstanceName);

                if (inputData.TryGetValue(fbItem.Key, out object value))
                {
                    if (value is AdsStream adsStream)
                    {
                        adsStream.ToArray().CopyTo(new Span<byte>(writeData, offset, item.ByteSize));
                    }
                    if (value is ReadOnlyMemory<byte> data)
                    {
                        data.Span.CopyTo(new Span<byte>(writeData, offset, item.ByteSize));
                    }
                    else
                    {
                        PrimitiveTypeMarshaler.Default.Marshal(item.DataType, item.ValueEncoding, value, new Span<byte>(writeData, offset, item.ByteSize));
                    }
                }
                else
                {
                    // Set it to default Value
                    var type = GetManagedTypeForSubItem(item.DataType);

                    PrimitiveTypeMarshaler.Default.Marshal(item.DataType, item.ValueEncoding, type.GetDefaultValue(), new Span<byte>(writeData, offset, item.ByteSize));
                }

                offset += item.ByteSize;
            }

            var handleCreator = new SumCreateHandles(adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumWriter = new SumHandleWriteData(adsConnection, handles);
                sumWriter.Write(writeData, fbItems.Values.Select(x => x.ByteSize));
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(adsConnection, handles);
                handleReleaser.ReleaseHandles();
            }
        }
    }
}
