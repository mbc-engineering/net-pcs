//-----------------------------------------------------------------------------
// Copyright (c) 2018 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using System;

namespace Mbc.Pcs.Net.Alarm
{
    /// <summary>
    /// Enthält Daten eines PLC-Alarmevents.
    /// </summary>
    public class PlcAlarmEvent : IEquatable<PlcAlarmEvent>
    {
        /// <summary>
        /// Die ID eines Events (eindeutig innerhalb der <see cref="SrcId"/>.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Die Quellen-ID eines Events.
        /// </summary>
        public long SrcId { get; set; }

        /// <summary>
        /// Die Klasse eines Events.
        /// </summary>
        /// <seealso cref="AlarmEventClass"/>
        public AlarmEventClass Class { get; set; }

        /// <summary>
        /// Die Priorität eines Events.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// Der lokalisierbare Name der Quelle <see cref="SrcId"/>. Wenn
        /// kein Name existiert wird ein Leerstring zurückgegeben.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// Der Zeitpunkt, wenn das Event aufgetreten ist.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Die lokalisierbare Meldung des Events mit aufgelösten Platzhaltern.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Der Zustand des Events
        /// </summary>
        /// <seealso cref="AlarmEventStates"/>
        public AlarmEventStates State { get; set; }

        /// <summary>
        /// Die Event-Flags.
        /// </summary>
        /// <seealso cref="AlarmEventFlags"/>
        public AlarmEventFlags Flags { get; set; }

        /// <summary>
        /// Definiert ob ein Alarm bestätigt werden muss.
        /// </summary>
        public bool MustConfirm { get; set; }

        /// <summary>
        /// Der Zeitpunkt, wenn das Event bestätigt wurde.
        /// </summary>
        public DateTime DateConfirmed { get; set; }

        /// <summary>
        /// Der Zeitpunkt, wenn das Event zurückgesetzt wurde.
        /// </summary>
        public DateTime DateReset { get; set; }

        /// <summary>
        /// Freie Verwendung von der PLC.
        /// </summary>
        public int UserData { get; set; }

        /// <summary>
        /// Liefert den Context des Alarms zurück.
        /// </summary>
        public AlarmEventContext Context { get; set; }

        public bool Equals(PlcAlarmEvent other)
        {
            return Id == other.Id && SrcId == other.SrcId;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PlcAlarmEvent);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public enum AlarmEventClass
    {
        None = 0,
        Maintenance = 1,
        Message = 2,
        Hint = 3,
        StateInfo = 4,
        Instruction = 5,
        Warning = 6,
        Alarm = 7,
        Paramerror = 8,
    }

    [Flags]
    public enum AlarmEventStates
    {
        Invalid = 0x00,
        Signaled = 0x01,
        Reset = 0x02,
        Confirmed = 0x10,
        ResetConfirmed = 0x12,
    }

    [Flags]
    public enum AlarmEventFlags
    {
        /// <summary>
        /// Setzt den Alarm, der nicht beweisbar ist.
        /// </summary>
        Req = 0x0001,

        /// <summary>
        /// Setzt den Alarm, der beweisbar ist.
        /// </summary>
        ReqMustConfirm = 0x0002,

        /// <summary>
        /// Setzt die Bestätigung.
        /// </summary>
        Confirm = 0x0004,

        /// <summary>
        /// Setzt das Event zurück.
        /// </summary>
        Reset = 0x0008,

        /// <summary>
        /// Die Event Priorität und Klasse wird von der Formatter Konfiguration gelesen.
        /// </summary>
        PriorityClass = 0x0010,

        /// <summary>
        /// Schreibt das Event in die Liste der logged Events.
        /// </summary>
        Log = 0x0040,

        /// <summary>
        /// Es wird eine Source-ID statt eines Source Namens verwendet.
        /// </summary>
        SrcId = 0x0100,

        /// <summary>
        /// Das Event setzt sich selber direkt zurück.
        /// </summary>
        SelfReset = 0x0200,

        /// <summary>
        /// Signalisiert erneut einen Alarm.
        /// </summary>
        Signal = 0x0800,

        /// <summary>
        /// Zeit an, dass das Event via ADS erzeugt wurde.
        /// </summary>
        Ads = 0x8000,
    }

    public enum AlarmEventContext
    {
        TestPlace,
        TestStand,
        TestGroup,
    }
}
