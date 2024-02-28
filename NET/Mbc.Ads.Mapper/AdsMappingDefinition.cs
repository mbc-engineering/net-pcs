//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Utils;
using System;
using System.Linq;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;

namespace Mbc.Ads.Mapper
{
    internal class AdsMappingDefinition<TDestination>
    {
        internal AdsMappingDefinition(IDataType sourceType, Type sourceElementType)
        {
            SourceType = sourceType;
            SourceElementType = sourceElementType;
        }

        internal IDataType SourceType { get; }

        internal Type SourceElementType { get; }

        internal IAdsDataReader AdsDataReader { get; set; }

        internal IAdsDataWriter AdsDataWriter { get; set; }

        internal Action<TDestination, object> DataObjectValueSetter { get; set; }

        internal Func<TDestination, object> DataObjectValueGetter { get; set; }

        internal IDestinationMemberConfiguration DestinationMemberConfiguration { get; set; }

        internal object ConvertFromSourceToDestination(object value)
        {
            if (DestinationMemberConfiguration.HasSourceToDestinationConverter)
            {
                return DestinationMemberConfiguration.ConvertSourceToDestination(value);
            }

            if (DestinationMemberConfiguration.MemberElementType.IsEnum)
            {
                return ConvertEnumValueFromPlc(value);
            }

            return AdsConvert.ChangeType(value, DestinationMemberConfiguration.MemberElementType);
        }

        internal object ConvertFromDestinationToSource(object value)
        {
            if (DestinationMemberConfiguration.HasDestinationToSourceConverter)
            {
                return DestinationMemberConfiguration.ConvertDestinationToSource(value, SourceElementType);
            }

            if (DestinationMemberConfiguration.MemberElementType.IsEnum)
            {
                return ConvertEnumValueToPlc(value);
            }

            // Die SPS kennt mehrere spezielle primitive Typen (TIME, DATE, DT, TOD) die
            // von Convert nicht gewandelt werden können.
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

                return new DATE(Convert.ToUInt32(value)).Date;
            }
            else if (SourceElementType == typeof(DT))
            {
                if (value is DateTime)
                {
                    return value;
                }

                return new DT(Convert.ToUInt32(value)).DateTime;
            }
            else if (SourceElementType == typeof(TOD))
            {
                if (value is TimeSpan)
                {
                    return value;
                }

                return new TOD(Convert.ToInt64(value)).Time;
            }
            else
            {
                return Convert.ChangeType(value, SourceElementType);
            }
        }

        /// <summary>
        /// Converts a PLC enum value to a .NET enum value.
        /// </summary>
        /// <param name="sourceValue">the PLC enumeration value.</param>
        /// <returns>A .NET enumeration value</returns>
        private object ConvertEnumValueFromPlc(object sourceValue)
        {
            try
            {
                var plcEnumName = ((IEnumType)SourceType).EnumValues
                    .Where(x => object.Equals(x.Primitive, sourceValue))
                    .Select(x => x.Name)
                    .FirstOrDefault();

                if (plcEnumName == null)
                    throw new AdsMapperException($"Could not find primitive plc enum value '{sourceValue}' in enum type '{SourceType.FullName}'.");

                // TODO konfigurierbares Mapping #26
                return Enum.Parse(DestinationMemberConfiguration.MemberElementType, plcEnumName.TrimStart('e'), true);
            }
            catch (Exception ex) when (!(ex is AdsMapperException))
            {
                throw new AdsMapperException($"Could not map plc enum value '{sourceValue}' to .NET enum type '{DestinationMemberConfiguration.MemberElementType}'.");
            }
        }

        /// <summary>
        /// Converts a .NET enum value to a PLC enum value.
        /// </summary>
        /// <param name="sourceValue">the .NET enum value</param>
        /// <returns>A primitive plc enum value.</returns>
        private object ConvertEnumValueToPlc(object sourceValue)
        {
            try
            {
                var name = Enum.GetName(DestinationMemberConfiguration.MemberElementType, sourceValue);

                // TODO konfigurierbares Mapping #26
                var plcPrimitiveValue = ((IEnumType)SourceType).EnumValues
                    .Where(x => x.Name.TrimStart('e') == name)
                    .Select(x => x.Primitive)
                    .FirstOrDefault();

                if (plcPrimitiveValue != null)
                    return plcPrimitiveValue;

                throw new AdsMapperException($"Could not map .NET enum value '{name}' of type '{DestinationMemberConfiguration.MemberElementType}' to plc type '{SourceType.FullName}'.");
            }
            catch (Exception ex) when (!(ex is AdsMapperException))
            {
                throw new AdsMapperException($"Could not map the enumeration value of '{sourceValue}' to plc type '{SourceType.FullName}'.", ex);
            }
        }
    }
}
