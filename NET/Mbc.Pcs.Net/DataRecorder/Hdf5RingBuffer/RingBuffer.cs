using EnsureThat;
using Mbc.Hdf5Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    public class RingBuffer : IDisposable
    {
        private const string CurrentWritePosAttrName = "wpos";
        private const string CurrentCountAttrName = "count";

        private readonly ReaderWriterLockSlim _hdf5Lock = new ReaderWriterLockSlim();
        private readonly Dictionary<string, H5DataSet> _dataSets = new Dictionary<string, H5DataSet>();
        private readonly string _ringBufferHd5Path;
        private readonly RingBufferInfo _ringBufferInfo;
        private H5File _h5File;

        /// <summary>
        /// Enthält die nächste Schreibposition von 0..size-1.
        /// </summary>
        private int _currentWritePos;

        /// <summary>
        /// Enthält die aktuelle Anzahl Samples von 0..size.
        /// </summary>
        private int _count;

        /// <summary>
        /// Enthält die Anzahl Samples, die geschrieben ober noch
        /// nicht commited worden sind.
        /// </summary>
        private int _uncommitedSamples;

        /// <summary>
        /// Enthält den Sample-Index des zuletzt geschriebenen Samples.
        /// Der Zähler zählt monoton alle Samples.
        /// </summary>
        private long _sampleIndex;

        public RingBuffer(string ringBufferHd5Path, RingBufferInfo ringBufferInfo)
        {
            _ringBufferHd5Path = ringBufferHd5Path;
            _ringBufferInfo = ringBufferInfo;

            OpenAndCheck();
        }

        public void Dispose()
        {
            foreach (var dataSet in _dataSets)
            {
                dataSet.Value.Dispose();
            }

            _h5File?.Dispose();
        }

        // Testing purpose
        internal int CurrentWritePos => _currentWritePos;

        // Testing purpose
        internal int Count => _count;

        public long LastSampleIndex => _sampleIndex;

        private void OpenAndCheck()
        {
            File.Delete(_ringBufferHd5Path);
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
                UpdateWritePos(0);
                UpdateCount(0);

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
            _hdf5Lock.EnterWriteLock();
            try
            {
                if (_uncommitedSamples != 0 && _uncommitedSamples != values.Length)
                {
                    throw new ArgumentException("Sample count must be same for single commit");
                }

                if (_uncommitedSamples == 0)
                {
                    _uncommitedSamples = values.Length;

                    if ((_ringBufferInfo.Size - _count) < _uncommitedSamples)
                    {
                        UpdateCount(_ringBufferInfo.Size - _uncommitedSamples);
                    }
                }

                var dataSet = _dataSets[channelName];

                using (var fileDataSpace = dataSet.GetSpace())
                using (var memoryDataSpace = H5DataSpace.CreateSimpleFixed(new[] { (ulong)values.Length }))
                {
                    var countPart1 = Math.Min(values.Length, _ringBufferInfo.Size - _currentWritePos);
                    var countPart2 = values.Length - countPart1;

                    H5DataSpace.CreateSelectionBuilder()
                        .Start((ulong)_currentWritePos)
                        .Count((ulong)countPart1)
                        .ApplyTo(fileDataSpace);

                    H5DataSpace.CreateSelectionBuilder()
                        .Start(0)
                        .Count((ulong)countPart1)
                        .ApplyTo(memoryDataSpace);

                    dataSet.Write(dataSet.ValueType, values, fileDataSpace, memoryDataSpace);

                    // Umlauf des Ringbuffers, 2. Teil
                    if (countPart2 > 0)
                    {
                        H5DataSpace.CreateSelectionBuilder()
                            .Start(0)
                            .Count((ulong)countPart2)
                            .ApplyTo(fileDataSpace);

                        H5DataSpace.CreateSelectionBuilder()
                            .Start((ulong)countPart1)
                            .Count((ulong)countPart2)
                            .ApplyTo(memoryDataSpace);

                        dataSet.Write(dataSet.ValueType, values, fileDataSpace, memoryDataSpace);
                    }
                }
            }
            finally
            {
                _hdf5Lock.ExitWriteLock();
            }
        }

        private void UpdateWritePos(int pos)
        {
            _currentWritePos = pos;
            _h5File.Attributes().Write(CurrentWritePosAttrName, _currentWritePos);
        }

        private void UpdateCount(int count)
        {
            _count = count;
            _h5File.Attributes().Write(CurrentCountAttrName, _count);
        }

        public long CommitWrite()
        {
            _hdf5Lock.EnterWriteLock();
            try
            {
                UpdateCount(_count + _uncommitedSamples);
                UpdateWritePos((_currentWritePos + _uncommitedSamples) % _ringBufferInfo.Size);
                _sampleIndex += _uncommitedSamples;
                return _sampleIndex;
            }
            finally
            {
                _hdf5Lock.ExitWriteLock();
            }
        }

        public int ReadChannel(string channelName, long startSampleIndex, Array values)
        {
            return ReadChannel(channelName, startSampleIndex, values, 0, values.Length);
        }

        /// <summary>
        /// Liest Daten eines Kanals aus. Der erste Sample wird über startSampleIndex
        /// referenziert. Können weniger Daten gelesen
        /// als das Array gross ist, werden diese rechtsbündig abgelegt. Die
        /// Methode liefert die Anzahl Samples im Array zurück:
        /// </summary>
        public int ReadChannel(string channelName, long startSampleIndex, Array values, int offset, int count)
        {
            EnsureArg.IsGte(offset, 0, nameof(offset));
            EnsureArg.IsGte(count, 0, nameof(offset));
            EnsureArg.IsTrue(offset + count <= values.Length, null, optsFn: x => x.WithMessage("offset/count does not match values."));

            _hdf5Lock.EnterReadLock();
            try
            {
                EnsureArg.IsLte(startSampleIndex, _sampleIndex, nameof(startSampleIndex));

                // Start-Index im Array
                var valuesStart = offset;

                // Anzahl im Array
                var valuesCount = count;

                // Offset zum Array-Anfang (pos. Wert)
                var leftOffset = _sampleIndex - startSampleIndex;

                // Offset ist grösser als verfügbare Samples
                if (leftOffset >= _count)
                {
                    var newLeftOffset = _count - 1;
                    valuesStart += (int)(leftOffset - newLeftOffset);
                    valuesCount -= valuesStart;
                    leftOffset = newLeftOffset;
                }

                // Shortcut: Wenn nichts gelesen werden kann wird hier beendet
                if (valuesCount <= 0)
                    return valuesCount;

                var dataSet = _dataSets[channelName];

                using (var fileDataSpace = dataSet.GetSpace())
                using (var memoryDataSpace = H5DataSpace.CreateSimpleFixed(new[] { (ulong)values.Length }))
                {
                    var startPart1 = _currentWritePos - leftOffset - 1;
                    if (startPart1 < 0)
                    {
                        startPart1 = _ringBufferInfo.Size + startPart1;
                    }

                    var countPart1 = Math.Min(valuesCount, _ringBufferInfo.Size - startPart1);

                    var startPart2 = (startPart1 + countPart1) % _ringBufferInfo.Size;
                    var countPart2 = valuesCount - countPart1;

                    H5DataSpace.CreateSelectionBuilder()
                        .Start((ulong)startPart1)
                        .Count((ulong)countPart1)
                        .ApplyTo(fileDataSpace);

                    H5DataSpace.CreateSelectionBuilder()
                        .Start((ulong)valuesStart)
                        .Count((ulong)countPart1)
                        .ApplyTo(memoryDataSpace);

                    dataSet.Read(values, fileDataSpace, memoryDataSpace);

                    if (countPart2 > 0)
                    {
                        H5DataSpace.CreateSelectionBuilder()
                            .Start((ulong)startPart2)
                            .Count((ulong)countPart2)
                            .ApplyTo(fileDataSpace);

                        H5DataSpace.CreateSelectionBuilder()
                            .Start((ulong)(valuesStart + countPart1))
                            .Count((ulong)countPart2)
                            .ApplyTo(memoryDataSpace);

                        dataSet.Read(values, fileDataSpace, memoryDataSpace);
                    }
                }

                return valuesCount;
            }
            finally
            {
                _hdf5Lock.ExitReadLock();
            }
        }
    }
}
