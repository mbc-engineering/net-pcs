using FakeItEasy;
using Mbc.Pcs.Net.Connection;
using Mbc.Pcs.Net.State;
using System;

namespace Mbc.Pcs.Net.Test.State
{
    public class PlcAdsStateReaderTest
    {
        private readonly IPlcAdsConnectionService _adsConnection;
        private readonly PlcAdsStateReaderConfig<StateObject> _plcAdsStateReaderConfig;

        public PlcAdsStateReaderTest()
        {
            _adsConnection = A.Fake<IPlcAdsConnectionService>();
            _plcAdsStateReaderConfig = new PlcAdsStateReaderConfig<StateObject>()
            {
                CycleTime = TimeSpan.FromMilliseconds(1),
            };
        }

        private class StateObject
        {
            public int Value1 { get; set; }
        }
    }
}
