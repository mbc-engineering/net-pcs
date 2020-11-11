//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.Alarm
{
    public class DataEventArgs
    {
        public DataEventArgs(string data)
        {
            Data = data;
        }

        public string Data { get; set; }
    }
}
