using EnsureThat;
using Mbc.Ads.Utils.SumCommand;
using System;
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
            throw new NotImplementedException();
        }

        public override void WriteInputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandInput input)
        {
            IDictionary<string, object> inputData = input.GetInputData();
            Ensure.That(inputData.Values.All(x => x is AdsStream), nameof(input), (opt) => opt.WithMessage("Must contain only AdsStream instances."));

            // read symbols with attribute flags for input data
            IReadOnlyDictionary<string, (string variablePath, Type type, int byteSize)> fbSymbols = ReadFbSymbols(adsConnection, adsCommandFbPath, PlcAttributeNames.PlcCommandInput);

            var missingInputVariables = inputData.Keys.Where(x => !fbSymbols.ContainsKey(x)).ToArray();
            if (missingInputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath,
                    string.Format(CommandResources.ERR_InputVariablesMissing, string.Join(",", missingInputVariables)));
            }

            var symbols = new List<string>();
            var streams = new List<AdsStream>();
            foreach (var name in inputData.Keys)
            {
                var symbolInfo = fbSymbols[name];
                symbols.Add(symbolInfo.variablePath);
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
