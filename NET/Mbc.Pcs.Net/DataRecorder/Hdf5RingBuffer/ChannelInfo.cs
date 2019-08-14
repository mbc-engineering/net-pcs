using System;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    /// <summary>
    /// Beschreibt einen einzelnen Channel eines Ringbuffers.
    /// </summary>
    public class ChannelInfo
    {
        public ChannelInfo(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; }
        public Type Type { get; }
    }
}
