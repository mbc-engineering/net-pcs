//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.Alarm
{
    public interface IPlcAlarmEventFormatter
    {
        PlcAlarmEvent Format(PlcAlarmEvent plcAlarmEvent);
    }
}
