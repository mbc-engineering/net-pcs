//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using System;
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
            var mapper = new AdsMapper<TDataObject>(symbolInfo.SymbolsSize);

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

        public AdsMapper<TDataObject> CreateAdsMapper(ITcAdsDataType dataType)
        {
            if (dataType.Category != DataTypeCategory.Struct)
            {
                throw new NotSupportedException("Can create only Ads Mappings for Structs");
            }

            Ensure.Bool.IsTrue(dataType.HasSubItemInfo);

            var mapper = new AdsMapper<TDataObject>(dataType.ByteSize);
            foreach (var subItem in dataType.SubItems)
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
                    AddPrimitiveSymbolsMapping(item, offset, name, mapper);
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

        private void AddPrimitiveSymbolsMapping(ITcAdsDataType adsDataType, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            var memberMappingConfiguration = FindAdsMappingDefinition(name);
            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                var primitiveManagedType = adsDataType.BaseType.ManagedType;

                var definition = new AdsMappingDefinition<TDataObject>(adsDataType);
                definition.DestinationMemberConfiguration = dest;

                definition.StreamReadFunction = AdsStreamAccessor.CreatePrimitiveTypeReadFunction(primitiveManagedType, offset);
                definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest.Member);

                definition.StreamWriterFunction = AdsStreamAccessor.CreatePrimitiveTypeWriteFunction(primitiveManagedType, offset);
                definition.DataObjectValueGetter = DataObjectAccessor.CreateValueGetter<TDataObject>(dest.Member);

                mapper.AddStreamMapping(definition);
            });
        }

        /// <summary>
        /// Adds a <see cref="AdsMappingDefinition{TDataObject}"/> to the given <paramref name="mapper"/> instance.
        /// </summary>
        /// <param name="adsDataType">the ADS item of the mapping</param>
        /// <param name="offset">the byte offset of the item in the data stream</param>
        /// <param name="name">the name of the symbol</param>
        /// <param name="mapper">the ADS mapper</param>
        private void AddEnumSymbolsMapping(ITcAdsDataType adsDataType, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            var memberMappingConfiguration = FindAdsMappingDefinition(name);

            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                var definition = new AdsMappingDefinition<TDataObject>(adsDataType);
                definition.DestinationMemberConfiguration = dest;

                definition.StreamReadFunction = AdsStreamAccessor.CreatePrimitiveTypeReadFunction(adsDataType.BaseType.BaseType.ManagedType, offset);
                definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest.Member);

                definition.StreamWriterFunction = AdsStreamAccessor.CreatePrimitiveTypeWriteFunction(adsDataType.BaseType.BaseType.ManagedType, offset);
                definition.DataObjectValueGetter = DataObjectAccessor.CreateValueGetter<TDataObject>(dest.Member);

                mapper.AddStreamMapping(definition);
            });
        }

        private void AddArraySymbolsMapping(ITcAdsDataType adsDataType, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            ITcAdsDataType arrayValueType = adsDataType.BaseType.BaseType;
            int valuesInArray = adsDataType.BaseType.Dimensions.ElementCount;

            var memberMappingConfiguration = FindAdsMappingDefinition(name);
            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                for (int idx = 0; idx < valuesInArray; idx++)
                {
                    var definition = new AdsMappingDefinition<TDataObject>(adsDataType);
                    definition.DestinationMemberConfiguration = dest;

                    int actStreamOffset = offset + (idx * arrayValueType.Size);
                    var capturedIdx = idx;

                    definition.StreamReadFunction = AdsStreamAccessor.CreatePrimitiveTypeReadFunction(arrayValueType.ManagedType, actStreamOffset);
                    definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest.Member, arrayIndex: capturedIdx);

                    definition.StreamWriterFunction = AdsStreamAccessor.CreatePrimitiveTypeWriteFunction(arrayValueType.ManagedType, actStreamOffset);
                    definition.DataObjectValueGetter = DataObjectAccessor.CreateValueGetter<TDataObject>(dest.Member, arrayIndex: capturedIdx);

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
