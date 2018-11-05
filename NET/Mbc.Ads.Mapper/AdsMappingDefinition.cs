using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mbc.Ads.Mapper
{
    internal class AdsMappingDefinition<TDestination>
        where TDestination : new()
    {
        internal AdsMappingDefinition()
        {
        }

        internal AdsMappingDefinition(int arrayIndex)
        {
            ArrayIndex = arrayIndex;
        }

        internal AdsMappingDefinition(IDictionary<object, string> enumvalues)
        {
            EnumValues = enumvalues;
        }

        internal AdsMappingDefinition(int arrayIndex, IDictionary<object, string> enumvalues)
        {
            ArrayIndex = arrayIndex;
            EnumValues = enumvalues;
        }

        internal AdsMapper<TDestination>.AdsMappingStreamReaderDelegate StreamReadFunction { get; set; }

        internal IDestinationMemberConfiguration DestinationMemberConfiguration { get; set; }

        internal int ArrayIndex { get; set; } = 0;

        internal IDictionary<object, string> EnumValues { get; } = new Dictionary<object, string>();

        internal object GetEnumValue(object value)
        {
            EnsureArg.IsTrue((DestinationMemberConfiguration.Member is FieldInfo fieldInfo && fieldInfo.FieldType.IsEnum) || (DestinationMemberConfiguration.Member is PropertyInfo propInfo && propInfo.PropertyType.IsEnum));

            var targetEnumType = (DestinationMemberConfiguration.Member as FieldInfo)?.FieldType ?? (DestinationMemberConfiguration.Member as PropertyInfo)?.PropertyType;

            try
            {
                long numericEnumValueToSet = Convert.ToInt64(value);

                var enumKey = EnumValues.Keys.FirstOrDefault(x => Convert.ToInt64(x) == numericEnumValueToSet);

                string plcEnumName;
                if (enumKey != null)
                {
                    plcEnumName = EnumValues[enumKey];
                }
                else
                {
                    // TODO Enum-Dict ist nicht sortiert, daher ist First() falsch
                    // TODO Leeres EnumValue berücksichtigen
                    // Achtung: Wenn Enum kein 0 Value definiert hat, und value = 0 ist, muss der 1. Enum verwendet werden als Default
                    plcEnumName = EnumValues.First().Value;
                }

                return Enum.Parse(targetEnumType, plcEnumName.TrimStart('e'), true);
            }
            catch (Exception ex)
            {
                throw new AdsMapperException($"Could not parse the Enumeration value of '{value}' to '{targetEnumType.ToString()}'", ex);
            }
        }
    }
}
