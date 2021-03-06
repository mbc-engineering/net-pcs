﻿//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.Command
{
    public enum CommandResultCode : ushort
    {
        Initialized = 0,
        Running = 1,
        Done = 2,
        Cancelled = 3,
        StartUserDefined = 100,
    }
}
