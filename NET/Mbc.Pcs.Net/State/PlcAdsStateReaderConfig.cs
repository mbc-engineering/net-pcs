//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using Mbc.Ads.Mapper;
using System;

namespace Mbc.Pcs.Net.State
{
#pragma warning disable CA1815 // Override equals and operator equals on value types
    /// <summary>
    /// Contains the configuration of an <see cref="PlcAdsStateReader{TStatus}"/>.
    /// </summary>
    /// <typeparam name="TStatus">the type of the status</typeparam>
    public struct PlcAdsStateReaderConfig<TStatus>
        where TStatus : new()
    {
        public string VariablePath { get; set; }

        public AdsMapperConfiguration<TStatus> AdsMapperConfiguration { get; set; }

        /// <summary>
        /// Cycle time the ADS Server notifies for new plc data
        /// </summary>
        public TimeSpan CycleTime { get; set; }

        /// <summary>
        /// Repporting Intervall of the <see cref="PlcAdsStateReader{TStatus}"/>. A Reportings contains a list of data in the time of <see cref="CycleTime"/>
        /// </summary>
        public TimeSpan MaxDelay { get; set; }
    }
}
