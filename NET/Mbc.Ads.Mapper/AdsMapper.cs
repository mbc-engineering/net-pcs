//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Mbc.Ads.Mapper
{
    public class AdsMapper<TDataObject>
        where TDataObject : new()
    {
        private readonly int _size;
        private readonly List<AdsMappingDefinition<TDataObject>> _streamMappingDefinition = new List<AdsMappingDefinition<TDataObject>>();

        internal AdsMapper(int size)
        {
            _size = size;
        }

        internal void AddStreamMapping(AdsMappingDefinition<TDataObject> mappingDefinition)
        {
            _streamMappingDefinition.Add(mappingDefinition);
        }

        public TDataObject MapData(ReadOnlySpan<byte> buffer)
        {
            var dataObject = new TDataObject();

            foreach (var def in _streamMappingDefinition)
            {
                try
                {
                    object value = def.AdsDataReader.Read(buffer);
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

        public void MapDataObject(Span<byte> buffer, TDataObject dataObject)
        {
            foreach (var def in _streamMappingDefinition)
            {
                try
                {
                    object value = def.DataObjectValueGetter(dataObject);
                    object convertedValue = def.ConvertFromDestinationToSource(value);
                    def.AdsDataWriter.Write(convertedValue, buffer);
                }
                catch (Exception e) when (!(e is AdsMapperException))
                {
                    throw new AdsMapperException($"Error mapping from source '{def.DestinationMemberConfiguration.Member.Name}'.", e);
                }
            }
        }

        public ReadOnlyMemory<byte> MapDataObject(TDataObject dataObject)
        {
            var buffer = new byte[_size];
            MapDataObject(buffer, dataObject);
            return buffer;
        }
    }
}
