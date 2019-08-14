using Mbc.Hdf5Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    public class RingBuffer : IDisposable
    {
        private const string CurrentWritePosAttrName = "wpos";

        private readonly Dictionary<string, H5DataSet> _dataSets = new Dictionary<string, H5DataSet>();
        private readonly string _ringBufferHd5Path;
        private readonly RingBufferInfo _ringBufferInfo;
        private H5File _h5File;
        private int _currentWritePos;

        public RingBuffer(string ringBufferHd5Path, RingBufferInfo ringBufferInfo)
        {
            _ringBufferHd5Path = ringBufferHd5Path;
            _ringBufferInfo = ringBufferInfo;

            OpenAndCheck();
        }

        public void Dispose()
        {
            _h5File?.Dispose();
        }

        // Testing purpose
        internal int CurrentWritePos => _currentWritePos;

        private void OpenAndCheck()
        {
            if (File.Exists(_ringBufferHd5Path))
            {
                _h5File = new H5File(_ringBufferHd5Path, H5File.Flags.ReadWrite);

                _currentWritePos = _h5File.Attributes().ReadInt(CurrentWritePosAttrName);

                var existingNames = _h5File.GetNames().ToList();

                foreach (var channelInfo in _ringBufferInfo.ChannelInfo)
                {
                    if (existingNames.Contains(channelInfo.Name))
                    {
                        var dataSet = H5DataSet.Open(_h5File, channelInfo.Name);
                        _dataSets.Add(channelInfo.Name, dataSet);
                    }
                    else
                    {
                        var dataSet = new H5DataSet.Builder()
                            .WithName(channelInfo.Name)
                            .WithType(channelInfo.Type)
                            .WithDimension(_ringBufferInfo.Size)
                            .WithChunking(_ringBufferInfo.ChunkSize)
                            .Create(_h5File);

                        _dataSets.Add(channelInfo.Name, dataSet);
                    }
                }
            }
            else
            {
                _h5File = new H5File(_ringBufferHd5Path, H5File.Flags.CreateOnly);

                // Struktur anlegen
                _h5File.Attributes().Write(CurrentWritePosAttrName, 0);
                _currentWritePos = 0;

                foreach (var channelInfo in _ringBufferInfo.ChannelInfo)
                {
                    var dataSet = new H5DataSet.Builder()
                        .WithName(channelInfo.Name)
                        .WithType(channelInfo.Type)
                        .WithDimension(_ringBufferInfo.Size)
                        .WithChunking(_ringBufferInfo.ChunkSize)
                        .Create(_h5File);

                    _dataSets.Add(channelInfo.Name, dataSet);
                }
            }
        }

        /// <summary>
        /// Schreibt die Werte eines Channels in den Ringbuffer an die
        /// aktuelle Schreibposition.
        /// </summary>
        public void WriteChannel(string channelName, Array values)
        {
            var dataSet = _dataSets[channelName];

            using (var targetDataSpace = dataSet.GetSpace())
            using (var sourceDataSpace = H5DataSpace.CreateSimpleFixed(new[] { (ulong)values.Length }))
            {
                var countPart1 = Math.Min(values.Length, _ringBufferInfo.Size - _currentWritePos);
                var countPart2 = values.Length - countPart1;

                H5DataSpace.CreateSelectionBuilder()
                    .Start((ulong)_currentWritePos)
                    .Count((ulong)countPart1)
                    .ApplyTo(targetDataSpace);

                H5DataSpace.CreateSelectionBuilder()
                    .Start(0)
                    .Count((ulong)countPart1)
                    .ApplyTo(sourceDataSpace);

                dataSet.Write(dataSet.ValueType, values, targetDataSpace, sourceDataSpace);

                // Umlauf des Ringbuffers, 2. Teil
                if (countPart2 > 0)
                {
                    H5DataSpace.CreateSelectionBuilder()
                        .Start(0)
                        .Count((ulong)countPart2)
                        .ApplyTo(targetDataSpace);

                    H5DataSpace.CreateSelectionBuilder()
                        .Start((ulong)countPart1)
                        .Count((ulong)countPart2)
                        .ApplyTo(sourceDataSpace);

                    dataSet.Write(dataSet.ValueType, values, targetDataSpace, sourceDataSpace);
                }
            }
        }

        public void IncrementSamples(int count)
        {
            _currentWritePos = (_currentWritePos + count) % _ringBufferInfo.Size;
            _h5File.Attributes().Write(CurrentWritePosAttrName, _currentWritePos);
        }
    }
}
