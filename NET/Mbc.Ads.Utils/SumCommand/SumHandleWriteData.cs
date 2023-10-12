//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using System;
using System.Collections.Generic;
using System.Linq;
using TwinCAT.Ads;
using TwinCAT.Ads.SumCommand;

namespace Mbc.Ads.Utils.SumCommand
{
    /// <summary>
    /// Provides a implementation of <see cref="SumWrite"/> similar to
    /// <see cref="SumHandleWrite"/> which expecteds <see cref="ReadOnlyMemory{T}"/>
    /// as data for each handle.
    /// </summary>
    public class SumHandleWriteData : SumWrite
    {
        private readonly uint[] _handles;

        public SumHandleWriteData(IAdsConnection connection, uint[] handles)
            : base(connection, SumAccessMode.ValueByHandle)
        {
            _handles = handles.ToArray();
        }

        public AdsErrorCode TryWrite(IEnumerable<ReadOnlyMemory<byte>> data, out AdsErrorCode[] returnCodes)
        {
            var writeData = data.Select(x => x.ToArray()).ToList();
            Ensure.That(writeData.Count, nameof(data), (opt) => opt.WithMessage("Size must match handles.")).Is(_handles.Length);

            sumEntities = _handles.Zip(writeData, (h, d) => new HandleSumDataEntity(h, d.Length)).Cast<SumDataEntity>().ToList();

            // SumWrite expects data in continous memory (unlike SumRead)
            var size = writeData.Select(x => x.Length).Sum();
            var buffer = new byte[size];
            int offset = 0;
            foreach (var w in writeData)
            {
                w.CopyTo(buffer, offset);
                offset += w.Length;
            }

            return TryWriteRaw(new ReadOnlyMemory<byte>(buffer), out returnCodes);
        }

        public void Write(IEnumerable<ReadOnlyMemory<byte>> data)
        {
            TryWrite(data, out AdsErrorCode[] returnCodes);
            if (Failed)
                throw new AdsSumCommandException("SumHandleWriteData failed", this);
        }

        public void Write(ReadOnlyMemory<byte> data, IEnumerable<int> writeSizes)
        {
            sumEntities = _handles.Zip(writeSizes, (h, d) => new HandleSumDataEntity(h, d)).Cast<SumDataEntity>().ToList();

            TryWriteRaw(data, out AdsErrorCode[] returnCodes);
            if (Failed)
                throw new AdsSumCommandException("SumHandleWriteData failed", this);
        }

        protected override int OnWriteSumEntityData(SumDataEntity entity, Span<byte> writer)
        {
            /* Muss überschrieben werden, da die Basisimplementierung eine interne Klasse
             * von SumDataEntity erwartet.*/

            if (mode == SumAccessMode.ValueByHandle)
            {
                HandleSumDataEntity sumStreamEntity = (HandleSumDataEntity)entity;
                return MarshalSumWriteHeader((uint)AdsReservedIndexGroup.SymbolValueByHandle, sumStreamEntity.Handle, sumStreamEntity.WriteLength, writer);
            }

            throw new NotSupportedException($"Mode {mode} not supported.");
        }

        internal class HandleSumDataEntity : SumDataEntity
        {
            public HandleSumDataEntity(uint handle, int writeLength)
                : base(0, writeLength)
            {
                Handle = handle;
            }

            public uint Handle { get; }
        }
    }
}
