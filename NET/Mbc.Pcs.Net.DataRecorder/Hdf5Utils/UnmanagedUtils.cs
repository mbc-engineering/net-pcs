using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    /// <summary>
    /// Hilfsfunktionen für unmanaged Daten.
    /// </summary>
    internal static class UnmanagedUtils
    {
        /// <summary>
        /// Liefert einen Single-Wert zurück, der sich an der
        /// Stelle <paramref name="ptr"/> befindet.
        /// </summary>
        public static unsafe float PtrToSingle(IntPtr ptr)
        {
            return *((float*)ptr.ToPointer());
        }

        /// <summary>
        /// Liefert einen Double-Wert zurück, der sich an der
        /// Stelle <paramref name="ptr"/> befindet.
        /// </summary>
        public static unsafe double PtrToDouble(IntPtr ptr)
        {
            return *((double*)ptr.ToPointer());
        }

        /// <summary>
        /// Schreibt einen Single-Wert an die Stelle
        /// von <paramref name="ptr"/>.
        /// </summary>
        public static unsafe void SingleToPtr(float value, IntPtr ptr)
        {
            *((float*)ptr.ToPointer()) = value;
        }

        /// <summary>
        /// Schreibt einen Double-Wert an die Stelle
        /// von <paramref name="ptr"/>.
        /// </summary>
        public static unsafe void DoubleToPtr(double value, IntPtr ptr)
        {
            *((double*)ptr.ToPointer()) = value;
        }

        /// <summary>
        /// Liefert einen ASCII-String, der an der Stelle <paramref name="ptr"/>
        /// liegt zurück. Die Grösse wird entweder explizit angegeben oder wenn
        /// <paramref name="size"/> -1 ist wird sie automatisch ermittelt.
        /// </summary>
        public static unsafe string PtrToStringASCII(IntPtr ptr, int size)
        {
            if (size == -1)
            {
                size = EndOfString(ptr);
            }

            var buffer = new byte[size];
            Marshal.Copy(ptr, buffer, 0, size);
            return Encoding.ASCII.GetString(buffer);
        }

        /// <summary>
        /// Liefert einen UTF8-String, der an der Stelle <paramref name="ptr"/>
        /// liegt zurück. Die Grösse wird entweder explizit angegeben oder wenn
        /// <paramref name="size"/> -1 ist wird sie automatisch ermittelt.
        /// </summary>
        public static string PtrToStringUTF8(IntPtr ptr, int size)
        {
            if (size == -1)
            {
                size = EndOfString(ptr);
            }

            var buffer = new byte[size];
            Marshal.Copy(ptr, buffer, 0, size);
            return Encoding.UTF8.GetString(buffer);
        }

        private static unsafe int EndOfString(IntPtr ptr)
        {
            var size = 0;
            while (*((byte*)(ptr + size).ToPointer()) != 0)
            {
                size++;
            }

            return size;
        }
    }
}
