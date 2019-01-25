using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Mbc.Pcs.Net.DataRecorder
{
    /// <summary>
    /// Eine Implementierung der <see cref="IObjectPersister"/>-Schnittstelle, der
    /// Objekt mit dem .NET-Serializierungsmechanismus schreibt und liest.
    /// </summary>
    public class SerializationObjectPersister : IObjectPersister
    {
        public object Deserialize(Stream stream)
        {
            var formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }

        public void Serialize(object data, Stream stream)
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, data);
        }
    }
}
