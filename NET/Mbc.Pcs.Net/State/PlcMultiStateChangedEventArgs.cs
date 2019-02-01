//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System.Collections.Generic;

namespace Mbc.Pcs.Net.State
{
    /// <summary>
    /// Event-Argument für mehrere State-Change Events des <see cref="IPlcStateSampler{TState}"/>.
    /// </summary>
    /// <typeparam name="TState">der Typ des Events</typeparam>
    public class PlcMultiStateChangedEventArgs<TState>
        where TState : IPlcState
    {
        private readonly TState[] _samples;

        public PlcMultiStateChangedEventArgs(List<TState> samples)
        {
            _samples = samples.ToArray();
        }

        /// <summary>
        /// Der aktuelleste (letzte) Status Daten der PLC.
        /// </summary>
        public TState State => _samples[_samples.Length - 1];

        public IEnumerable<TState> States => _samples;
    }
}
