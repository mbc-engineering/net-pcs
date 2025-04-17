using Mbc.Pcs.Net.Test.Util.Command;
using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Test
{
    public abstract class MbcPlcCommandTestBase
    {
        private readonly string _plcAmsNetId = "172.26.23.66.1.1";
        private readonly int _plcAmsPort = 851;

        public MbcPlcCommandTestBase()
        {
            // ToDo: Add configuration file for System Test flag and device parameters (AmsNet)
            bool realPlc = false;
            if (realPlc)
            {
                var adsClient = new AdsClient();
                adsClient.Connect(_plcAmsNetId, _plcAmsPort);

                AdsCommandConnectionFake.SetSystemTestConnection(adsClient);
            }
        }
    }
}
