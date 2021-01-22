//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.State
{
    public enum PlcDataQuality
    {
        /// <summary>
        /// Connection is lost
        /// </summary>
        Lost,

        /// <summary>
        /// Data are misssing
        /// </summary>
        Skipped,

        /// <summary>
        /// Regular
        /// </summary>
        Good,
    }
}
