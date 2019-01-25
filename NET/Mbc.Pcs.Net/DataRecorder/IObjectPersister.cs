using System.IO;

namespace Mbc.Pcs.Net.DataRecorder
{
    /// <summary>
    /// Implementierungen persistieren Objekte als Stream und zurück
    /// für den <see cref="FileRingBuffer"/>.
    /// </summary>
    public interface IObjectPersister
    {
        void Serialize(object data, Stream stream);

        object Deserialize(Stream stream);
    }
}
