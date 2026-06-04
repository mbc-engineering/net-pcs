using HDF.PInvoke;
using System;
using System.Threading;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    public static class H5Utils
    {
        private static object _initLock = new object();
        private static bool _initialized;
        private static int _destroyed;

        public static void OpenH5(bool autoClose = true)
        {
            lock (_initLock)
            {
                if (_initialized)
                    return;
                _initialized = true;

                H5Error.CheckH5Result(H5.open());
                if (autoClose)
                {
                    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                    {
                        CloseH5();
                    };
                    AppDomain.CurrentDomain.DomainUnload += (sender, e) =>
                    {
                        CloseH5();
                    };
                }
            }
        }

        public static void CloseH5()
        {
            if (Interlocked.Exchange(ref _destroyed, 1) == 1)
                return;
            H5Error.CheckH5Result(H5.close());
        }

        /// <summary>
        /// Liefert den Gruppennamen eines Location-Strings zurück,
        /// sofern einer existiert.
        /// </summary>
        public static string GetGroups(string name)
        {
            var idx = name.LastIndexOf('/');
            if (idx < 0)
                return string.Empty;

            return name.Substring(0, idx);
        }

        /// <summary>
        /// Erzeugt eine <see cref="H5Attribute"/>-Instanz für eine <see cref="H5File"/>.
        /// </summary>
        public static H5Attribute Attributes(this H5File file) => new H5Attribute(file);

        /// <summary>
        /// Erzeugt eine <see cref="H5Attribute"/>-Instanz für eine <see cref="H5DataSet"/>.
        /// </summary>
        public static H5Attribute Attributes(this H5DataSet dataSet) => new H5Attribute(dataSet);
    }
}
