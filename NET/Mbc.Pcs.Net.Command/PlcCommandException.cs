//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// The exception is thrown if the execution of a <see cref="PlcCommand"/>
    /// fails for some reason.
    /// </summary>
    public class PlcCommandException : Exception
    {
        public PlcCommandException(string commandVariable)
        {
            CommandVariable = commandVariable;
        }

        public PlcCommandException(string commandVariable, string message)
            : base(message)
        {
            CommandVariable = commandVariable;
        }

        public PlcCommandException(string commandVariable, string message, Exception innerException)
            : base(message, innerException)
        {
            CommandVariable = commandVariable;
        }

        public PlcCommandException(string commandVariable, Exception innerException)
            : base(innerException.Message, innerException)
        {
            CommandVariable = commandVariable;
        }

        public string CommandVariable { get; }

        public virtual string DebugMessage
        {
            get
            {
                var message = Message;

                if (!string.IsNullOrEmpty(CommandVariable))
                {
                    message += Environment.NewLine + $"Command: {CommandVariable}";
                }

                return message;
            }
        }
    }
}
