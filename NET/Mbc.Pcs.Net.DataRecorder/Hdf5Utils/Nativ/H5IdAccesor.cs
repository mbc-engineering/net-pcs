namespace Mbc.Pcs.Net.DataRecorder.Hdf5Utils.Nativ
{
    /// <summary>
    /// Liefert für spezielle Anwendungen die interne ID von verschiedenen
    /// HDF5-Objekten zurück.
    /// </summary>
    public static class H5IdAccesor
    {
        public static long GetId(H5File h5File) => h5File.Id;
        public static long GetId(H5DataSet h5DataSet) => h5DataSet.Id;
        public static long GetId(H5DataSpace h5DataSpace) => h5DataSpace.Id;
        public static long GetId(H5Group h5Group) => h5Group.Id;
        public static long GetId(H5Type h5Type) => h5Type.Id;

    }
}
