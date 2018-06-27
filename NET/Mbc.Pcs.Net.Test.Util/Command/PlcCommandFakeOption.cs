namespace Mbc.Pcs.Net.Test.Util.Command
{
    public enum PlcCommandFakeOption
    {
        /// <summary>
        /// A faked plc answert will returned and the defined status Code wil be set
        /// </summary>
        ResponseImmediatelyFinished,
        /// <summary>
        /// After 200ms a simulated SPS Cancel wil be returned.
        /// </summary>
        ResponseDelayedCancel,
        /// <summary>
        /// The .Net PlcCommand will regular abort after TimeOut
        /// </summary>
        NoResponse,
    }
}
