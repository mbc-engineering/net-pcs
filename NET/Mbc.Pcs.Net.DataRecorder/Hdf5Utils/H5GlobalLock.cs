using System;
using System.Threading;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    /// <summary>
    /// Stellt einen anwendungsweiten Lock für den Zugriff auf HDF5
    /// Funktionen zur Verfügung.
    /// <p>Alle public-Methoden dieses Assembly, die auf HDF5 zugreifen
    /// benutzen intern diesen Lock.</p>
    /// </summary>
    public class H5GlobalLock : IDisposable
    {
        public static readonly object Sync = new object();

        public static bool HasLock => Monitor.IsEntered(Sync);

        public H5GlobalLock()
        {
            Monitor.Enter(Sync);
        }

        public void Dispose()
        {
            Monitor.Exit(Sync);
        }
    }
}
