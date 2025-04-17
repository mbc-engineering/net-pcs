//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils;
using Mbc.Ads.Utils.SumCommand;
using System;
using System.Buffers;
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

            // This are the the tested types
            var validTypeCategories = new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String, DataTypeCategory.Array, DataTypeCategory.Alias };
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

            // fb required flaged arguments must be in the CommandOutput
            var missingRequiredOutputVariables = requiredfbItems.Where(x => !outputNames.Contains(x.Key)).ToArray();
            if (missingOutputVariables.Length > 0)
            {
                throw new PlcCommandException(adsCommandFbPath, string.Format(CommandResources.ERR_RequiredOutputVariablesMissing, string.Join(",", missingOutputVariables)));
            }

            var symbols = new List<string>();
            var readSizes = new List<int>();
            foreach (string name in outputNames)
            {
                IMember item = fbItems[name];
                symbols.Add(adsCommandFbPath + "." + item.InstanceName);
                readSizes.Add(item.DataType.ByteSize);
            }

            var handleCreator = new SumCreateHandles(adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumReader = new SumHandleReadData(adsConnection, handles, readSizes.ToArray());
                var marshaledValues = sumReader.Read();

                for (int i = 0; i < marshaledValues.Count; i++)
                {
                    IMember item = fbItems[outputNames[i]];
                    var encoding = item.ValueEncoding;
                    var converter = new PrimitiveTypeMarshaler(encoding);
                    var valueType = output.GetOutputDataType(outputNames[i]);
                    try
                    {
                        converter.Unmarshal(item.DataType, marshaledValues[i].Span, valueType, out object value);
                        output.SetOutputData(outputNames[i], value);

                    }
                    catch (Exception ex)
                        when (ex is DataTypeException               // Cannot map to.NET Value!
                            || ex is ArgumentOutOfRangeException)   // source or ValueType parameter mismatches dataTypes managed type! - valueType
                    {
                        // Cannot map to .NET Value or source or ValueType parameter mismatches dataTypes managed type!
                        throw new PlcCommandException($"Output variable {outputNames[i]} has not compatible type {valueType.Name} to the PLC data type {item.DataType.ToString()}, it cannot be handled with PrimitiveTypeMarshaler.", ex);
                    }
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

            // This are the the tested types
            var validTypeCategories = new[] { DataTypeCategory.Primitive, DataTypeCategory.Enum, DataTypeCategory.String, DataTypeCategory.Array, DataTypeCategory.Alias };
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
            var marshaledValues = new List<ReadOnlyMemory<byte>>();
            // Based on fbItems, write the values from  ICommandInput data to fb
            foreach (KeyValuePair<string, IMember> fbItem in fbItems)
            {
                IMember item = fbItem.Value;
                var encoding = item.ValueEncoding;
                var converter = new PrimitiveTypeMarshaler(encoding);
                symbols.Add(adsCommandFbPath + "." + item.InstanceName);

                if (!inputData.TryGetValue(fbItem.Key, out object value))
                {
                    // Set default value for PlcAttributeNames.PlcCommandInputOptional when they not exist in inputData                    
                    if (item.DataType.IsPrimitive())
                    {
                        value = item.DataType.GetManagedType().GetDefaultValue();
                    }
                    else
                    {
                        // Set fallback value 0 of byte length
                        value = new byte[item.DataType.ByteSize];
                    }
                }

                Memory<byte> marshaledValueBuffer = new byte[item.DataType.ByteSize];
                if (!converter.TryMarshal(item.DataType, encoding, value, marshaledValueBuffer.Span, out int size))
                {
                    throw new PlcCommandException(string.Format("Input variable {0} has invalid PLC data type {1} to serialize with PrimitiveTypeMarshaler.", item.InstanceName, item.DataType.ToString()));
                }

                marshaledValues.Add(marshaledValueBuffer.Slice(0, size).ToArray());
            }

            var handleCreator = new SumCreateHandles(adsConnection, symbols);
            var handles = handleCreator.CreateHandles();
            try
            {
                var sumWriter = new SumHandleWriteData(adsConnection, handles);
                sumWriter.Write(marshaledValues);
                sumWriter.Write(marshaledValues.ToArray());
            }
            finally
            {
                var handleReleaser = new SumReleaseHandles(adsConnection, handles);
                handleReleaser.ReleaseHandles();
            }
        }
    }
}
