//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

namespace Mbc.Pcs.Net.Alarm
{
    public abstract class PlcAlarmEventFormatterBase : IPlcAlarmEventFormatter
    {
        public PlcAlarmEventFormatterBase()
        {
        }

        public PlcAlarmEventFormatterBase(IPlcAlarmEventFormatter nextFormater)
        {
            NextFormater = nextFormater;
        }

        public IPlcAlarmEventFormatter NextFormater { get; protected set; } = null;

        public virtual PlcAlarmEvent Format(PlcAlarmEvent plcAlarmEvent)
        {
            return OnChainFormatter(plcAlarmEvent);
        }

        protected virtual PlcAlarmEvent OnChainFormatter(PlcAlarmEvent plcAlarmEvent)
        {
            if (NextFormater != null)
            {
                return NextFormater.Format(plcAlarmEvent);
            }

            return plcAlarmEvent;
        }
    }
}
