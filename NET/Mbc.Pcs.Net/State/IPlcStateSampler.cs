﻿//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.State
{
    /// <summary>
    /// Provides access to sampled PLC states.
    /// </summary>
    /// <typeparam name="TState">the type of the sampled PLC data</typeparam>
    public interface IPlcStateSampler<TState>
    {
        event EventHandler<PlcStateChangedEventArgs<TState>> StateChanged;

        /// <summary>
        /// Gets the sample rate of the <see cref="StateChanged"/> event
        /// in [Hz].
        /// </summary>
        uint SampleRate { get; }

        /// <summary>
        /// Gets the state and time of the last sample as an atomic tuple.
        /// </summary>
        (DateTime TimeStamp, TState State) CurrentSample { get; }
    }
}