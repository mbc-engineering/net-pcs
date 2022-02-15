//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils;
using System;
using TwinCAT.Ads.TypeSystem;
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
            IAdsSymbol symbol = symbolInfo.Symbol;

            return CreateAdsMapper(symbol);

        }

        public AdsMapper<TDataObject> CreateAdsMapper(IAdsSymbol symbol)
        {
            if (!(symbol.DataType is IStructType structType))
            {
                throw new NotSupportedException("Can create only Ads Mappings for Structs");
            }

            // TODO this is a workaround because `structType.Members.[x].Category` contains zero otherwise
            _ = ((IStructInstance)symbol).MemberInstances;

            var mapper = new AdsMapper<TDataObject>(symbol.ByteSize);

            foreach (IMember subItem in structType.Members)
            {
                AddSymbolsMappingRecursive(subItem, subItem.Offset, subItem.InstanceName, mapper);
            }

            return mapper;
        }

        private void AddSymbolsMappingRecursive(IMember item, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            // Check for the right item type
            // -----------------------------
            switch (item.DataType.Category)
            {
                case DataTypeCategory.Primitive:
                    AddPrimitiveSymbolsMapping((IPrimitiveType)item.DataType, offset, name, mapper);
                    break;
                case DataTypeCategory.Enum:
                    AddEnumSymbolsMapping((IEnumType)item.DataType, offset, name, mapper);
                    break;
                case DataTypeCategory.Array:
                    IArrayType arrayType = (IArrayType)item.DataType;
                    switch (arrayType.ElementType.Category)
                    {
                        case DataTypeCategory.Primitive:
                            AddArraySymbolsMapping(arrayType, offset, name, mapper);
                            break;
                        case DataTypeCategory.Enum:
                        case DataTypeCategory.Struct:
                        case DataTypeCategory.String:
                            throw new NotImplementedException($"This Category type '{arrayType.ElementType.Category}' used for the Array PLC Varialbe {name} is yet not implemented.");
                        default:
                            throw new NotSupportedException($"This Category type '{arrayType.ElementType.Category}' used for the Array PLC Varialbe {name} is not supported.");
                    }
                    break;
                case DataTypeCategory.String:
                    AddStringSymbolsMapping((IStringType)item.DataType, offset, name, mapper);
                    break;
                case DataTypeCategory.Struct:
                    throw new NotImplementedException($"This Category type '{item.DataType.Category}' used for PLC Varialbe {name} is yet not implemented.");
                // TODO
                //case DataTypeCategory.Alias:
                //    // If alias call it recursive to find underlying primitive
                //    IAliasType aliasType = (IAliasType)item.DataType;
                //    AddSymbolsMappingRecursive(aliasType.BaseType, offset, name, mapper);
                //    break;
                default:
                    throw new NotSupportedException($"This Category type '{item.DataType.Category}' used for PLC Varialbe {name} is not supported.");
            }

            // Handle supitems if exists
            if (item.DataType is IStructType structType)
            {
                // TODO this is a workaround because `structType.Members.[x].Category` contains zero otherwise
                _ = ((IStructInstance)item).MemberInstances;

                foreach (IMember member in structType.Members)
                {
                    AddSymbolsMappingRecursive(member, member.Offset, member.InstanceName, mapper);
                }
            }
        }

        private void AddPrimitiveSymbolsMapping(IPrimitiveType primitiveType, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            var memberMappingConfiguration = FindAdsMappingDefinition(name);
            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                var primitiveManagedType = primitiveType.GetManagedType();

                var definition = new AdsMappingDefinition<TDataObject>(primitiveType, primitiveManagedType);
                definition.DestinationMemberConfiguration = dest;

                definition.AdsDataReader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(primitiveManagedType, offset, primitiveType);
                definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest.Member);

                definition.AdsDataWriter = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(primitiveManagedType, offset, primitiveType);
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
        private void AddEnumSymbolsMapping(IEnumType adsDataType, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            MemberMappingConfiguration memberMappingConfiguration = FindAdsMappingDefinition(name);

            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                Type baseManagedType = adsDataType.BaseType.GetManagedType();

                var definition = new AdsMappingDefinition<TDataObject>(adsDataType, baseManagedType);
                definition.DestinationMemberConfiguration = dest;

                definition.AdsDataReader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(baseManagedType, offset, adsDataType.BaseType);
                definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest.Member);

                definition.AdsDataWriter = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(baseManagedType, offset, adsDataType.BaseType);
                definition.DataObjectValueGetter = DataObjectAccessor.CreateValueGetter<TDataObject>(dest.Member);

                mapper.AddStreamMapping(definition);
            });
        }

        private void AddArraySymbolsMapping(IArrayType arrayType, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            IDataType arrayValueType = arrayType.ElementType;
            Type baseManagedType = arrayValueType.GetManagedType();
            int valuesInArray = arrayType.Dimensions.ElementCount;

            var memberMappingConfiguration = FindAdsMappingDefinition(name);
            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                for (int idx = 0; idx < valuesInArray; idx++)
                {
                    var definition = new AdsMappingDefinition<TDataObject>(arrayType, baseManagedType);
                    definition.DestinationMemberConfiguration = dest;

                    int actStreamOffset = offset + (idx * arrayValueType.ByteSize);
                    var capturedIdx = idx;

                    definition.AdsDataReader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(baseManagedType, actStreamOffset, arrayValueType);
                    definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest.Member, arrayIndex: capturedIdx);

                    definition.AdsDataWriter = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(baseManagedType, actStreamOffset, arrayValueType);
                    definition.DataObjectValueGetter = DataObjectAccessor.CreateValueGetter<TDataObject>(dest.Member, arrayIndex: capturedIdx);

                    mapper.AddStreamMapping(definition);
                }
            });
        }

        private void AddStringSymbolsMapping(IStringType stringType, int offset, string name, AdsMapper<TDataObject> mapper)
        {
            Type managedType = stringType.GetManagedType();

            var memberMappingConfiguration = FindAdsMappingDefinition(name);

            memberMappingConfiguration.Destination.MatchSome(dest =>
            {
                var definition = new AdsMappingDefinition<TDataObject>(stringType, managedType);
                definition.DestinationMemberConfiguration = dest;

                definition.AdsDataReader = AdsBinaryAccessorFactory.CreatePrimitiveTypeReadFunction(managedType, offset, stringType);
                definition.DataObjectValueSetter = DataObjectAccessor.CreateValueSetter<TDataObject>(dest.Member);

                definition.AdsDataWriter = AdsBinaryAccessorFactory.CreatePrimitiveTypeWriteFunction(managedType, offset, stringType);
                definition.DataObjectValueGetter = DataObjectAccessor.CreateValueGetter<TDataObject>(dest.Member);

                mapper.AddStreamMapping(definition);
            });
        }

        private MemberMappingConfiguration FindAdsMappingDefinition(string sourceSymbolName)
        {
            // Get Mapping configuration from source member name
            return _config.GetMappingFromSource(sourceSymbolName);
        }
    }
}
