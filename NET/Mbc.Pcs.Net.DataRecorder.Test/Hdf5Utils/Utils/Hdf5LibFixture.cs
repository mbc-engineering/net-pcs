using Mbc.Pcs.Net.DataRecorder.Hdf5Utils;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Mbc.Pcs.Net.DataRecorder.Test.Hdf5Utils.Utils
{
    /// <summary>
    /// Hilfsklasse um bei Testende die geladene HDF5-DLL zu entladen.
    /// </summary>
    public class Hdf5LibFixture : IDisposable
    {
        public Hdf5LibFixture()
        {
            H5Utils.OpenH5();
        }

        public void Dispose()
        {
            H5Utils.CloseH5();

            var hdf5Dll = FindHdf5DllModule();
            if (hdf5Dll != null)
            {
                FreeLibrary(hdf5Dll.BaseAddress);
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);

        private ProcessModule FindHdf5DllModule()
        {
            return Process.GetCurrentProcess()
                .Modules
                .OfType<ProcessModule>()
                .Where(x => x.ModuleName == "hdf5.dll")
                .FirstOrDefault();
        }
    }
}
