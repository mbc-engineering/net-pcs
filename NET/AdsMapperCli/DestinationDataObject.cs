using Mbc.Pcs.Net.State;
using System;

namespace AdsMapperCli
{
    /* PLC Struct:
     TYPE ST_Test :
    STRUCT
	    bBoolValue1 : BOOL := TRUE;
	    nByteValue1 : BYTE := 255;
	    nSbyteValue1 : BYTE := 127;
	    nUshortValue1 : UINT := 65535;
	    nShortValue1 : INT := 32767;
	    nUintValue1 : UDINT := 4294967295;
	    nIntValue1 : DINT := 2147483647;
	    fFloatValue1 : REAL := -1.11;
	    fDoubleValue1 : LREAL := 1.11;
	    fDoubleValue2 : LREAL := 2.22;
	    fDoubleValue3 : LREAL := 3.33;
	    fDoubleValue4MappedName : LREAL := 4.44;
	    tPlcTimeValue1 : TIME := T#1H33M44S555MS;
	    dPlcDateValue1 : DATE := D#2021-08-30;
	    dtPlcDateTimeValue1 : DATE_AND_TIME := DT#2021-08-30-11:12:13;
	    aIntArrayValue : ARRAY[0..2] OF DINT := [1, 2, 3];
	    eEnumStateValue : E_State := E_State.eRunning;
        sPlcVersion : STRING(10) := '21.08.30.0';
    	sPlcVersion : STRING(10) := '21.08.30.0';
	    sUtf7String : STRING(6) := 'ÄÖö@Ü7';
	    wsUnicodeString : WSTRING(6) := "ÄÖö@Ü8";
    END_STRUCT
    END_TYPE

    {attribute 'qualified_only'}
    {attribute 'strict'}
    TYPE E_State :
    (
	    eNone    := 0,
	    eStartup := 1,
	    eRunning := 2,
	    eStop    := 3
    );
    END_TYPE
    */

    public class DestinationDataObject : IPlcState
    {
        #region " PlcAdsStateReader requires IPlcState interface "

        /// <summary>
        /// SPS Zeitstempel der Status Daten
        /// </summary>
        public DateTime PlcTimeStamp { get; set; }

        /// <summary>
        /// Güte der Status Daten
        /// </summary>
        public PlcDataQuality PlcDataQuality { get; set; }

        #endregion

        public bool BoolValue1 { get; set; }
        public byte ByteValue1 { get; set; }
        public sbyte SbyteValue1 { get; set; }
        public ushort UshortValue1 { get; set; }
        public short ShortValue1 { get; set; }
        public uint UintValue1 { get; set; }
        public int IntValue1 { get; set; }
        public float FloatValue1 { get; set; }
        public double DoubleValue1 { get; set; }
        public double DoubleValue2 { get; set; }
        public double DoubleValue3 { get; set; }
        public double DoubleValue4MappedName { get; set; }
        public TimeSpan PlcTimeValue1 { get; set; }
        public DateTime PlcDateValue1 { get; set; }
        public DateTime PlcDateTimeValue1 { get; set; }
        public int[] IntArrayValue { get; set; } = new int[3];
        public State EnumStateValue { get; set; }
        public string PlcVersion { get; set; }
        public string Utf7String { get; set; }
        public string UnicodeString { get; set; }

        //public Motor MotorObject { get; set; }
    }

    public class Motor
    {
        public double ActualSpeed { get; set; }
    }

    public enum State
    {
        None = 0,
        Startup = 1,
        Running = 2,
        Stop = 3,
    }
}
