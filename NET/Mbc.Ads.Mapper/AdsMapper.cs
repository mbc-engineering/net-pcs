using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TwinCAT.Ads;

namespace Mbc.Ads.Mapper
{
    public class AdsMapper<TDestination>
        where TDestination : new()
    {
        /// <summary>
        /// Delegate to set a value from a source stream to a destination Object
        /// </summary>
        /// <param name="destination">Testination type to set the values</param>
        /// <param name="adsStreamReader">The source stream to read from</param>
        /// <returns>the actual value of the symbol</returns>
        internal delegate object AdsMappingStreamReaderDelegate(TDestination destination, AdsBinaryReader adsStreamReader);

        private List<AdsMappingDefinition<TDestination>> _streamMappingDefinition = new List<AdsMappingDefinition<TDestination>>();

        public TDestination MapStream(AdsStream adsStream)
        {
            TDestination filledData = ReadStream(adsStream);
            return filledData;
        }

        internal void AddStreamMapping(AdsMappingDefinition<TDestination> mappingDefinition)
        {
            _streamMappingDefinition.Add(mappingDefinition);
        }

        private TDestination ReadStream(AdsStream adsStream)
        {
            var data = new TDestination();

            var reader = new AdsBinaryReader(adsStream);
            foreach (var def in _streamMappingDefinition)
            {
                object value = def.StreamReadFunction(data, reader);
                try
                {
                    SetObjectValue(data, value, def);
                }
                catch (Exception e) when (!(e is AdsMapperException))
                {
                    throw new AdsMapperException($"Error mapping to destination '{def.DestinationMemberConfiguration.Member.Name}'.", e);
                }
            }

            return data;
        }

        private void SetObjectValue(TDestination destinationObject, object value, AdsMappingDefinition<TDestination> definition)
        {
            object valueToSet = value;

            var destinationConfig = definition.DestinationMemberConfiguration;

            switch (destinationConfig.Member.MemberType)
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo)destinationConfig.Member;

                    if (fieldInfo.FieldType.IsEnum)
                    {
                        valueToSet = definition.GetEnumValue(valueToSet);
                    }

                    if (fieldInfo.FieldType.IsArray)
                    {
                        IList array = (IList)fieldInfo.GetValue(destinationObject);
                        array[definition.ArrayIndex] = destinationConfig.Convert(valueToSet);
                        return;
                    }

                    // change to target type
                    valueToSet = Convert.ChangeType(valueToSet, fieldInfo.FieldType);

                    fieldInfo.SetValue(destinationObject, destinationConfig.Convert(valueToSet));
                    break;

                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo)destinationConfig.Member;

                    if (propertyInfo.PropertyType.IsEnum)
                    {
                        valueToSet = definition.GetEnumValue(valueToSet);
                    }

                    if (propertyInfo.PropertyType.IsArray)
                    {
                        Array array = (Array)propertyInfo.GetValue(destinationObject);

                        if (array.Rank == 1)
                        {
                            array.SetValue(destinationConfig.Convert(valueToSet), definition.ArrayIndex);
                        }
                        else if (array.Rank == 2)
                        {
                            array.SetValue(
                                destinationConfig.Convert(valueToSet),
                                definition.ArrayIndex / array.GetLength(1),
                                definition.ArrayIndex % array.GetLength(1));
                        }
                        else
                        {
                            throw new AdsMapperException("Only 1 or 2 dimensional arrays are currently supported.");
                        }

                        return;
                    }

                    // change to target type
                    valueToSet = Convert.ChangeType(valueToSet, propertyInfo.PropertyType);

                    propertyInfo.SetValue(destinationObject, destinationConfig.Convert(valueToSet));
                    break;
                default:
                    throw new AdsMapperException("IDestinationMemberConfiguration.Member of type MemberInfo must be of type FieldInfo or PropertyInfo");
            }
        }
    }
}
