﻿//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// The exception is thrown if the execution of a <see cref="PlcCommand"/>
    /// results in an error.
    /// </summary>
    public class PlcCommandErrorException : PlcCommandException
    {
        public PlcCommandErrorException(string commandVariable, ushort resultCode, string message)
            : base(commandVariable, message)
        {
            ResultCode = resultCode;
        }

        public ushort ResultCode { get; }

        public override string DebugMessage
        {
            get
            {
                var message = base.DebugMessage;
                if (!string.IsNullOrEmpty(CommandVariable))
                {
                    message += Environment.NewLine + $"Result Code: {ResultCode}";
                }

                return message;
            }
        }
    }
}
