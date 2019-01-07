//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.Alarm
{
    /// <summary>
    /// Beschreibt die Änderung des <see cref="PlcAlarmEvent"/>.
    /// </summary>
    public enum PlcAlarmEventChangeType
    {
        New,
        Confirm,
        Reset,
        Signal,
        Clear,
    }
}
