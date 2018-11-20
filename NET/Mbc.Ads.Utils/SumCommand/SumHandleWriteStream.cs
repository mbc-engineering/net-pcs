//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
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
    /// <see cref="SumHandleWrite"/> which expecteds <see cref="AdsStream"/>
    /// as data for each handle.
    /// </summary>
    public class SumHandleWriteStream : SumWrite
    {
        private readonly uint[] _handles;

        public SumHandleWriteStream(IAdsConnection connection, uint[] handles)
            : base(connection, SumAccessMode.ValueByHandle)
        {
            _handles = handles.ToArray();
        }

        public AdsErrorCode TryWrite(IEnumerable<AdsStream> streams, out AdsErrorCode[] returnCodes)
        {
            var writeData = streams.Select(x => x.ToArray()).ToList();
            Ensure.That(writeData.Count, nameof(streams), (opt) => opt.WithMessage("Size must match handles.")).Is(_handles.Length);

            sumEntities = new List<SumDataEntity>();
            foreach (var entity in _handles.Zip(writeData, (h, d) => new HandleSumStreamEntity(h, d.Length)))
            {
                sumEntities.Add(entity);
            }

            return TryWriteRaw(writeData, out returnCodes);
        }

        public void Write(IEnumerable<AdsStream> streams)
        {
            TryWrite(streams, out AdsErrorCode[] returnCodes);
            if (Failed)
                throw new AdsSumCommandException("SumHandleWriteStream failed", this);
        }

        protected override int OnWriteSumEntityData(SumDataEntity entity, AdsBinaryWriter writer)
        {
            /* Muss überschrieben werden, da die Basisimplementierung eine interne Klasse
             * von SumDataEntity erwartet.*/

            if (mode == SumAccessMode.ValueByHandle)
            {
                HandleSumStreamEntity sumStreamEntity = (HandleSumStreamEntity)entity;
                return MarshalSumWriteHeader((uint)AdsReservedIndexGroups.SymbolValueByHandle, sumStreamEntity.Handle, sumStreamEntity.WriteLength, writer);
            }

            throw new NotSupportedException($"Mode {mode} not supported.");
        }
    }

#pragma warning disable SA1402 // File may only contain a single type
    internal class HandleSumStreamEntity : SumDataEntity
    {
        public HandleSumStreamEntity(uint handle, int writeLength)
            : base(0, writeLength)
        {
            Handle = handle;
        }

        public uint Handle { get; }
    }
#pragma warning restore SA1402 // File may only contain a single type
}
