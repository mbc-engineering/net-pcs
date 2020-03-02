using Mbc.Pcs.Net.State;
using System;

namespace Mbc.Pcs.Net.Test.TestUtils
{
    public class TestState : IPlcState
    {
        public DateTime PlcTimeStamp { get; set; }

        public int Foo { get; set; }
    }
}
