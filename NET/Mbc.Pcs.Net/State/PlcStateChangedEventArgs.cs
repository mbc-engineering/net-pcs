//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.State
{
    public class PlcStateChangedEventArgs<TState>
    {
        public PlcStateChangedEventArgs(TState status, DateTime plcTimeStamp)
        {
            Status = status;
            PlcTimeStamp = plcTimeStamp;
        }

        /// <summary>
        /// Status Daten der PLC
        /// </summary>
        public TState Status { get; }

        /// <summary>
        /// PLC Time Stamp zu welchem Zeitpunkt die erhaltenen Status daten generiert wurden
        /// </summary>
        public DateTime PlcTimeStamp { get;  }
    }
}
