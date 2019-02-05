//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
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
        DateTime PlcTimeStamp { get; set; }
    }
}
