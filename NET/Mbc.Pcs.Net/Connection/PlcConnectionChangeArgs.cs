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
