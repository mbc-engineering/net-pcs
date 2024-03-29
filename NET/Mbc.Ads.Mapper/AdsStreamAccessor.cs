﻿//-----------------------------------------------------------------------------
// Copyright (c) 2020 by mbc engineering, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using System;
using TwinCAT.Ads;
using TwinCAT.PlcOpen;
using TwinCAT.TypeSystem;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Functions for reading and writing to/from ADS streams.
    /// </summary>
    internal static class AdsStreamAccessor
    {
        /// <summary>
        /// Create a function for reading a primitive data type from an ADS reader.
        /// </summary>
        /// <param name="managedType">The .NET type to read</param>
        /// <param name="streamByteOffset">The offset of the subitem in bytes</param>
        /// <param name="sourceDatatype">Source Datatype information of the source ITcAdsDataType</param>
        /// <returns>A function to read a primitive value from the given ADS reader (not <c>null</c>).</returns>
        public static Func<AdsBinaryReader, object> CreatePrimitiveTypeReadFunction(Type managedType, int streamByteOffset, ITcAdsDataType sourceDatatype)
        {
            // Guards
            Ensure.Any.IsNotNull(managedType, optsFn: opts => opts.WithMessage("Could not create AdsStreamMappingDelegate for a PrimitiveType because the managedType is null."));
            EnsureArg.IsGte(streamByteOffset, 0, nameof(streamByteOffset));

            // Create Read delegate functions
            // ---------------------
            if (managedType == typeof(bool))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadBoolean();
            }

            if (managedType == typeof(byte))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadByte();
            }

            if (managedType == typeof(sbyte))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadSByte();
            }

            if (managedType == typeof(ushort))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadUInt16();
            }

            if (managedType == typeof(short))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadInt16();
            }

            if (managedType == typeof(uint))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadUInt32();
            }

            if (managedType == typeof(int))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadInt32();
            }

            if (managedType == typeof(float))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadSingle();
            }

            if (managedType == typeof(double))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadDouble();
            }

            if (managedType == typeof(TIME))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadPlcTIME();
            }

            if (managedType == typeof(DATE))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadPlcDATE();
            }

            if (managedType == typeof(DT))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadPlcDATE();
            }

            if (managedType == typeof(TOD))
            {
                return (adsReader) => Position(adsReader, streamByteOffset).ReadPlcTIME();
            }

            if (managedType == typeof(string) && sourceDatatype.DataTypeId == AdsDatatypeId.ADST_STRING)
            {
                // Example: byteSize = 81; // Size of 80 ANSI chars + /0 (STRING[80])
                return (adsReader) => Position(adsReader, streamByteOffset).ReadPlcString(sourceDatatype.ByteSize, System.Text.Encoding.UTF7);
            }

            if (managedType == typeof(string) && sourceDatatype.DataTypeId == AdsDatatypeId.ADST_WSTRING)
            {
                // Example: byteSize = 2 * 81; // Size of 80 UNICODE chars + /0 (WSTRING[80])
                return (adsReader) => Position(adsReader, streamByteOffset).ReadPlcString(sourceDatatype.ByteSize, System.Text.Encoding.Unicode);
            }

            throw new NotSupportedException($"AdsStreamMappingDelegate execution not supported for the ManagedType '{managedType?.ToString()}'.");
        }

        private static AdsBinaryReader Position(AdsBinaryReader adsReader, int offset)
        {
            adsReader.BaseStream.Position = offset;
            return adsReader;
        }

        /// <summary>
        /// Create a function for writing primitive data type to an ADS writer.
        /// </summary>
        /// <param name="managedType">The ADS managed type to write.</param>
        /// <param name="streamByteOffset">The offset of the subtime in bytes.</param>
        /// <param name="sourceDatatype">Size information of the source ITcAdsDataType</param>
        /// <returns>A function (=Action) to write a primitive value to a given ADS writer (not <c>null</c>).</returns>
        public static Action<AdsBinaryWriter, object> CreatePrimitiveTypeWriteFunction(Type managedType, int streamByteOffset, ITcAdsDataType sourceDatatype)
        {
            // Guards
            Ensure.Any.IsNotNull(managedType, optsFn: opts => opts.WithMessage("Could not create AdsStreamMappingDelegate for a PrimitiveType because the managedType is null."));
            EnsureArg.IsGte(streamByteOffset, 0, nameof(streamByteOffset));

            // Create Write delegate functions
            // ---------------------
            if (managedType == typeof(bool))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).Write((bool)value);
            }

            if (managedType == typeof(byte))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).Write((byte)value);
            }

            if (managedType == typeof(sbyte))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).Write((sbyte)value);
            }

            if (managedType == typeof(ushort))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).Write((ushort)value);
            }

            if (managedType == typeof(short))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).Write((short)value);
            }

            if (managedType == typeof(uint))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).Write((uint)value);
            }

            if (managedType == typeof(int))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).Write((int)value);
            }

            if (managedType == typeof(float))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).Write((float)value);
            }

            if (managedType == typeof(double))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).Write((double)value);
            }

            if (managedType == typeof(TIME))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).WritePlcType(new TIME((TimeSpan)value).Value);
            }

            if (managedType == typeof(DATE))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).WritePlcType(new DATE((DateTime)value).Value);
            }

            if (managedType == typeof(DT))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).WritePlcType(new DT((DateTime)value).Value);
            }

            if (managedType == typeof(TOD))
            {
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).WritePlcType(new TOD((TimeSpan)value).Time);
            }

            if (managedType == typeof(string) && sourceDatatype.DataTypeId == AdsDatatypeId.ADST_STRING)
            {
                if (sourceDatatype.ByteSize <= 1)
                {
                    throw new NotSupportedException($"AdsStreamMappingDelegate execution not possible for the ManagedType '{managedType?.ToString()}' with a total ByteSize of '{sourceDatatype.ByteSize}'.");
                }

                // Writing the string without the termination of /0
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).WritePlcAnsiStringFixedLength((string)value, sourceDatatype.ByteSize);
            }

            if (managedType == typeof(string) && sourceDatatype.DataTypeId == AdsDatatypeId.ADST_WSTRING)
            {
                if (sourceDatatype.ByteSize <= 2)
                {
                    throw new NotSupportedException($"AdsStreamMappingDelegate execution not possible for the ManagedType '{managedType?.ToString()}' with a total ByteSize of '{sourceDatatype.ByteSize}'.");
                }

                // Writing the string with the termination of /0
                return (adsWriter, value) => Position(adsWriter, streamByteOffset).WritePlcUnicodeStringFixedLength((string)value, sourceDatatype.ByteSize);
            }

            throw new NotSupportedException($"AdsStreamMappingDelegate execution not supported for the ManagedType '{managedType?.ToString()}'.");
        }

        private static AdsBinaryWriter Position(AdsBinaryWriter adsReader, int offset)
        {
            adsReader.BaseStream.Position = offset;
            return adsReader;
        }
    }
}
