//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils;
using System;
using System.Collections.Generic;
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
            IDictionary<string, IMember> fbItems = ReadFbSymbols(adsConnection, adsCommandFbPath, new string[] { PlcAttributeNames.PlcCommandOutput, PlcAttributeNames.PlcCommandOutputOptional });
            IDictionary<string, IMember> requiredfbItems = fbItems
                .Where(x => x.Value.Attributes.Any(a => string.Equals(a.Name, PlcAttributeNames.PlcCommandOutput, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(x => x.Key, x => x.Value);

            var validTypeCategories = new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String };
            foreach (IMember item in fbItems.Values)
            {
                if (!validTypeCategories.Contains(item.DataType.Category))
                    throw new PlcCommandException(string.Format("Output variable {0} has invalid category {1}.", item.InstanceName, item.DataType.Category));
            }

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
            var types = new List<Type>();
            foreach (string name in outputNames)
            {
                IMember item = fbItems[name];
                symbols.Add(adsCommandFbPath + "." + item.InstanceName);
                types.Add(item.DataType.GetManagedType());
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
            IDictionary<string, IMember> fbItems = ReadFbSymbols(adsConnection, adsCommandFbPath, new string[] { PlcAttributeNames.PlcCommandInput, PlcAttributeNames.PlcCommandInputOptional });
            IDictionary<string, IMember> requiredfbItems = fbItems
                .Where(x => x.Value.Attributes.Any(a => string.Equals(a.Name, PlcAttributeNames.PlcCommandInput, StringComparison.OrdinalIgnoreCase)))
                .ToDictionary(x => x.Key, x => x.Value);

            var validTypeCategories = new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String };
            foreach (var item in fbItems.Values)
            {
                if (!validTypeCategories.Contains(item.DataType.Category))
                    throw new PlcCommandException(string.Format("Input variable {0} has invalid data type category {1}.", item.InstanceName, item.DataType.Category));
            }

            IDictionary<string, object> inputData = input.GetInputData();

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
            var types = new List<Type>();
            var values = new List<object>();
            // Based on fbItems, write the values from  ICommandInput data to fb
            foreach (KeyValuePair<string, IMember> fbItem in fbItems)
            {
                IMember item = fbItem.Value;
                Type type = item.DataType.GetManagedType();
                symbols.Add(adsCommandFbPath + "." + item.InstanceName);
                types.Add(type);

                if (inputData.TryGetValue(fbItem.Key, out object value))
                {
                    values.Add(AdsConvert.ChangeType(value, type));
                }
                else
                {
                    values.Add(type.GetDefaultValue());
                }
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
    }
}
