//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.State
{
    public class PlcStateChangedEventArgs<TState>
        where TState : IPlcState
    {
        public PlcStateChangedEventArgs(TState status)
        {
            Status = status;
        }

        /// <summary>
        /// Status Daten der PLC
        /// </summary>
        public TState Status { get; }

        /// <summary>
        /// PLC Time Stamp zu welchem Zeitpunkt die erhaltenen Status daten generiert wurden
        /// </summary>
        [Obsolete("Use Status.Timestamp")]
        public DateTime PlcTimeStamp => Status.PlcTimeStamp;
    }
}
