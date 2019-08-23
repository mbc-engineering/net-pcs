using System;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    /// <summary>
    /// Beschreibt einen einzelnen Channel eines Ringbuffers.
    /// </summary>
    public class ChannelInfo
    {
        public ChannelInfo(string name, Type type, int oversamplingFator = 1)
        {
            Name = name;
            Type = type;
            OversamplingFator = oversamplingFator;
        }

        public string Name { get; }
        public Type Type { get; }
        public int OversamplingFator { get; }
        public int OversamplingFactor { get; }
    }
}
