//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
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
            IDictionary<string, ITcAdsSubItem> fbItems = ReadFbSymbols(adsConnection, adsCommandFbPath, new string[] { PlcAttributeNames.PlcCommandOutput, PlcAttributeNames.PlcCommandOutputOptional });
            IDictionary<string, ITcAdsSubItem> requiredfbItems = fbItems
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
            foreach (var name in outputNames)
            {
                var item = fbItems[name];
                symbols.Add(adsCommandFbPath + "." + item.SubItemName);
                symbolSizes.Add(item.ByteSize);
            }

            IList<AdsStream> readData;
            var handleCreator = new SumCreateHandles(adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumReader = new SumHandleReadStream(adsConnection, handles, symbolSizes.ToArray());
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
                var name = outputNames[i];
                var item = fbItems[name];
                // TODO Marker nur 2. Wahl, siehe MR !11 und Issue #28
                if (output.GetOutputData<object>(name) == ReadAsPrimitiveMarker)
                {
                    PrimitiveTypeConverter.Default.Unmarshal(item.BaseType, readData[i].ToArray(), 0, out object value);
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
            IDictionary<string, ITcAdsSubItem> fbItems = ReadFbSymbols(adsConnection, adsCommandFbPath, new string[] { PlcAttributeNames.PlcCommandInput, PlcAttributeNames.PlcCommandInputOptional });
            IDictionary<string, ITcAdsSubItem> requiredfbItems = fbItems
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

            var symbols = new List<string>();
            var streams = new List<AdsStream>();
            // Based on fbItems, write the values from  ICommandInput data to fb
            foreach (var fbItem in fbItems)
            {
                var item = fbItem.Value;
                symbols.Add(adsCommandFbPath + "." + item.SubItemName);

                if (inputData.TryGetValue(fbItem.Key, out object value))
                {
                    if (value is AdsStream adsStream)
                    {
                        streams.Add(adsStream);
                    }
                    else
                    {
                        Ensure.Bool.IsTrue(PrimitiveTypeConverter.CanMarshal(item.DataTypeId), nameof(input), (opt) => opt.WithMessage($"Input '{fbItem.Key}' of data type '{item.DataTypeId}' cannot be marshalled."));

                        PrimitiveTypeConverter.Marshal(item.DataTypeId, value, out byte[] data);
                        streams.Add(new AdsStream(data));
                    }
                }
                else
                {
                    // Set it to default Value
                    Ensure.Bool.IsTrue(PrimitiveTypeConverter.CanMarshal(item.DataTypeId), nameof(input), (opt) => opt.WithMessage($"Input '{fbItem.Key}' of data type '{item.DataTypeId}' cannot be marshalled."));

                    var type = GetManagedTypeForSubItem(item);
                    PrimitiveTypeConverter.Marshal(item.DataTypeId, type.GetDefaultValue(), out byte[] data);
                    streams.Add(new AdsStream(data));
                }
            }

            var handleCreator = new SumCreateHandles(adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumWriter = new SumHandleWriteStream(adsConnection, handles);
                sumWriter.Write(streams);
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(adsConnection, handles);
                handleReleaser.ReleaseHandles();
            }
        }
    }
}
