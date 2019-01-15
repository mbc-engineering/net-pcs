//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Mbc.Pcs.Net.Alarm
{
    /// <summary>
    /// Stellt Zugriffe auf die PLC-Alarme zur Verfügung.
    /// </summary>
    public interface IPlcAlarmService
    {
        event EventHandler<PlcAlarmChangeEventArgs> AlarmChanged;

        event EventHandler<PlcAlarmChangeEventArgs> AlarmChangedWithInitialization;

        event EventHandler<DataEventArgs> Error;

        /// <summary>
        /// Liefert zurück, ob der Service mit der PLC
        /// verbunden ist.
        /// </summary>
        bool IsConnected { get; }

        void Connect();

        void Disconnect();

        /// <summary>
        /// Liefert alle aktiven Alarme zurück.
        /// </summary>
        List<PlcAlarmEvent> GetActiveAlarms();
    }
}
