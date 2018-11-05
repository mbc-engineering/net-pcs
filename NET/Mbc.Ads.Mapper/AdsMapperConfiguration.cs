//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using System;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;

namespace Mbc.Ads.Mapper
{
    public class AdsMapperConfiguration<TDestination>
        where TDestination : new()
    {
        private readonly AdsMappingExpression<TDestination> _config;

        public AdsMapperConfiguration(Action<IAdsMappingExpression<TDestination>> config)
        {
            // Setup Mapper
            _config = new AdsMappingExpression<TDestination>();
            config(_config);
        }

        /// <summary>
        /// Create a instance of a <see cref="AdsMapper{TDestination}"/> from a <see cref="IAdsSymbolInfo"/>
        /// It analyze the symbol structure and create a mapping from the configuration.
        /// </summary>
        /// <param name="symbolInfo">the ADS symbol information to construct the mapper</param>
        /// <returns>a <see cref="AdsMapper{TDestination}"/> for the given symbol</returns>
        public AdsMapper<TDestination> CreateAdsMapper(IAdsSymbolInfo symbolInfo)
        {
            var mapper = new AdsMapper<TDestination>();

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

        private void AddSymbolsMappingRecursive(ITcAdsDataType item, int offset, string name, AdsMapper<TDestination> mapper)
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

        private void AddPrimitiveSymbolsMapping(Type primitiveManagedType, int offset, string name, AdsMapper<TDestination> mapper)
        {
            var definition = new AdsMappingDefinition<TDestination>();

            var memberMappingConfiguration = FindAdsMappingDefinition(name);
            if (memberMappingConfiguration == null)
            {
                return;
            }

            memberMappingConfiguration.Destination.MatchSome(dest => definition.DestinationMemberConfiguration = dest);

            var readFunction = CreatePrimitiveTypeReadFunction(primitiveManagedType, offset);
            definition.StreamReadFunction = readFunction;

            mapper.AddStreamMapping(definition);
        }

        /// <summary>
        /// Adds a <see cref="AdsMappingDefinition{TDestination}"/> to the given <paramref name="mapper"/> instance.
        /// </summary>
        /// <param name="item">the ADS item of the mapping</param>
        /// <param name="offset">the byte offset of the item in the data stream</param>
        /// <param name="name">the name of the symbol</param>
        /// <param name="mapper">the ADS mapper</param>
        private void AddEnumSymbolsMapping(ITcAdsDataType item, int offset, string name, AdsMapper<TDestination> mapper)
        {
            var enumValues = item.BaseType.EnumValues.ToDictionary(i => i.Primitive, i => i.Name);
            var definition = new AdsMappingDefinition<TDestination>(enumValues);

            var memberMappingConfiguration = FindAdsMappingDefinition(name);
            if (memberMappingConfiguration == null)
            {
                return;
            }

            memberMappingConfiguration.Destination.MatchSome(dest => definition.DestinationMemberConfiguration = dest);

            var readFunction = CreatePrimitiveTypeReadFunction(item.BaseType.BaseType.ManagedType, offset);
            definition.StreamReadFunction = readFunction;

            mapper.AddStreamMapping(definition);
        }

        private void AddArraySymbolsMapping(ITcAdsDataType arrayItem, int offset, string name, AdsMapper<TDestination> mapper)
        {
            ITcAdsDataType arrayValueType = arrayItem.BaseType.BaseType;
            int valuesInArray = arrayItem.BaseType.Dimensions.ElementCount;

            var memberMappingConfiguration = FindAdsMappingDefinition(name);
            if (memberMappingConfiguration == null)
            {
                return;
            }

            for (int idx = 0; idx < valuesInArray; idx++)
            {
                var definition = new AdsMappingDefinition<TDestination>(idx);
                memberMappingConfiguration.Destination.MatchSome(dest => definition.DestinationMemberConfiguration = dest);
                int actStreamOffset = offset + (idx * arrayValueType.Size);
                var readFunction = CreatePrimitiveTypeReadFunction(arrayValueType.ManagedType, actStreamOffset);

                if (readFunction != null)
                {
                    definition.StreamReadFunction = readFunction;
                    mapper.AddStreamMapping(definition);
                }
            }
        }

        private MemberMappingConfiguration FindAdsMappingDefinition(string sourceSymbolName)
        {
            var mappingDefinition = new AdsMappingDefinition<TDestination>();

            // Get Mapping configuration from source member name
            var memberMapping = _config.GetMappingFromSource(sourceSymbolName);

            if (!memberMapping.Destination.HasValue)
            {
                // TDestionation member does not exist, skip to read this symbol
                return null;
            }

            return memberMapping;
        }

        /// <summary>
        /// Create a <see cref="AdsMapper<TDestination>.AdsStreamMappingDelegate"/> to read the stream with the correct
        /// configuration characteristics and set it to the <see cref="TDestination"/> mapped member
        /// </summary>
        /// <param name="managedType">the .net type to read</param>
        /// <param name="streamOffset">The offset of the SubItem in Bytes</param>
        /// <returns></returns>
        private AdsMapper<TDestination>.AdsMappingStreamReaderDelegate CreatePrimitiveTypeReadFunction(Type managedType, int streamByteOffset)
        {
            // Guards
            Ensure.Any.IsNotNull(managedType, optsFn: opts => opts.WithMessage("Could not create AdsStreamMappingDelegate for a PrimitiveType because the managedType is null."));

            // Declare repeated code for reading the stream symbol
            // ---------------------------------------------------
            void BevoreReadSymbol(AdsBinaryReader adsReader)
            {
                // Move reader position to the right offset
                adsReader.BaseStream.Position = streamByteOffset;
            }

            // Create Read delegate functions
            // ---------------------
            if (managedType == typeof(bool))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadBoolean();
                    return value;
                };
            }

            if (managedType == typeof(byte))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadByte();
                    return value;
                };
            }

            if (managedType == typeof(sbyte))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadSByte();
                    return value;
                };
            }

            if (managedType == typeof(ushort))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadUInt16();
                    return value;
                };
            }

            if (managedType == typeof(short))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadInt16();
                    return value;
                };
            }

            if (managedType == typeof(uint))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadUInt32();
                    return value;
                };
            }

            if (managedType == typeof(int))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadInt32();
                    return value;
                };
            }

            if (managedType == typeof(float))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadSingle();
                    return value;
                };
            }

            if (managedType == typeof(double))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadDouble();
                    return value;
                };
            }

            if (managedType == typeof(TIME))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadPlcTIME();
                    return value;
                };
            }

            if (managedType == typeof(DATE))
            {
                return (destinationObject, adsReader) =>
                {
                    BevoreReadSymbol(adsReader);
                    object value = adsReader.ReadPlcDATE();
                    return value;
                };
            }

            throw new NotSupportedException($"AdsStreamMappingDelegate execution not supported for the ManagedType '{managedType?.ToString()}'.");
        }
    }
}
