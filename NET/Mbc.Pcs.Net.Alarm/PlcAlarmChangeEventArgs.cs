//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
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
