//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection;
using TwinCAT.Ads;

namespace Mbc.Ads.Mapper
{
    public class AdsMapper<TDataObject>
        where TDataObject : new()
    {

        private List<AdsMappingDefinition<TDataObject>> _streamMappingDefinition = new List<AdsMappingDefinition<TDataObject>>();

        public TDataObject MapStream(AdsStream adsStream)
        {
            TDataObject filledData = ReadStream(adsStream);
            return filledData;
        }

        public AdsStream MapDataObject(TDataObject dataObject)
        {
            var adsStream = new AdsStream();
            var writer = new AdsBinaryWriter(adsStream);
            foreach (var def in _streamMappingDefinition)
            {
                try
                {
                    var value = GetObjectValue(dataObject, def);
                    def.StreamWriterFunction(writer, value);
                }
                catch (Exception e) when (!(e is AdsMapperException))
                {
                    throw new AdsMapperException($"Error mapping from source '{def.DestinationMemberConfiguration.Member.Name}'.", e);
                }
            }
            writer.Flush();

            return adsStream;
        }

        internal void AddStreamMapping(AdsMappingDefinition<TDataObject> mappingDefinition)
        {
            _streamMappingDefinition.Add(mappingDefinition);
        }

        private TDataObject ReadStream(AdsStream adsStream)
        {
            var dataObject = new TDataObject();

            var reader = new AdsBinaryReader(adsStream);
            foreach (var def in _streamMappingDefinition)
            {
                try
                {
                    object value = def.StreamReadFunction(reader);
                    def.DataObjectValueSetter(dataObject, value);
                }
                catch (Exception e) when (!(e is AdsMapperException))
                {
                    throw new AdsMapperException($"Error mapping to destination '{def.DestinationMemberConfiguration.Member.Name}'.", e);
                }
            }

            return dataObject;
        }


        private object GetObjectValue(TDataObject sourceObject, AdsMappingDefinition<TDataObject> definition)
        {
            var destinationConfig = definition.DestinationMemberConfiguration;

            switch (destinationConfig.Member.MemberType)
            {
                case MemberTypes.Field:
                    var fieldInfo = (FieldInfo)destinationConfig.Member;

                    // TODO enum + array

                    return fieldInfo.GetValue(sourceObject);

                case MemberTypes.Property:
                    var propertyInfo = (PropertyInfo)destinationConfig.Member;

                    // TODO enum + array

                    return propertyInfo.GetValue(sourceObject);

                default:
                    throw new AdsMapperException("IDestinationMemberConfiguration.Member of type MemberInfo must be of type FieldInfo or PropertyInfo");
            }
        }
    }
}
