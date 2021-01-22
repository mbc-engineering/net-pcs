//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.Alarm
{
    [Flags]
    public enum AlarmUserFlags
    {
        None = 0,

        /// <summary>
        /// Message-Argumente enthält Seriennummer nach dem letzten Argument
        /// </summary>
        IncludeSerialNo = 0x1,
    }
}
