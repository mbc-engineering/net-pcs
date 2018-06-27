namespace Mbc.Pcs.Net.Command
{
    public enum CommandResultCode : ushort
    {
        Initialized = 0,
        Running = 1,
        Done = 2,
        Cancelled = 3,
        StartUserDefined = 100
    }
}
