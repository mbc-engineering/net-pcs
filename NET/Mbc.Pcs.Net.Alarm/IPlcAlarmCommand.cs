namespace Mbc.Pcs.Net.Alarm
{
    public interface IPlcAlarmCommand
    {
        /// <summary>
        /// Plc Kommando für "ResetAlarmsCommand".
        /// </summary>
        bool ResetAlarms();
    }
}
