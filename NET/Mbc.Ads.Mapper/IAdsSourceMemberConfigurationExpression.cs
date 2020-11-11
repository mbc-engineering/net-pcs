//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Source Member configuration options with the type <see cref="TwinCAT.Ads.ITcAdsSymbol5"/>
    /// </summary>
    public interface IAdsSourceMemberConfigurationExpression : IAdsAllSourceMemberConfigurationExpression
    {
        /// <summary>
        /// Gets the member symbol name of the Source (PLC) type who is mapped with destination member
        /// </summary>
        string SymbolName { get; }
    }
}
