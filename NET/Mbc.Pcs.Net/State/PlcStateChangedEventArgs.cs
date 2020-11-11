//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

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
    }
}
