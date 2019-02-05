//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;

namespace Mbc.Pcs.Net.Alarm
{
    /// <summary>
    /// Hilfsfunktionen für die Sortierung von <see cref="PlcAlarmEvent"/>.
    /// </summary>
    public static class PlcAlarmEventSorter
    {
        /// <summary>
        /// Gibt einen <see cref="IComparer{T}"/> zurück der die <see cref="PlcAlarmEvent"/>
        /// nach der Default-Reihenfolge sortiert.
        /// </summary>
        public static IComparer<PlcAlarmEvent> DefaultSortOrder => new DefaultSorter();

        private class DefaultSorter : IComparer<PlcAlarmEvent>, IComparer
        {
            public int Compare(PlcAlarmEvent x, PlcAlarmEvent y)
            {
                if (x.Class > y.Class)
                {
                    return -1;
                }
                else if (x.Class < y.Class)
                {
                    return 1;
                }
                else if (x.Date > y.Date)
                {
                    return 1;
                }
                else if (x.Date < y.Date)
                {
                    return -1;
                }
                else
                {
                    return 0;
                }
            }

            public int Compare(object x, object y)
            {
                if (x is PlcAlarmEvent xAlarm && y is PlcAlarmEvent yAlarm)
                {
                    return Compare(xAlarm, yAlarm);
                }

                // fallback
                return 0;
            }
        }
    }
}
