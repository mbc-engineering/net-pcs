using System;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    /// <summary>
    /// Beschreibt einen einzelnen Channel eines Ringbuffers.
    /// </summary>
    public class ChannelInfo
    {
        public ChannelInfo(string name, Type type, int oversamplingFactor = 1)
        {
            Name = name;
            Type = type;
            OversamplingFactor = oversamplingFactor;
        }

        public string Name { get; }
        public Type Type { get; }
        public int OversamplingFactor { get; }
    }
}
