//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// The exception is thrown if the execution of a <see cref="PlcCommand"/>
    /// timed out.
    /// </summary>
    public class PlcCommandTimeoutException : PlcCommandException
    {
        public PlcCommandTimeoutException(string commandVariable, string message)
            : base(commandVariable, message)
        {
        }

        public PlcCommandTimeoutException(string commandVariable, string message, Exception innerException)
            : base(commandVariable, message, innerException)
        {
        }
    }
}
