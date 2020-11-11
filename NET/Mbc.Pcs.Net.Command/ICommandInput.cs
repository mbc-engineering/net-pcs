//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// Provides input data for a <see cref="PlcCommand"/>:
    /// </summary>
    public interface ICommandInput
    {
        /// <summary>
        /// Returns the command input data.
        /// </summary>
        IDictionary<string, object> GetInputData();
    }
}
