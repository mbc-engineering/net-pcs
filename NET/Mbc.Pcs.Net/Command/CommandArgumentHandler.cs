using TwinCAT.Ads;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// Base class for handler reading or writing arguments of commands.
    /// </summary>
    public abstract class CommandArgumentHandler
    {
        public abstract void WriteInputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandInput input);

        public abstract void ReadOutputData(IAdsConnection adsConnection, string adsCommandFbPath, ICommandOutput output);
    }
}
