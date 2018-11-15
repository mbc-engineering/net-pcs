//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using System;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.TypeSystem;

namespace Mbc.Ads.Mapper
{
    public class AdsMapperConfiguration<TDataObject>
        where TDataObject : new()
    {
        private readonly AdsMappingExpression<TDataObject> _config;

        public AdsMapperConfiguration(Action<IAdsMappingExpression<TDataObject>> cfgExpression)
        {
            // Setup Mapper
            _config = new AdsMappingExpression<TDataObject>();
            cfgExpression(_config);
        }

        /// <summary>
        /// Create a instance of a <see cref="AdsMapper{TDataObject}"/> from a <see cref="IAdsSymbolInfo"/>
        /// It analyze the symbol structure and create a mapping from the configuration.
        /// </summary>
        /// <param name="symbolInfo">the ADS symbol information to construct the mapper</param>
        /// <returns>A <see cref="AdsMapper{TDataObject}"/> for the given symbol</returns>
        public AdsMapper<TDataObject> CreateAdsMapper(IAdsSymbolInfo symbolInfo)
        {
            var mapper = new AdsMapper<TDataObject>();

            // ToDo: @MiHe can call directly AddSymbolsMappingRecursive ensteed of looping subitems when structs are supported
            if (symbolInfo.Symbol.Category != DataTypeCategory.Struct)
            {
                throw new NotSupportedException("Can create only Ads Mappings for Structs");
            }

            Ensure.Bool.IsTrue(symbolInfo.Symbol.DataType.HasSubItemInfo);

            foreach (var subItem in symbolInfo.Symbol.DataType.SubItems)
            {
                AddSymbolsMappingRecursive(subItem, subItem.Offset, subItem.SubItemName, mapper);
            }

            return mapper;
        }

        private void AddSymbolsMappingRecursive(ITcAdsDataType item, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            // Check for the right item type
            // -----------------------------
            switch (item.BaseType.Category)
            {
                case DataTypeCategory.Primitive:
                    AddPrimitiveSymbolsMapping(item.BaseType.ManagedType, offset, name, mapper);
                    break;
                case DataTypeCategory.Enum:
                    AddEnumSymbolsMapping(item, offset, name, mapper);
                    break;
                case DataTypeCategory.Array:
                    switch (item.BaseType.BaseType.Category)
                    {
                        case DataTypeCategory.Primitive:
                            AddArraySymbolsMapping(item, offset, name, mapper);
                            break;
                        case DataTypeCategory.Enum:
                        case DataTypeCategory.Struct:
                        case DataTypeCategory.String:
                            throw new NotImplementedException($"This Category type '{item.BaseType.BaseType.Category}' used for the Array PLC Varialbe {name} is yet not implemented.");
                        default:
                            throw new NotSupportedException($"This Category type '{item.BaseType.BaseType.Category}' used for the Array PLC Varialbe {name} is not supported.");
                    }

                    break;
                case DataTypeCategory.Struct:
                case DataTypeCategory.String:
                    throw new NotImplementedException($"This Category type '{item.BaseType.Category}' used for PLC Varialbe {name} is yet not implemented.");
                case DataTypeCategory.Alias:
                    // If alias call it recursive to find underlying primitive
                    AddSymbolsMappingRecursive(item.BaseType, offset, name, mapper);
                    break;
                default:
                    throw new NotSupportedException($"This Category type '{item.BaseType.Category}' used for PLC Varialbe {name} is not supported.");
            }

            // Handle supitems if exists
            if (item.HasSubItemInfo)
            {
                foreach (ITcAdsSubItem subItem in item.SubItems)
                {
                    AddSymbolsMappingRecursive(subItem, subItem.Offset, subItem.SubItemName, mapper);
                }
            }
        }

        private void AddPrimitiveSymbolsMapping(Type primitiveManagedType, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            var memberMappingConfiguration = FindAdsMappingDefinition(name);
            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                var definition = new AdsMappingDefinition<TDataObject>();
                definition.DestinationMemberConfiguration = dest;

                definition.StreamReadFunction = PrimitiveDataTypeMapping.CreatePrimitiveTypeReadFunction(primitiveManagedType, offset);
                definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest);

                definition.StreamWriterFunction = PrimitiveDataTypeMapping.CreatePrimitiveTypeWriteFunction(primitiveManagedType, offset);

                mapper.AddStreamMapping(definition);
            });
        }

        /// <summary>
        /// Adds a <see cref="AdsMappingDefinition{TDataObject}"/> to the given <paramref name="mapper"/> instance.
        /// </summary>
        /// <param name="item">the ADS item of the mapping</param>
        /// <param name="offset">the byte offset of the item in the data stream</param>
        /// <param name="name">the name of the symbol</param>
        /// <param name="mapper">the ADS mapper</param>
        private void AddEnumSymbolsMapping(ITcAdsDataType item, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            var definition = new AdsMappingDefinition<TDataObject>();

            var memberMappingConfiguration = FindAdsMappingDefinition(name);

            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                var enumValues = item.BaseType.EnumValues.ToDictionary(i => i.Primitive, i => i.Name);

                definition.DestinationMemberConfiguration = dest;

                definition.StreamReadFunction = PrimitiveDataTypeMapping.CreatePrimitiveTypeReadFunction(item.BaseType.BaseType.ManagedType, offset);
                definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest, enumValues:enumValues);

                definition.StreamWriterFunction = PrimitiveDataTypeMapping.CreatePrimitiveTypeWriteFunction(item.BaseType.BaseType.ManagedType, offset);

                mapper.AddStreamMapping(definition);
            });
        }

        private void AddArraySymbolsMapping(ITcAdsDataType arrayItem, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            ITcAdsDataType arrayValueType = arrayItem.BaseType.BaseType;
            int valuesInArray = arrayItem.BaseType.Dimensions.ElementCount;

            var memberMappingConfiguration = FindAdsMappingDefinition(name);
            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                for (int idx = 0; idx < valuesInArray; idx++)
                {
                    var definition = new AdsMappingDefinition<TDataObject>();
                    definition.DestinationMemberConfiguration = dest;

                    int actStreamOffset = offset + (idx * arrayValueType.Size);
                    definition.StreamReadFunction = PrimitiveDataTypeMapping.CreatePrimitiveTypeReadFunction(arrayValueType.ManagedType, actStreamOffset);

                    var capturedIdx = idx;
                    definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest, arrayIndex: idx);

                    definition.StreamWriterFunction = PrimitiveDataTypeMapping.CreatePrimitiveTypeWriteFunction(arrayValueType.ManagedType, actStreamOffset);
                    mapper.AddStreamMapping(definition);
                }
            });
        }

        private MemberMappingConfiguration FindAdsMappingDefinition(string sourceSymbolName)
        {
            // Get Mapping configuration from source member name
            return _config.GetMappingFromSource(sourceSymbolName);
        }
    }
}
