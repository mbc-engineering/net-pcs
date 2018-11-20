//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

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
            IDictionary<string, ITcAdsSubItem> items = ReadFbSymbols(adsConnection, adsCommandFbPath, PlcAttributeNames.PlcCommandOutput);

            var validTypeCategories = new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String };
            foreach (var item in items.Values)
            {
                if (!validTypeCategories.Contains(item.BaseType.Category))
                    throw new PlcCommandException(string.Format("Output variable {0} has invalid category {1}.", item.SubItemName, item.Category));
            }

            // ToList => deterministische Reihenfolge notwendig
            List<string> outputNames = output.GetOutputNames().ToList();

            var missingOutputVariables = outputNames.Where(x => !items.ContainsKey(x)).ToArray();
            if (missingOutputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_OutputVariablesMissing, string.Join(",", missingOutputVariables)));
            }

            var symbols = new List<string>();
            var types = new List<Type>();
            foreach (var name in outputNames)
            {
                var item = items[name];
                symbols.Add(adsCommandFbPath + "." + item.SubItemName);
                types.Add(GetManagedTypeForSubItem(item));
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
            IDictionary<string, ITcAdsSubItem> items = ReadFbSymbols(adsConnection, adsCommandFbPath, PlcAttributeNames.PlcCommandInput);

            var validTypeCategories = new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String };
            foreach (var item in items.Values)
            {
                if (!validTypeCategories.Contains(item.BaseType.Category))
                    throw new PlcCommandException(string.Format("Input variable {0} has invalid data type category {1}.", item.SubItemName, item.Category));
            }

            IDictionary<string, object> inputData = input.GetInputData();

            var missingInputVariables = inputData.Keys.Where(x => !items.ContainsKey(x)).ToArray();
            if (missingInputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_InputVariablesMissing, string.Join(",", missingInputVariables)));
            }

            var symbols = new List<string>();
            var types = new List<Type>();
            var values = new List<object>();
            foreach (var name in inputData.Keys)
            {
                var item = items[name];
                var type = GetManagedTypeForSubItem(item);
                symbols.Add(adsCommandFbPath + "." + item.SubItemName);
                types.Add(type);
                values.Add(Convert.ChangeType(inputData[name], type));
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

        private static Type GetManagedTypeForSubItem(ITcAdsSubItem subitem)
        {
            if (subitem.BaseType.ManagedType != null)
                return subitem.BaseType.ManagedType;

            return subitem.BaseType.BaseType.ManagedType;
        }
    }
}
