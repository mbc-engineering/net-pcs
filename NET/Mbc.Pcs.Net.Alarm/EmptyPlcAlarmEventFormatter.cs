//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.Alarm
{
    public class EmptyPlcAlarmEventFormatter : PlcAlarmEventFormatterBase
    {
        public EmptyPlcAlarmEventFormatter()
        {
        }

        public EmptyPlcAlarmEventFormatter(IPlcAlarmEventFormatter nextFormater)
            : base(nextFormater)
        {
        }

        public override PlcAlarmEvent Format(PlcAlarmEvent plcAlarmEvent)
        {
            return base.Format(plcAlarmEvent);
        }
    }
}
