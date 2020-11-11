//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using TwinCAT.Ads;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Provides information about an ADS symbol.
    /// </summary>
    public interface IAdsSymbolInfo
    {
        /// <summary>
        /// The symbol size in byte.
        /// </summary>
        int SymbolsSize { get; }

        /// <summary>
        /// The Full path to the symbol defintion
        /// </summary>
        string SymbolPath { get; }

        /// <summary>
        /// The <see cref="ITcAdsSymbol5"/> information from ADS.
        /// </summary>
        ITcAdsSymbol5 Symbol { get; }
    }
}
