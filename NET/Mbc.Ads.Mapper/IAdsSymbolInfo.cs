//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using TwinCAT.Ads.TypeSystem;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Provides information about an ADS symbol.
    /// </summary>
    public interface IAdsSymbolInfo
    {
        /// <summary>
        /// The Full path to the symbol defintion
        /// </summary>
        string SymbolPath { get; }

        /// <summary>
        /// The <see cref="IAdsSymbol"/> information from ADS.
        /// </summary>
        IAdsSymbol Symbol { get; }
    }
}
