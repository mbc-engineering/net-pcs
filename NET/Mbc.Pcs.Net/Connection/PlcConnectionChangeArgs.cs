//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Connection
{
    public class PlcConnectionChangeArgs : EventArgs
    {
        public PlcConnectionChangeArgs(bool connected, IAdsConnection connection)
        {
            Connected = connected;
            Connection = connection;
        }

        public bool Connected { get; }

        public IAdsConnection Connection { get; }
    }
}
