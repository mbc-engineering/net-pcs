using System.Collections.Generic;
using System.Linq;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    /// <summary>
    /// Beschreibt die Struktur eines Ringbuffers.
    /// </summary>
    public class RingBufferInfo
    {
        public RingBufferInfo(int size, int chunkSize, IEnumerable<ChannelInfo> channelInfos)
        {
            Size = size;
            ChunkSize = chunkSize;
            ChannelInfo = channelInfos.ToList();
        }

        public int Size { get; }
        public int ChunkSize { get; }
        public IReadOnlyList<ChannelInfo> ChannelInfo { get; }
    }
}
