//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net
{
    public class HeartBeatDiedEventArgs
    {
        public DateTime LastHeartBeat { get; set; }

        public DateTime DiedTime { get; set; }
    }
}
