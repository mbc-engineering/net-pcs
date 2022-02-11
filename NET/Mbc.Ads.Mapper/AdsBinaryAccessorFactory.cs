//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using System;
using System.Text;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;

#nullable enable
namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Create <see cref="IAdsDataReader"/> or <see cref="IAdsDataWriter"/> instances
    /// for reading and writing to/from binary ADS data.
    /// </summary>
    internal static class AdsBinaryAccessorFactory
    {
        /// <summary>
        /// Create a <see cref="IAdsDataReader"/> for reading primitive data type from an ADS data.
        /// </summary>
        /// <param name="managedType">The .NET type to read</param>
        /// <param name="readOffset">The offset of the subitem in bytes</param>
        /// <param name="sourceDatatype">Source Datatype information of the source ITcAdsDataType</param>
        /// <returns> A instance of <see cref="IAdsDataReader"/> (not <c>null</c>).</returns>
        public static IAdsDataReader CreatePrimitiveTypeReadFunction(Type managedType, int readOffset, IDataType sourceDatatype)
        {
            // Guards
            Ensure.Any.IsNotNull(managedType);
            EnsureArg.IsGte(readOffset, 0, nameof(readOffset));

            if (managedType == typeof(bool))
            {
                return new AdsDataReaderBoolean(readOffset);
            }

            if (managedType == typeof(byte))
            {
                return new AdsDataReaderByte(readOffset);
            }

            if (managedType == typeof(sbyte))
            {
                return new AdsDataReaderSByte(readOffset);
            }

            if (managedType == typeof(ushort))
            {
                return new AdsDataReaderUShort(readOffset);
            }

            if (managedType == typeof(short))
            {
                return new AdsDataReaderShort(readOffset);
            }

            if (managedType == typeof(uint))
            {
                return new AdsDataReaderUInt(readOffset);
            }

            if (managedType == typeof(int))
            {
                return new AdsDataReaderInt(readOffset);
            }

            if (managedType == typeof(float))
            {
                return new AdsDataReaderFloat(readOffset);
            }

            if (managedType == typeof(double))
            {
                return new AdsDataReaderDouble(readOffset);
            }

            if (managedType == typeof(TIME))
            {
                return new AdsDataReaderTime(readOffset);
            }

            if (managedType == typeof(DATE))
            {
                return new AdsDataReaderDate(readOffset);
            }

            if (managedType == typeof(DT))
            {
                return new AdsDataReaderDT(readOffset);
            }

            if (managedType == typeof(string))
            {
                var stringType = (IStringType)sourceDatatype;
                if (!stringType.IsFixedLength)
                {
                    throw new NotSupportedException("Only fixed string types are supported.");
                }

                return new AdsDataReaderString(readOffset, stringType.Encoding, stringType.Length, stringType.ByteSize);
            }

            throw new NotSupportedException($"AdsStreamMappingDelegate execution not supported for the ManagedType '{managedType?.ToString()}'.");
        }


        /// <summary>
        /// Create a function for writing primitive data type to an ADS writer.
        /// </summary>
        /// <param name="managedType">The ADS managed type to write.</param>
        /// <param name="writeOffset">The offset of the subtime in bytes.</param>
        /// <param name="sourceDatatype">Size information of the source ITcAdsDataType</param>
        /// <returns>A function (=Action) to write a primitive value to a given ADS writer (not <c>null</c>).</returns>
        public static IAdsDataWriter CreatePrimitiveTypeWriteFunction(Type managedType, int writeOffset, IDataType sourceDatatype)
        {
            // Guards
            Ensure.Any.IsNotNull(managedType, optsFn: opts => opts.WithMessage("Could not create AdsStreamMappingDelegate for a PrimitiveType because the managedType is null."));
            EnsureArg.IsGte(writeOffset, 0, nameof(writeOffset));

            if (managedType == typeof(bool))
            {
                return new AdsDataWriterBoolean(writeOffset);
            }

            if (managedType == typeof(byte))
            {
                return new AdsDataWriterByte(writeOffset);
            }

            if (managedType == typeof(sbyte))
            {
                return new AdsDataWriterSByte(writeOffset);
            }

            if (managedType == typeof(ushort))
            {
                return new AdsDataWriterUShort(writeOffset);
            }

            if (managedType == typeof(short))
            {
                return new AdsDataWriterShort(writeOffset);
            }

            if (managedType == typeof(uint))
            {
                return new AdsDataWriterUInt(writeOffset);
            }

            if (managedType == typeof(int))
            {
                return new AdsDataWriterInt(writeOffset);
            }

            if (managedType == typeof(float))
            {
                return new AdsDataWriterFloat(writeOffset);
            }

            if (managedType == typeof(double))
            {
                return new AdsDataWriterDouble(writeOffset);
            }

            if (managedType == typeof(TIME))
            {
                return new AdsDataWriterTime(writeOffset);
            }

            if (managedType == typeof(DATE))
            {
                return new AdsDataWriterDate(writeOffset);
            }

            if (managedType == typeof(DT))
            {
                return new AdsDataWriterDT(writeOffset);
            }

            if (managedType == typeof(string))
            {
                var stringType = (IStringType)sourceDatatype;
                if (!stringType.IsFixedLength)
                {
                    throw new NotSupportedException("Only fixed string types are supported.");
                }

                return new AdsDataWriterString(writeOffset, stringType.Encoding, stringType.Length, stringType.ByteSize);
            }

            throw new NotSupportedException($"AdsStreamMappingDelegate execution not supported for the ManagedType '{managedType?.ToString()}'.");
        }

        private abstract class AdsOffsetBase
        {
            protected readonly int _offset;

            public AdsOffsetBase(int offset)
            {
                _offset = offset;
            }
        }

        #region IAdsReader implementation

        private class AdsDataReaderBoolean : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderBoolean(int offset)
                : base(offset)
            {
            }

            public object Read(ReadOnlySpan<byte> buffer) => buffer[_offset] != 0;
        }

        private class AdsDataReaderByte : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderByte(int offset)
                : base(offset)
            {
            }

            public object Read(ReadOnlySpan<byte> buffer) => buffer[_offset];
        }

        private class AdsDataReaderSByte : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderSByte(int offset)
                : base(offset)
            {
            }

            public object Read(ReadOnlySpan<byte> buffer) => (sbyte)buffer[_offset];
        }

        private class AdsDataReaderUShort : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderUShort(int offset)
                : base(offset)
            {
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 2))
                {
                    return *(ushort*)p;
                }
            }
        }

        private class AdsDataReaderShort : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderShort(int offset)
                : base(offset)
            {
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 2))
                {
                    return *(short*)p;
                }
            }
        }

        private class AdsDataReaderUInt : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderUInt(int offset)
                : base(offset)
            {
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    return *(uint*)p;
                }
            }
        }

        private class AdsDataReaderInt : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderInt(int offset)
                : base(offset)
            {
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    return *(int*)p;
                }
            }
        }

        private class AdsDataReaderFloat : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderFloat(int offset)
                : base(offset)
            {
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    return *(float*)p;
                }
            }
        }

        private class AdsDataReaderDouble : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderDouble(int offset)
                : base(offset)
            {
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    return *(double*)p;
                }
            }
        }

        private class AdsDataReaderTime : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderTime(int offset)
                : base(offset)
            {
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    var value = *(uint*)p;
                    return new TIME(value).Time;
                }
            }
        }

        private class AdsDataReaderDate : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderDate(int offset)
                : base(offset)
            {
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    var value = *(uint*)p;
                    return new DATE(value).Date;
                }
            }
        }

        private class AdsDataReaderDT : AdsOffsetBase, IAdsDataReader
        {
            public AdsDataReaderDT(int offset)
                : base(offset)
            {
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    var value = *(uint*)p;
                    return new DT(value).DateTime;
                }
            }
        }

        private class AdsDataReaderString : AdsOffsetBase, IAdsDataReader
        {
            private readonly Encoding _encoding;
            private readonly int _fixedLength;
            private readonly int _byteSize;

            public AdsDataReaderString(int offset, Encoding encoding, int fixedLength, int byteSize)
                : base(offset)
            {
                _encoding = encoding;
                _fixedLength = fixedLength;
                _byteSize = byteSize;
            }

            public unsafe object Read(ReadOnlySpan<byte> buffer)
            {
                var charBuffer = new char[_fixedLength + 1];

                fixed (byte* p = buffer.Slice(_offset, _byteSize))
                {
                    fixed (char* q = charBuffer)
                    {
                        // last byte is always 0 and must not be decoded
                        int actualCharLength = _encoding.GetChars(p, _byteSize, q, _fixedLength + 1);

                        for (int i = 0; i < actualCharLength; i++)
                        {
                            if (charBuffer[i] == char.MinValue)
                            {
                                actualCharLength = i;
                                break;
                            }
                        }

                        return new string(charBuffer, 0, actualCharLength);
                    }
                }
            }
        }

        #endregion

        private class AdsDataWriterBoolean : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterBoolean(int offset)
                : base(offset)
            {
            }

            public void Write(object value, Span<byte> buffer)
            {
                buffer[_offset] = (byte)((bool)value ? 1 : 0);
            }
        }

        private class AdsDataWriterByte : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterByte(int offset)
                : base(offset)
            {
            }

            public void Write(object value, Span<byte> buffer)
            {
                buffer[_offset] = (byte)value;
            }
        }

        private class AdsDataWriterSByte : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterSByte(int offset)
                : base(offset)
            {
            }

            public void Write(object value, Span<byte> buffer)
            {
                buffer[_offset] = (byte)(sbyte)value;
            }
        }

        private class AdsDataWriterUShort : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterUShort(int offset)
                : base(offset)
            {
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 2))
                {
                    *(ushort*)p = (ushort)value;
                }
            }
        }

        private class AdsDataWriterShort : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterShort(int offset)
                : base(offset)
            {
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 2))
                {
                    *(short*)p = (short)value;
                }
            }
        }

        private class AdsDataWriterUInt : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterUInt(int offset)
                : base(offset)
            {
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    *(uint*)p = (uint)value;
                }
            }
        }

        private class AdsDataWriterInt : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterInt(int offset)
                : base(offset)
            {
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    *(int*)p = (int)value;
                }
            }
        }

        private class AdsDataWriterFloat : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterFloat(int offset)
                : base(offset)
            {
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    *(float*)p = (float)value;
                }
            }
        }

        private class AdsDataWriterDouble : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterDouble(int offset)
                : base(offset)
            {
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, 8))
                {
                    *(double*)p = (double)value;
                }
            }
        }

        private class AdsDataWriterTime : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterTime(int offset)
                : base(offset)
            {
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                var time = new TIME((TimeSpan)value);
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    *(uint*)p = time.InternalTimeValue;
                }
            }
        }

        private class AdsDataWriterDate : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterDate(int offset)
                : base(offset)
            {
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                var date = new DATE((DateTime)value);
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    *(uint*)p = date.Ticks;
                }
            }
        }

        private class AdsDataWriterDT : AdsOffsetBase, IAdsDataWriter
        {
            public AdsDataWriterDT(int offset)
                : base(offset)
            {
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                var date = new DT((DateTime)value);
                fixed (byte* p = buffer.Slice(_offset, 4))
                {
                    *(uint*)p = date.Ticks;
                }
            }
        }

        private class AdsDataWriterString : AdsOffsetBase, IAdsDataWriter
        {
            private readonly Encoding _encoding;
            private readonly int _fixedLength;
            private readonly int _byteSize;

            public AdsDataWriterString(int offset, Encoding encoding, int fixedLength, int byteSize)
                : base(offset)
            {
                _encoding = encoding;
                _fixedLength = fixedLength;
                _byteSize = byteSize;
            }

            public unsafe void Write(object value, Span<byte> buffer)
            {
                fixed (byte* p = buffer.Slice(_offset, _byteSize))
                {
                    var chars = new char[_fixedLength + 1];

                    var s = (string)value;
                    var charCount = Math.Min(s.Length, _fixedLength);
                    s.CopyTo(0, chars, 0, charCount);
                    chars[charCount] = char.MinValue;

                    fixed (char* q = chars)
                    {
                        _encoding.GetBytes(q, charCount + 1, p, _byteSize);
                    }
                }
            }
        }

    }
}
