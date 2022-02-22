//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.Command
{
    /// <summary>
    /// Detail configuration of <see cref="PlcCommand"/>.
    /// </summary>
    public class PlcCommandConfiguration
    {
        public static PlcCommandConfiguration CreateDefaultOnChange()
            => new PlcCommandConfiguration
            {
                UseCyclicNotifications = false,
                OnChangeCycleTime = TimeSpan.FromMilliseconds(50),
                OnChangeMaxDelay = TimeSpan.Zero,
                MaxWaitForInitialEvent = TimeSpan.FromSeconds(1),
                MaxRetriesForInitialEvent = 3,
            };

        public static PlcCommandConfiguration CreateDefaultCyclic()
            => new PlcCommandConfiguration
            {
                UseCyclicNotifications = true,
                OnChangeCycleTime = TimeSpan.FromMilliseconds(50),
                OnChangeMaxDelay = TimeSpan.Zero,
            };

        /// <summary>
        /// If <c>true</c> cyclic notifications are used instead of
        /// on change.
        /// </summary>
        public bool UseCyclicNotifications { get; set; }

        /// <summary>
        /// Parameter <c>cycleTime</c> for registration of an ADS device
        /// notification for command callbacks. Default: 50ms.
        /// </summary>
        public TimeSpan OnChangeCycleTime { get; set; } = TimeSpan.FromMilliseconds(50);

        /// <summary>
        /// Parameter <c>maxDelay</c> for registration of an ADS device
        /// notification for command callbacks. Default: 0.
        /// </summary>
        public TimeSpan OnChangeMaxDelay { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Max. wait time for initial event after registering a on-change
        /// Ads device notification. Default: 1s.
        /// </summary>
        public TimeSpan MaxWaitForInitialEvent { get; set; } = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Max. number of retrying getting an initial event. Default: 3.
        /// </summary>
        public int MaxRetriesForInitialEvent { get; set; } = 3;
    }
}
