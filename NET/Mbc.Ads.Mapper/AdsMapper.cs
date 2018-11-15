//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using TwinCAT.Ads;

namespace Mbc.Ads.Mapper
{
    public class AdsMapper<TDataObject>
        where TDataObject : new()
    {
        private List<AdsMappingDefinition<TDataObject>> _streamMappingDefinition = new List<AdsMappingDefinition<TDataObject>>();
        private readonly int _size;

        internal AdsMapper(int size)
        {
            _size = size;
        }

        internal void AddStreamMapping(AdsMappingDefinition<TDataObject> mappingDefinition)
        {
            _streamMappingDefinition.Add(mappingDefinition);
        }

        public TDataObject MapStream(AdsStream adsStream)
        {
            TDataObject filledData = ReadStream(adsStream);
            return filledData;
        }

        public AdsStream MapDataObject(TDataObject dataObject)
        {
            var adsStream = new AdsStream(_size);
            var writer = new AdsBinaryWriter(adsStream);
            foreach (var def in _streamMappingDefinition)
            {
                try
                {
                    object value = def.DataObjectValueGetter(dataObject);
                    object convertedValue = def.ConvertFromDestinationToSource(value);
                    def.StreamWriterFunction(writer, convertedValue);
                }
                catch (Exception e) when (!(e is AdsMapperException))
                {
                    throw new AdsMapperException($"Error mapping from source '{def.DestinationMemberConfiguration.Member.Name}'.", e);
                }
            }
            writer.Flush();

            return adsStream;
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
                    object convertedValue = def.ConvertFromSourceToDestination(value);
                    def.DataObjectValueSetter(dataObject, convertedValue);
                }
                catch (Exception e) when (!(e is AdsMapperException))
                {
                    throw new AdsMapperException($"Error mapping to destination '{def.DestinationMemberConfiguration.Member.Name}'.", e);
                }
            }

            return dataObject;
        }
    }
}
