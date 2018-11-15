using EnsureThat;
using System;
using TwinCAT.Ads;
using TwinCAT.PlcOpen;

namespace Mbc.Ads.Mapper
{
    /// <summary>
    /// Function for mapping primitve data types to/from ADS streams.
    /// </summary>
    internal static class PrimitiveDataTypeMapping
    {
        /// <summary>
        /// Create a function for reading a primitive data type from an ADS reader.
        /// </summary>
        /// <param name="managedType">The .NET type to read</param>
        /// <param name="streamOffset">The offset of the SubItem in Bytes</param>
        /// <returns>A function to read a primitive value from the given ADS reader (not <c>null</c>).</returns>
        public static Func<AdsBinaryReader, object> CreatePrimitiveTypeReadFunction(Type managedType, int streamByteOffset)
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

            throw new NotSupportedException($"AdsStreamMappingDelegate execution not supported for the ManagedType '{managedType?.ToString()}'.");
        }

        private static AdsBinaryReader Position(AdsBinaryReader adsReader, int offset)
        {
            adsReader.BaseStream.Position = offset;
            return adsReader;
        }


    }
}
