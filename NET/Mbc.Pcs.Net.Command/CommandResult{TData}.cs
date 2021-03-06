﻿//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// Base class for command results with generic data.
    /// </summary>
    /// <typeparam name="TData">the type of the command result data</typeparam>
    public class CommandResult<TData> : CommandResult
    {
        public CommandResult(DateTime plcTcExecutionTime, TData data)
            : base(plcTcExecutionTime)
        {
            Data = data;
        }

        public TData Data { get; }
    }
}
