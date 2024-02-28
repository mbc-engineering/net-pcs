using System;
using TwinCAT.PlcOpen;

namespace Mbc.Ads.Utils
{
    public static class AdsConvert
    {
        public static T ChangeType<T>(object value)
        {
            return (T)ChangeType(value, typeof(T));
        }

        public static object ChangeType(object value, Type conversionType)
        {
            if (value == null)
            {
                if (conversionType.IsValueType)
                {
                    throw new InvalidCastException("Cannot cast value=null to value type.");
                }

                return null;
            }
            else if (conversionType.IsInstanceOfType(value))
            {
                return value;
            }
            else if (conversionType == typeof(TimeSpan))
            {
                if (value is TIME plcTime)
                {
                    return plcTime.Time;
                }

                throw new InvalidCastException($"Typce conversion requires TIME-plc type for TimeSpan.");
            }
            else if (conversionType == typeof(DateTime))
            {
                if (value is DATE plcDate)
                {
                    return plcDate.Date;
                }

                throw new InvalidCastException($"Typce conversion requires DATE-plc type for DateTime.");
            }
            else if (conversionType == typeof(DateTimeOffset))
            {
                if (value is DATE plcDate)
                {
                    return new DateTimeOffset(plcDate.Date);
                }

                throw new InvalidCastException($"Typce conversion requires DATE-plc type for DateTime.");
            }
            else if (conversionType == typeof(TIME))
            {
                if (value is TimeSpan tsTime)
                {
                    return new TIME(tsTime);
                }
                else if (value is long lngTime)
                {
                    return new TIME(lngTime);
                }
                else if (value is uint uintTime)
                {
                    return new TIME(uintTime);
                }

                throw new InvalidCastException($"Typce conversion requires TimeSpan, long or uint type for TwinCAT.PlcOpen.TIME.");
            }
            else if (conversionType == typeof(DATE))
            {
                if (value is DateTime dtDate)
                {
                    return new DATE(dtDate);
                }
                else if (value is DateTimeOffset dtoDate)
                {
                    return new DATE(dtoDate);
                }

                throw new InvalidCastException($"Typce conversion requires DateTime or DateTimeOffset for TwinCAT.PlcOpen.DATE.");
            }
            else
            {
                try
                {
                    return Convert.ChangeType(value, conversionType);
                }
                catch (InvalidCastException e)
                {
                    throw new InvalidCastException($"Typce conversion is not from the required type symbol to {conversionType}. Actual type = {value?.GetType()}", e);
                }
            }
        }
    }
}
