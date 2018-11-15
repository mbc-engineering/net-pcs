using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mbc.Ads.Mapper
{
    internal static class DataObjectAccessor
    {
        /// <summary>
        /// Returns a function which sets a primitive value to an object.
        /// </summary>
        internal static Action<TDataObject, object> CreateValueSetter<TDataObject>(IDestinationMemberConfiguration destinationMemberConfiguration, IDictionary<object, string> enumValues = null, int arrayIndex = -1)
        {
            return (dataObject, value) =>
            {
                var type = destinationMemberConfiguration.Member.GetSettableDataType();
                if (type.IsArray && arrayIndex >= 0)
                {
                    type = type.GetElementType();
                }

                object valueToSet;
                if (destinationMemberConfiguration.HasConverter)
                {
                    valueToSet = destinationMemberConfiguration.Convert(value);
                }
                else
                {
                    if (enumValues != null)
                    {
                        valueToSet = ConvertEnumValueFromPlc(value, type, enumValues);
                    }
                    else
                    {
                        valueToSet = Convert.ChangeType(value, type);
                    }
                }

                if (arrayIndex < 0)
                {
                    destinationMemberConfiguration.Member.SetValue(dataObject, valueToSet);
                }
                else
                {
                    Array array = (Array)destinationMemberConfiguration.Member.GetValue(dataObject);
                    if (array.Rank == 1)
                    {
                        array.SetValue(valueToSet, arrayIndex);
                    }
                    else if (array.Rank == 2)
                    {
                        array.SetValue(
                            valueToSet,
                            arrayIndex / array.GetLength(1),
                            arrayIndex % array.GetLength(1));
                    }
                    else
                    {
                        throw new AdsMapperException("Only 1 or 2 dimensional arrays are currently supported.");
                    }
                }
            };
        }

        /// <summary>
        /// Gets the .NET enum value of an PLC enumeration.
        /// </summary>
        /// <param name="sourceValue">the PLC enumeration value.</param>
        /// <param name="targetType">the .NET target type.</param>
        /// <param name="plcEnumValues">the PLC enumeration values.</param>
        /// <returns>A .NET enumeration value</returns>
        private static object ConvertEnumValueFromPlc(object sourceValue, Type targetType, IDictionary<object, string> plcEnumValues)
        {
            try
            {
                long numericEnumValueToSet = Convert.ToInt64(sourceValue);

                var enumKey = plcEnumValues.Keys.FirstOrDefault(x => Convert.ToInt64(x) == numericEnumValueToSet);

                string plcEnumName;
                if (enumKey != null)
                {
                    plcEnumName = plcEnumValues[enumKey];
                }
                else
                {
                    // TODO Enum-Dict ist nicht sortiert, daher ist First() falsch
                    // TODO Leeres EnumValue berücksichtigen
                    // Achtung: Wenn Enum kein 0 Value definiert hat, und value = 0 ist, muss der 1. Enum verwendet werden als Default
                    plcEnumName = plcEnumValues.First().Value;
                }

                // TODO konfiguriererbares Mapping (Prefix e)
                return Enum.Parse(targetType, plcEnumName.TrimStart('e'), true);
            }
            catch (Exception ex)
            {
                throw new AdsMapperException($"Could not parse the Enumeration value of '{sourceValue}' to '{targetType}'", ex);
            }
        }

    }
}
