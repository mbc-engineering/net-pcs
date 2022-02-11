//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
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
    /// <see cref="SumHandleRead"/> which provides <see cref="ReadOnlyMemory{T}"/>
    /// as data for each handle.
    /// </summary>
    public class SumHandleReadData : SumRead
    {
        private readonly uint[] _handles;
        private readonly int[] _readSize;

        public SumHandleReadData(IAdsConnection connection, uint[] handles, int[] readSize)
            : base(connection, SumAccessMode.ValueByHandle)
        {
            _handles = handles;
            _readSize = readSize;
        }

        public AdsErrorCode TryRead(out IList<ReadOnlyMemory<byte>> data, out AdsErrorCode[] returnCodes)
        {
            sumEntities = _handles.Zip(_readSize, (h, rs) => new HandleSumDataEntity(h, rs)).ToArray();

            return TryReadRaw(out data, out returnCodes);
        }

        public IList<ReadOnlyMemory<byte>> Read()
        {
            TryRead(out IList<ReadOnlyMemory<byte>> data, out AdsErrorCode[] returnCodes);
            if (Failed)
                throw new AdsSumCommandException("SumHandleRead failed.", this);

            return data;
        }

        protected override int OnWriteSumEntityData(SumDataEntity entity, Span<byte> writer)
        {
            /* Muss überschrieben werden, da die Basisimplementierung eine interne Klasse
             * von HandleSumEntity erwartet und diese internal declariert ist. */

            if (mode == SumAccessMode.ValueByHandle)
            {
                HandleSumDataEntity sumStreamEntity = (HandleSumDataEntity)entity;
                return MarshalSumReadHeader((uint)AdsReservedIndexGroup.SymbolValueByHandle, sumStreamEntity.Handle, sumStreamEntity.ReadLength, writer);
            }

            throw new NotSupportedException($"Mode {mode} not supported.");
        }

        internal class HandleSumDataEntity : SumDataEntity
        {
            public HandleSumDataEntity(uint handle, int readLength)
                : base(readLength, 0)
            {
                Handle = handle;
            }

            public uint Handle { get; }
        }
    }
}
