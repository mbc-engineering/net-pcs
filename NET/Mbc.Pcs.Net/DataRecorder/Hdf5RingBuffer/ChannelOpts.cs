using System.Collections.Generic;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    /// <summary>
    /// Optionen für <see cref="ChannelInfo"/>.
    /// </summary>
    public class ChannelOpts
    {
        private readonly Dictionary<string, int> _oversamplingChannels = new Dictionary<string, int>();
        private readonly HashSet<string> _ignoredProperties = new HashSet<string>();
        private readonly Dictionary<string, (int Start, int Count)> _multiChannel = new Dictionary<string, (int Start, int Count)>();

        internal ChannelOpts()
        {
        }

        /// <summary>
        /// Konfiguriert den angegebenen Kanal als Oversampling-Channel
        /// mit dem angegebenen Faktor.
        /// </summary>
        public ChannelOpts WithOversampling(string propName, int oversamplingFactor)
        {
            _oversamplingChannels[propName] = oversamplingFactor;
            return this;
        }

        public ChannelOpts WithMulti(string propName, int startIndex, int count)
        {
            _multiChannel[propName] = (startIndex, count);
            return this;
        }

        /// <summary>
        /// Ignoriert das Property mit dem angegebenen Namen.
        /// </summary>
        public ChannelOpts IgnoreProperty(string propName)
        {
            _ignoredProperties.Add(propName);
            return this;
        }

        internal Dictionary<string, int> OversamplingChannels => _oversamplingChannels;

        internal HashSet<string> IgnoredProperties => _ignoredProperties;

        internal Dictionary<string, (int Start, int Count)> MultiChannel => _multiChannel;
    }
}
