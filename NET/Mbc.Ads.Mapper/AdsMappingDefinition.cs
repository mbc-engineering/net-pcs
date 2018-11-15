//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;

namespace Mbc.Ads.Mapper
{
    internal class AdsMappingDefinition<TDestination>
    {
        internal AdsMappingDefinition(ITcAdsDataType sourceType)
        {
            SourceType = sourceType;

            if (sourceType.BaseType.Category == DataTypeCategory.Array)
            {
                SourceElementType = sourceType.BaseType.BaseType.ManagedType;
            }
            else if (sourceType.BaseType.Category == DataTypeCategory.Enum)
            {
                SourceElementType = sourceType.BaseType.BaseType.ManagedType;
            }
            else
            {
                SourceElementType = sourceType.BaseType.ManagedType;
            }
        }

        internal ITcAdsDataType SourceType { get; }

        internal Type SourceElementType { get; }

        internal Func<AdsBinaryReader, object> StreamReadFunction { get; set; }

        internal Action<AdsBinaryWriter, object> StreamWriterFunction { get; set; }

        internal Action<TDestination, object> DataObjectValueSetter { get; set; }

        internal Func<TDestination, object> DataObjectValueGetter { get; set; }

        internal IDestinationMemberConfiguration DestinationMemberConfiguration { get; set; }

        internal object ConvertFromSourceToDestination(object value)
        {
            if (DestinationMemberConfiguration.HasConverter)
            {
                return DestinationMemberConfiguration.Convert(value);
            }
            else
            {
                if (DestinationMemberConfiguration.MemberElementType.IsEnum)
                {
                    return ConvertEnumValueFromPlc(value);
                }
                else
                {
                    return Convert.ChangeType(value, DestinationMemberConfiguration.MemberElementType);
                }
            }
        }

        internal object ConvertFromDestinationToSource(object value)
        {
            if (DestinationMemberConfiguration.MemberElementType.IsEnum)
            {
                return ConvertEnumValueToPlc(value);
            }
            else
            {
                // Die SPS kennt zwei spezielle primitive Typen: TIME und DATE
                if (SourceElementType == typeof(TIME))
                {
                    if (value is TimeSpan)
                    {
                        return value;
                    }
                    return new TIME(Convert.ToInt64(value)).Time;
                }
                else if (SourceElementType == typeof(DATE))
                {
                    if (value is DateTime)
                    {
                        return value;
                    }
                    return new DATE(Convert.ToInt64(value)).Date;
                }
                else if (SourceElementType == typeof(DT))
                {
                    if (value is DateTime)
                    {
                        return value;
                    }
                    return new DT(Convert.ToInt64(value)).Date;
                }
                else
                {
                    return Convert.ChangeType(value, SourceElementType);
                }
            }
        }

        /// <summary>
        /// Converts a PLC enum value to a .NET enum value.
        /// </summary>
        /// <param name="sourceValue">the PLC enumeration value.</param>
        /// <param name="targetType">the .NET target type.</param>
        /// <param name="plcEnumValues">the PLC enumeration values.</param>
        /// <returns>A .NET enumeration value</returns>
        private object ConvertEnumValueFromPlc(object sourceValue)
        {
            try
            {
                // TODO can be optimized with remaining code
                var enumValues = SourceType.BaseType.EnumValues.ToDictionary(i => i.Primitive, i => i.Name);

                long numericEnumValueToSet = Convert.ToInt64(sourceValue);

                var enumKey = enumValues.Keys.FirstOrDefault(x => Convert.ToInt64(x) == numericEnumValueToSet);

                string plcEnumName;
                if (enumKey != null)
                {
                    plcEnumName = enumValues[enumKey];
                }
                else
                {
                    // TODO Enum-Dict ist nicht sortiert, daher ist First() falsch
                    // TODO Leeres EnumValue berücksichtigen
                    // Achtung: Wenn Enum kein 0 Value definiert hat, und value = 0 ist, muss der 1. Enum verwendet werden als Default
                    plcEnumName = enumValues.First().Value;
                }

                // TODO konfiguriererbares Mapping (Prefix e)
                return Enum.Parse(DestinationMemberConfiguration.MemberElementType, plcEnumName.TrimStart('e'), true);
            }
            catch (Exception ex)
            {
                throw new AdsMapperException($"Could not parse the Enumeration value of '{sourceValue}' to '{DestinationMemberConfiguration.MemberElementType}'", ex);
            }
        }

        /// <summary>
        /// Converts a .NET enum value to a PLC enum value.
        /// </summary>
        /// <param name="sourceValue">the .NET enum value</param>
        /// <param name="sourceType">the .NET enum type</param>
        /// <param name="plcEnumValues">PLC enum values</param>
        /// <returns>a plc enum value</returns>
        private object ConvertEnumValueToPlc(object sourceValue)
        {
            try
            {
                // TODO can be optimized with remaining code
                var enumValues = SourceType.BaseType.EnumValues.ToDictionary(i => i.Primitive, i => i.Name);

                var name = Enum.GetName(DestinationMemberConfiguration.MemberElementType, sourceValue);
                var plcEnum = enumValues.Where(x => x.Value.TrimStart('e') == name).Select(x => x.Key).FirstOrDefault();
                if (plcEnum != null)
                {
                    return plcEnum;
                }

                // TODO Enum-Dict ist nicht sortiert, daher ist First() falsch
                // TODO Leeres EnumValue berücksichtigen
                // Achtung: Wenn Enum kein 0 Value definiert hat, und value = 0 ist, muss der 1. Enum verwendet werden als Default
                return enumValues.First().Key;
            }
            catch (Exception ex)
            {
                throw new AdsMapperException($"Could not map the enumeration value of '{sourceValue}' to plc.", ex);
            }
        }

    }
}
