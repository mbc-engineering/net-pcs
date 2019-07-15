//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// Base class for command results without data.
    /// </summary>
    public class CommandResult
    {
        public CommandResult(DateTime plcTcExecutionTime)
        {
            PlcTcExecutionTime = plcTcExecutionTime;
        }

        public DateTime PlcTcExecutionTime { get; }
    }
}
