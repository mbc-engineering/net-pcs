//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.Alarm
{
    public class PlcAlarmChangeEventArgs
    {
        public PlcAlarmEventChangeType ChangeType { get; set; }

        public PlcAlarmEvent AlarmEvent { get; set; }
    }
}
