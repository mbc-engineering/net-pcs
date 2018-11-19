//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;

namespace Mbc.Ads.Utils.SumCommand
{
    /// <summary>
    /// Provides a implementation of <see cref="SumRead"/> similar to
    /// <see cref="SumHandleRead"/> which provides <see cref="AdsStream"/>
    /// as data for each handle.
    /// </summary>
    public class SumHandleReadStream : SumRead
    {
        private readonly uint[] _handles;
        private readonly int[] _readSize;

        public SumHandleReadStream(IAdsConnection connection, uint[] handles, int[] readSize)
            : base(connection, SumAccessMode.ValueByHandle)
        {
            _handles = handles.ToArray();
            _readSize = readSize;
        }

        public AdsErrorCode TryRead(out IList<AdsStream> streams, out AdsErrorCode[] returnCodes)
        {
            sumEntities = new List<SumDataEntity>();
            foreach (var entity in _handles.Zip(_readSize, (h, rs) => new HandleSumStreamEntity(h, rs)))
            {
                sumEntities.Add(entity);
            }


            var adsErrorCode = TryReadRaw(out IList<byte[]> readData, out returnCodes);
            if (adsErrorCode == AdsErrorCode.NoError)
            {
                streams = readData.Select(x => new AdsStream(x)).ToList();
            }
            else
            {
                streams = new List<AdsStream>();
            }

            return adsErrorCode;
        }

        public IList<AdsStream> Read()
        {
            TryRead(out IList<AdsStream> streams, out AdsErrorCode[] returnCodes);
            if (Failed)
                throw new AdsSumCommandException("SumHandleRead failed.", this);
            return streams;
        }

        protected override int OnWriteSumEntityData(SumDataEntity entity, AdsBinaryWriter writer)
        {
            /* Muss überschrieben werden, da die Basisimplementierung eine interne Klasse
             * von SumDataEntity erwartet.*/

            if (mode == SumAccessMode.ValueByHandle)
            {
                HandleSumStreamEntity sumStreamEntity = (HandleSumStreamEntity)entity;
                return MarshalSumReadHeader((uint)AdsReservedIndexGroups.SymbolValueByHandle, sumStreamEntity.Handle, sumStreamEntity.ReadLength, writer);
            }
            throw new NotSupportedException($"Mode {mode} not supported.");

        }

        internal class HandleSumStreamEntity : SumDataEntity
        {
            public HandleSumStreamEntity(uint handle, int readLength)
                : base(readLength, 0)
            {
                Handle = handle;
            }

            public uint Handle { get; }
        }
    }
}
