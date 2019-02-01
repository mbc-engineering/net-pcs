using System;

namespace Mbc.Pcs.Net.State
{
    /// <summary>
    /// Basis-Interface für alle PLC-Statusklassen.
    /// </summary>
    public interface IPlcState
    {
        DateTime PlcTimeStamp { get; set; }
    }
}
