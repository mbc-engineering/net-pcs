using HDF.PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils
{
    [Serializable]
    public class H5Error : Exception
    {
        public H5Error()
        {
        }

        public H5Error(string message)
            : base(message)
        {
        }

        public H5Error(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public static H5Error GetExceptionFromHdf5Stack()
        {
            return new ErrorWalker().Error;
        }

        public static int CheckH5Result(int result)
        {
            if (result < 0)
                throw GetExceptionFromHdf5Stack();

            return result;
        }

        public static long CheckH5Result(long result)
        {
            if (result < 0)
                throw GetExceptionFromHdf5Stack();

            return result;
        }

        private class ErrorWalker
        {
            private readonly List<H5Error> _errors = new List<H5Error>();
            private readonly List<H5E.error_t> _walkedHdf5Errors = new List<H5E.error_t>();

            public ErrorWalker()
            {
                lock (H5GlobalLock.Sync)
                {
                    var res = H5E.walk(H5E.DEFAULT, H5E.direction_t.H5E_WALK_UPWARD, Walker, IntPtr.Zero);
                    if (res < 0)
                        throw new InvalidOperationException("Could not walk HDF5 error.");
                }

                /*
                 * Aus einen unbekannten Grund können die Error-Messages nicht im Callback des Walkers
                 * erstellt werden (Program crashed ohne erkennbare Ursache). Daher werden im Callback
                 * nur die Error-Strukturen gesammelt und dann anschliessend hier ausgewertet.
                 */

                foreach (var foo in _walkedHdf5Errors)
                {
                    {
                        H5E.type_t type = default(H5E.type_t); // wird eigentlich nicht benötigt

                        var majorMsg = new StringBuilder(1024);
                        var minorMsg = new StringBuilder(1024);

                        lock (H5GlobalLock.Sync)
                        {
                            H5E.get_msg(foo.maj_num, ref type, majorMsg, new IntPtr(majorMsg.Capacity));
                            H5E.get_msg(foo.min_num, ref type, minorMsg, new IntPtr(majorMsg.Capacity));
                        }

                        var msg = $"{foo.func_name}: {foo.desc} -> {majorMsg} - {minorMsg}";

                        H5Error exc;
                        if (_errors.Count == 0)
                            exc = new H5Error(msg);
                        else
                            exc = new H5Error(msg, _errors.Last());

                        /*
                        TODO hier könnten noch deutlich mehr Informationen herausgeholt werden, wenn notwendig.
                        Siehe auch http://davis.lbl.gov/Manuals/HDF5-1.8.7/UG/13_ErrorHandling.html
                        */

                        _errors.Add(exc);
                    }
                }
            }

            private int Walker(uint n, ref H5E.error_t err_desc, IntPtr client_data)
            {
                _walkedHdf5Errors.Add(err_desc);
                return 0;
            }

            public H5Error Error => _errors.Last();
        }

        protected H5Error(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}
