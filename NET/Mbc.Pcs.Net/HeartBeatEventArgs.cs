//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net
{
    public class HeartBeatEventArgs
    {
        public HeartBeatEventArgs(DateTime beatTime)
        {
            BeatTime = beatTime;
        }

        public DateTime BeatTime { get; }
    }
}
