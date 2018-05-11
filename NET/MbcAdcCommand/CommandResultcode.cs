namespace MbcAdcCommand
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
