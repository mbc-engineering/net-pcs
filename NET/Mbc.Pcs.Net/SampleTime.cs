using EnsureThat;
using System;
using System.Collections.Generic;

namespace Mbc.Pcs.Net
{
    /// <summary>
    /// Representiert einen diskreten Zeitpunkt eines Samples,
    /// der Abhängig ist von seiner Samplerate.
    /// <para>Der Rohwert (<see cref="FromRawValue(ulong)"/> und <see cref="ToRawValue"/> ist
    /// ein Int64-Typ in dem sowohl die Samplerate wie auch der Samplezähler kombiniert ist. Der
    /// Wert ist so aufgebaut, dass Differenzen die Sampleanzahl ergibt und im Dezimalsystem
    /// leicht abgelesen werden kann um welches Sample es sich handelt.</para>
    /// </summary>
    public struct SampleTime : IComparable, IComparable<SampleTime>, IEquatable<SampleTime>
    {
        private const long FileTimePerMillisecond = 10 * 1000;
        private const long FileTimePerSecond = FileTimePerMillisecond * 1000;
        private const uint RawMaxSampleRate = 1000000;
        private const ulong RawMaxSampleCount = 100000000000000000;

        private static readonly long RawFileTimeBase = new DateTime(2010, 1, 1, 0, 0, 0).ToFileTime();
        private static readonly SortedList<uint, uint> SampleRateMapping;

        private readonly long _sampleNumber;

        static SampleTime()
        {
            var divisors = new SortedList<uint, uint>();
            for (uint i = 1; i <= RawMaxSampleRate; i++)
            {
                if (RawMaxSampleRate % i == 0)
                    divisors.Add(i, i);
            }

            SampleRateMapping = divisors;
        }

        public static SampleTime FromFileTime(long fileTime, uint sampleRate) => new SampleTime(fileTime / GetSampleFileTime(sampleRate), sampleRate);

        public static SampleTime FromRawValue(ulong rawValue)
        {
            var sampleRate = SampleRateMapping.Values[(int)(rawValue / RawMaxSampleCount)];
            return new SampleTime((RawFileTimeBase / GetSampleFileTime(sampleRate)) + (long)(rawValue % RawMaxSampleCount), sampleRate);
        }

        public SampleTime(DateTime timeStamp, uint sampleRate)
            : this(timeStamp.ToFileTime() / GetSampleFileTime(sampleRate), sampleRate)
        {
        }

        private SampleTime(long sampleNumber, uint sampleRate)
        {
            _sampleNumber = sampleNumber;
            SampleRate = sampleRate;
        }

        /// <summary>
        /// Liefert für die gegebene Samplerate die Zeit als FileTime zurück.
        /// </summary>
        private static long GetSampleFileTime(uint sampleRate)
        {
            EnsureArg.Is(FileTimePerSecond % sampleRate, 0, nameof(sampleRate), optsFn: x => x.WithMessage("Uneven samplerate"));
            return FileTimePerSecond / sampleRate;
        }

        /// <summary>
        /// Gibt die Samplerate in [Hz] zurück.
        /// </summary>
        public uint SampleRate { get; }

        /// <summary>
        /// Gibt die Samplerate als <see cref="TimeSpan"/> zurück.
        /// </summary>
        public TimeSpan SampleRateTimeSpan => TimeSpan.FromTicks(GetSampleFileTime(SampleRate) * TimeSpan.TicksPerMillisecond / FileTimePerMillisecond);

        public DateTime ToDateTime() => DateTime.FromFileTime(GetSampleFileTime(SampleRate) * _sampleNumber);

        public bool Equals(SampleTime other) => _sampleNumber == other._sampleNumber && SampleRate == other.SampleRate;

        public override bool Equals(object obj)
        {
            if (obj is SampleTime other)
            {
                return Equals(other);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (int)((int)_sampleNumber ^ (int)(_sampleNumber >> 32) ^ SampleRate);
        }

        public int CompareTo(object obj)
        {
            return CompareTo((SampleTime)obj);
        }

        public int CompareTo(SampleTime other)
        {
            if (SampleRate == other.SampleRate)
            {
                if (_sampleNumber > other._sampleNumber)
                    return 1;
                if (_sampleNumber < other._sampleNumber)
                    return -1;
                return 0;
            }

            return ToDateTime().CompareTo(other.ToDateTime());
        }

        public long Subtract(SampleTime minuend)
        {
            if (SampleRate != minuend.SampleRate)
                throw new ArgumentException($"SampleRate of subtraction must be the same ({SampleRate} != {minuend.SampleRate}).");

            return _sampleNumber - minuend._sampleNumber;
        }

        public SampleTime Subtract(long minuend) => new SampleTime(_sampleNumber - minuend, SampleRate);

        public SampleTime Subtract(TimeSpan minuend) => new SampleTime(_sampleNumber - (minuend.Ticks * SampleRate / TimeSpan.TicksPerSecond), SampleRate);

        public SampleTime Add(long summand) => new SampleTime(_sampleNumber + summand, SampleRate);

        public SampleTime Add(TimeSpan summand) => new SampleTime(_sampleNumber + (summand.Ticks * SampleRate / TimeSpan.TicksPerSecond), SampleRate);

        public SampleTime Next() => new SampleTime(_sampleNumber + 1, SampleRate);

        public SampleTime Previous() => new SampleTime(_sampleNumber - 1, SampleRate);

        public long GetSampleNumber() => _sampleNumber;

        public ulong ToRawValue()
        {
            var divisor = (uint)SampleRateMapping.IndexOfKey(SampleRate);

            return (ulong)(_sampleNumber - (RawFileTimeBase / GetSampleFileTime(SampleRate))) + (divisor * RawMaxSampleCount);
        }

        public override string ToString() => $"{_sampleNumber}@{SampleRate}|{ToDateTime()}";

        public static bool operator ==(SampleTime left, SampleTime right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SampleTime left, SampleTime right)
        {
            return !(left == right);
        }

        public static bool operator <(SampleTime left, SampleTime right) => left.CompareTo(right) < 0;

        public static bool operator >(SampleTime left, SampleTime right) => left.CompareTo(right) > 0;

        public static bool operator <=(SampleTime left, SampleTime right) => left.CompareTo(right) <= 0;

        public static bool operator >=(SampleTime left, SampleTime right) => left.CompareTo(right) >= 0;

        public static long operator -(SampleTime left, SampleTime right) => left.Subtract(right);

        public static SampleTime operator -(SampleTime left, long right) => left.Subtract(right);

        public static SampleTime operator +(SampleTime left, long right) => left.Add(right);

        public static SampleTime operator -(SampleTime left, TimeSpan right) => left.Subtract(right);

        public static SampleTime operator +(SampleTime left, TimeSpan right) => left.Add(right);

        public static SampleTime operator ++(SampleTime op) => op.Next();

        public static SampleTime operator --(SampleTime op) => op.Previous();

        public static explicit operator long(SampleTime op) => op.GetSampleNumber();
    }
}
