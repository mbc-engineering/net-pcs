using EnsureThat;
using Mbc.Hdf5Utils;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    public class RingBuffer : IDisposable, IDataChannelWriter
    {
        private const string CurrentWritePosAttrName = "wpos";
        private const string CurrentCountAttrName = "count";
        private const string OversamplingFactorAttrName = "OversamplingFactor";

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        // Alle Zugriffe auf HDF5-Lib müssen gelockt werden
        private readonly object _hdf5Lock = new object();
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
            if (File.Exists(_ringBufferHd5Path))
            {
                var success = true;
                try
                {
                    _h5File = new H5File(_ringBufferHd5Path, H5File.Flags.ReadWrite);

                    _currentWritePos = _h5File.Attributes().ReadInt(CurrentWritePosAttrName);
                    _count = _h5File.Attributes().ReadInt(CurrentCountAttrName);

                    var existingNames = _h5File.GetNames().ToList();

                    foreach (var channelInfo in _ringBufferInfo.ChannelInfo)
                    {
                        if (existingNames.Contains(channelInfo.Name))
                        {
                            var dataSet = H5DataSet.Open(_h5File, channelInfo.Name);
                            _dataSets.Add(channelInfo.Name, dataSet);

                            if (dataSet.ValueType != channelInfo.Type)
                            {
                                _logger.Error("Error reopening hdf5 ring buffer: channel type changed {oldType} != {newType} on channel {channelName}", dataSet.ValueType, channelInfo.Type, channelInfo.Name);
                                success = false;
                                break;
                            }

                            if (channelInfo.OversamplingFactor > 1)
                            {
                                if (!dataSet.Attributes().GetAttributeNames().Contains(OversamplingFactorAttrName) || dataSet.Attributes().ReadInt(OversamplingFactorAttrName) != channelInfo.OversamplingFactor)
                                {
                                    _logger.Error("Error reopening hdf5 ring buffer: oversampling factor does not match on channel {channelName}", channelInfo.Name);
                                    success = false;
                                    break;
                                }
                            }
                            else
                            {
                                if (dataSet.Attributes().GetAttributeNames().Contains(OversamplingFactorAttrName))
                                {
                                    _logger.Error("Error reopening hdf5 ring buffer: channel {channelName} is not oversampled.", channelInfo.Name);
                                    success = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            _logger.Error("Error reopening hdf5 ring buffer: missing channel {channelName}.", channelInfo.Name);
                            success = false;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error reopening hdf5 ring buffer.");
                    success = false;
                }

                if (success)
                {
                    _logger.Info("Using existing hdf5 ring buffer with {count} samples.", _count);
                    return;
                }

                foreach (var dataSet in _dataSets.Values)
                {
                    try
                    {
                        dataSet.Dispose();
                    }
                    catch
                    {
                        // kein Error-Handling
                    }
                }

                try
                {
                    _h5File?.Dispose();
                }
                catch
                {
                    // kein Error-Handling
                }

                File.Delete(_ringBufferHd5Path);
            }

            CreateNewHdf5File();
        }

        private void CreateNewHdf5File()
        {
            _logger.Info("Creating new hdf5 ring buffer ({path}).", _ringBufferHd5Path);

            _h5File = new H5File(_ringBufferHd5Path, H5File.Flags.CreateOnly);

            // Struktur anlegen
            UpdateWritePos(0);
            UpdateCount(0);

            _dataSets.Clear();
            foreach (var channelInfo in _ringBufferInfo.ChannelInfo)
            {
                var dataSet = new H5DataSet.Builder()
                    .WithName(channelInfo.Name)
                    .WithType(channelInfo.Type)
                    .WithDimension(_ringBufferInfo.Size * channelInfo.OversamplingFactor)
                    .WithChunking(_ringBufferInfo.ChunkSize * channelInfo.OversamplingFactor)
                    .Create(_h5File);

                if (channelInfo.OversamplingFactor > 1)
                {
                    dataSet.Attributes().Write(OversamplingFactorAttrName, channelInfo.OversamplingFactor);
                    throw new NotImplementedException("Oversampling-Logging is not yet implemented.");
                }

                _dataSets.Add(channelInfo.Name, dataSet);
            }
        }

        /// <summary>
        /// Schreibt die Werte eines Channels in den Ringbuffer an die
        /// aktuelle Schreibposition.
        /// </summary>
        public void WriteChannel(string channelName, Array values)
        {
            lock (_hdf5Lock)
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

        /// <summary>
        /// Bestätigt die vorher mit <see cref="WriteChannel(string, Array)"/> geschriebenen
        /// Samples und macht diese zum Lesen verfügbar. Liefert den Sampleindex des zu
        /// letzt geschriebenen Samples zurück.
        /// </summary>
        public long CommitWrite()
        {
            lock (_hdf5Lock)
            {
                UpdateCount(_count + _uncommitedSamples);
                UpdateWritePos((_currentWritePos + _uncommitedSamples) % _ringBufferInfo.Size);
                _sampleIndex += _uncommitedSamples;
                return _sampleIndex;
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

            lock (_hdf5Lock)
            {
                if (startSampleIndex > _sampleIndex)
                    return 0;

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
        }
    }
}
