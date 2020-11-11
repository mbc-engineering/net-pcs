//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net
{
    /// <summary>
    /// Interface that provide a Heart beat event and also a died event
    /// </summary>
    public interface IHeartBeat
    {
        /// <summary>
        /// The intervall time the heart will beat
        /// </summary>
        TimeSpan HeartBeatInterval { get; set; }

        /// <summary>
        /// The time for missing feedback until die
        /// </summary>
        TimeSpan TimeUntilDie { get; set; }

        /// <summary>
        /// The beat of the heart
        /// </summary>
        event EventHandler<HeartBeatEventArgs> HeartBeats;

        /// <summary>
        /// Hert beat has exposed
        /// </summary>
        event EventHandler<HeartBeatDiedEventArgs> HeartDied;
    }
}
