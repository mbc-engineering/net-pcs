//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using Mbc.Ads.Utils.SumCommand;
using System.Collections.Generic;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// A  implementation of <see cref="CommandArgumentHandler"/> which
    /// writes and reads <see cref="AdsStream"/> for arguments.
    /// </summary>

    public class AdsStreamCommandArgumentHandler : CommandArgumentHandler
    {
        public override void ReadOutputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandOutput output)
        {
            // ToList => deterministische Reihenfolge notwendig
            List<string> outputNames = output.GetOutputNames().ToList();

            // read symbols with attribute flags for output data
            IDictionary<string, ITcAdsSubItem> items = ReadFbSymbols(adsConnection, adsCommandFbPath, PlcAttributeNames.PlcCommandOutput);

            var missingOutputVariables = outputNames.Where(x => !items.ContainsKey(x)).ToArray();
            if (missingOutputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath,
                    string.Format(CommandResources.ERR_OutputVariablesMissing, string.Join(",", missingOutputVariables)));
            }

            var symbols = new List<string>();
            var symbolSizes = new List<int>();
            foreach (var name in outputNames)
            {
                var item = items[name];
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
                output.SetOutputData(outputNames[i], readData[i]);
            }
        }

        public override void WriteInputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandInput input)
        {
            IDictionary<string, object> inputData = input.GetInputData();
            Ensure.That(inputData.Values.All(x => x is AdsStream), nameof(input), (opt) => opt.WithMessage("Must contain only AdsStream instances."));

            // read symbols with attribute flags for input data
            IDictionary<string, ITcAdsSubItem> items = ReadFbSymbols(adsConnection, adsCommandFbPath, PlcAttributeNames.PlcCommandInput);

            var missingInputVariables = inputData.Keys.Where(x => !items.ContainsKey(x)).ToArray();
            if (missingInputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath,
                    string.Format(CommandResources.ERR_InputVariablesMissing, string.Join(",", missingInputVariables)));
            }

            var symbols = new List<string>();
            var streams = new List<AdsStream>();
            foreach (var name in inputData.Keys)
            {
                var item = items[name];
                symbols.Add(adsCommandFbPath + "." + item.SubItemName);
                streams.Add((AdsStream)inputData[name]);
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
