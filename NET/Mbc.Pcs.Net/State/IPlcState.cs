//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.State
{
    /// <summary>
    /// Basis-Interface für alle PLC-Statusklassen.
    /// </summary>
    public interface IPlcState
    {
        /// <summary>
        /// SPS Zeitstempel der Status Daten
        /// </summary>
        DateTime PlcTimeStamp { get; set; }

        /// <summary>
        /// Güte der Status Daten
        /// </summary>
        PlcDataQuality PlcDataQuality { get; set; }
    }
}
