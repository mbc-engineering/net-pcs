//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Mbc.Pcs.Net.State
{
    /// <summary>
    /// Event-Argument für mehrere State-Change Events des <see cref="IPlcStateSampler{TState}"/>.
    /// </summary>
    /// <typeparam name="TState">der Typ des Events</typeparam>
    public class PlcMultiStateChangedEventArgs<TState>
    {
        private readonly (DateTime timestamp, TState state)[] _samples;

        public PlcMultiStateChangedEventArgs(List<(DateTime timestamp, TState state)> samples)
        {
            _samples = samples.ToArray();
        }

        /// <summary>
        /// Der aktuelleste (letzte) Status Daten der PLC.
        /// </summary>
        public TState Status => _samples[_samples.Length - 1].state;

        /// <summary>
        /// Der aktuellste (letzte) Zeitstempel der PLC.
        /// </summary>
        public DateTime PlcTimeStamp => _samples[_samples.Length - 1].timestamp;

        public IEnumerable<(DateTime timestamp, TState state)> Samples => _samples;

        public IEnumerable<TState> States => _samples.Select(x => x.state);
    }
}
