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
    /// Provides a implementation of <see cref="SumWrite"/> similar to
    /// <see cref="SumHandleWrite"/> which expecteds <see cref="AdsStream"/>
    /// as data for each handle.
    /// </summary>
    [Obsolete("AdsStream is deprecated")]
    public class SumHandleWriteStream : SumWrite
    {
        private readonly uint[] _handles;

        public SumHandleWriteStream(IAdsConnection connection, uint[] handles)
            : base(connection, SumAccessMode.ValueByHandle)
        {
            _handles = handles.ToArray();
        }

        [Obsolete("AdsStream is deprecated")]
        public AdsErrorCode TryWrite(IEnumerable<AdsStream> streams, out AdsErrorCode[] returnCodes)
        {
            var writeData = streams.Select(x => x.ToArray()).ToList();

            if (writeData.Count != _handles.Length)
            {
                throw new ArgumentException($"Size must match handles. Value '{writeData.Count}' is not '{_handles.Length}'.", nameof(streams));
            }

            sumEntities = new List<SumDataEntity>();
            foreach (var entity in _handles.Zip(writeData, (h, d) => new HandleSumStreamEntity(h, d.Length)))
            {
                sumEntities.Add(entity);
            }

            return TryWriteRaw(new ReadOnlyMemory<byte>(writeData.SelectMany(x => x).ToArray()), out returnCodes);
        }

        [Obsolete("AdsStream is deprecated")]
        public void Write(IEnumerable<AdsStream> streams)
        {
            TryWrite(streams, out AdsErrorCode[] returnCodes);
            if (Failed)
                throw new AdsSumCommandException("SumHandleWriteStream failed", this);
        }

        protected override int OnWriteSumEntityData(SumDataEntity entity, Span<byte> writer)
        {
            /* Muss überschrieben werden, da die Basisimplementierung eine interne Klasse
             * von SumDataEntity erwartet.*/

            if (mode == SumAccessMode.ValueByHandle)
            {
                HandleSumStreamEntity sumStreamEntity = (HandleSumStreamEntity)entity;
                return MarshalSumWriteHeader((uint)AdsReservedIndexGroup.SymbolValueByHandle, sumStreamEntity.Handle, sumStreamEntity.WriteLength, writer);
            }

            throw new NotSupportedException($"Mode {mode} not supported.");
        }

        internal class HandleSumStreamEntity : SumDataEntity
        {
            public HandleSumStreamEntity(uint handle, int writeLength)
                : base(0, writeLength)
            {
                Handle = handle;
            }

            public uint Handle { get; }
        }
    }
}
