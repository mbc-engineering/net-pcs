//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.Test.Util.Command
{
    public enum PlcCommandFakeOption
    {
        /// <summary>
        /// A faked plc answert will returned and the defined status Code wil be set
        /// </summary>
        ResponseImmediatelyFinished,

        /// <summary>
        /// After 200ms a faked plc answert will returned and the defined status Code wil be set
        /// </summary>
        ResponseDelayedFinished,

        /// <summary>
        /// After 200ms a simulated SPS Cancel wil be returned.
        /// </summary>
        ResponseDelayedCancel,

        /// <summary>
        /// The defined Fb Path is on the PLC not implemented, return Symbol error.
        /// </summary>
        ResponseFbPathNotExist,

        /// <summary>
        /// The .Net PlcCommand will regular abort after TimeOut
        /// </summary>
        NoResponse,
    }
}
