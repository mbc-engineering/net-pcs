using EnsureThat;
using Mbc.Hdf5Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ExtLogging = Microsoft.Extensions.Logging;

namespace Mbc.Pcs.Net.DataRecorder.Hdf5RingBuffer
{
    public class RingBuffer : IDisposable, IDataChannelWriter
    {
        private const string CurrentWritePosAttrName = "wpos";
        private const string CurrentCountAttrName = "count";
        private const string OversamplingFactorAttrName = "OversamplingFactor";

        // Alle Zugriffe auf HDF5-Lib müssen gelockt werden
        private readonly object _hdf5Lock = new object();
        private readonly Dictionary<string, H5DataSet> _dataSets = new Dictionary<string, H5DataSet>();
        private readonly string _ringBufferHd5Path;
        private readonly RingBufferInfo _ringBufferInfo;
        private readonly ExtLogging.ILogger _logger;
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

        public RingBuffer(string ringBufferHd5Path, RingBufferInfo ringBufferInfo, ExtLogging.ILogger logger)
        {
            _ringBufferHd5Path = ringBufferHd5Path;
            _ringBufferInfo = ringBufferInfo;
            _logger = logger;
            ExecuteAndLogDuration("OpenAndCheck()", OpenAndCheck);
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
                    _logger.LogDebug("Opening Ringbuffer File '{file}'.", _ringBufferHd5Path);
                    _h5File = new H5File(_ringBufferHd5Path, H5File.Flags.ReadWrite);

                    _currentWritePos = _h5File.Attributes().ReadInt(CurrentWritePosAttrName);
                    if (_currentWritePos < 0 || _currentWritePos >= _ringBufferInfo.Size)
                    {
                        _logger.LogWarning("Invalid write position {writePos}.", _currentWritePos);
                        UpdateWritePos(0);
                        UpdateCount(0);
                    }

                    _count = _h5File.Attributes().ReadInt(CurrentCountAttrName);
                    if (_count > _ringBufferInfo.Size)
                    {
                        _logger.LogWarning("Invalid size {_count}.", _count);
                        UpdateCount(_ringBufferInfo.Size);
                    }

                    var existingNames = _h5File.GetNames().ToList();

                    foreach (var channelInfo in _ringBufferInfo.ChannelInfo)
                    {
                        if (existingNames.Contains(channelInfo.Name))
                        {
                            var dataSet = H5DataSet.Open(_h5File, channelInfo.Name);

                            _dataSets.Add(channelInfo.Name, dataSet);

                            if (dataSet.ValueType != channelInfo.Type)
                            {
                                _logger.LogError("Error reopening hdf5 ring buffer: channel type changed {oldType} != {newType} on channel {channelName}.", dataSet.ValueType, channelInfo.Type, channelInfo.Name);
                                success = false;
                                break;
                            }

                            var dim = dataSet.GetDimensions();
                            if (dim.Length != 1 || dim[0] != (ulong)_ringBufferInfo.Size)
                            {
                                _logger.LogError("Error reopening hdf5 ring buffer: channel dimension is invalid on channel {channelName}.", channelInfo.Name);
                                success = false;
                                break;
                            }

                            var chunk = dataSet.GetChunkSize();
                            if (chunk.Length != 1 || chunk[0] != (ulong)_ringBufferInfo.ChunkSize)
                            {
                                _logger.LogError("Error reopening hdf5 ring buffer: channel chunk size is invalid on channel {channelName}.", channelInfo.Name);
                                success = false;
                                break;
                            }

                            /* ToDo: .GetAttributeNames() is very slow operation for large files when application coldstarts. Change check for OversamplingFactorAttrName
                            var swReadAttributeNames = Stopwatch.StartNew();
                            var attributeNames = dataSet.Attributes().GetAttributeNames().ToList();
                            _logger.LogDebug($"Read Dataset attributenames in {swReadAttributeNames.Elapsed.TotalMilliseconds}MS.");

                            if (channelInfo.OversamplingFactor > 1)
                            {
                                _logger.LogDebug($"enter oversampling channel {channelInfo.Name}.");

                                if (!attributeNames.Contains(OversamplingFactorAttrName) || dataSet.Attributes().ReadInt(OversamplingFactorAttrName) != channelInfo.OversamplingFactor)
                                {
                                    _logger.LogError("Error reopening hdf5 ring buffer: oversampling factor does not match on channel {channelName}", channelInfo.Name);
                                    success = false;
                                    break;
                                }
                            }
                            else
                            {
                                if (attributeNames.Contains(OversamplingFactorAttrName))
                                {
                                    _logger.LogError("Error reopening hdf5 ring buffer: channel {channelName} is not oversampled.", channelInfo.Name);
                                    success = false;
                                    break;
                                }
                            }
                            */
                        }
                        else
                        {
                            _logger.LogError("Error reopening hdf5 ring buffer: missing channel {channelName}.", channelInfo.Name);
                            success = false;
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error reopening hdf5 ring buffer.");
                    success = false;
                }

                if (success)
                {
                    _logger.LogInformation("Using existing hdf5 ring buffer with {count} samples.", _count);
                    return;
                }

                foreach (var dataSet in _dataSets.Values)
                {
                    try
                    {
                        _logger.LogDebug($"Dispose dataset.");
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
            _logger.LogInformation("Creating new hdf5 ring buffer ({path}).", _ringBufferHd5Path);

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

                    if (_ringBufferInfo.Size - _count < _uncommitedSamples)
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
                _uncommitedSamples = 0;
                return _sampleIndex;
            }
        }

        public int ReadChannel(string channelName, long startSampleIndex, Array values)
        {
            return ReadChannel(channelName, startSampleIndex, values, 0, values.Length);
        }

        public int ReadChannel(string channelName, long startSampleIndex, Array values, int offset, int count)
        {
#if true
            return ReadChannel(channelName, startSampleIndex, values, offset, count, 1);
#else
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

                if (valuesCount > 0)
                {
                    ReadChannelInternal(channelName, _sampleIndex - leftOffset, values, valuesStart, valuesCount, 1);
                }

                return valuesCount;
            }
#endif
        }

        /// <summary>
        /// Liest Daten eines Kanals aus. Der erste Sample wird über startSampleIndex
        /// referenziert. Können weniger Daten gelesen
        /// als das Array gross ist, werden diese rechtsbündig abgelegt. Die
        /// Methode liefert die Anzahl Samples im Array zurück.
        /// </summary>
        /// <param name="channelName">Der Name des Kanals, der gelesen werden soll.</param>
        /// <param name="startSampleIndex">Der SampleIndex des ersten zu lesenden Samples.</param>
        /// <param name="values">Ein vordefiniertes Array, in dem die Daten geschrieben werden.</param>
        /// <param name="offset">Offset im Array, ab dem die Daten geschrieben werden.</param>
        /// <param name="count">Anzahl Daten, die in das Array geschrieben werden.</param>
        /// <param name="stride">Schrittgrösse zwischen den Samples (1=jedes Sample, 2=jedes 2. Sample, usw.)</param>
        public int ReadChannel(string channelName, long startSampleIndex, Array values, int offset, int count, int stride)
        {
            EnsureArg.IsGte(offset, 0, nameof(offset));
            EnsureArg.IsGte(count, 0, nameof(offset));
            EnsureArg.IsTrue(offset + count <= values.Length, null, optsFn: x => x.WithMessage("offset/count does not match values."));
            EnsureArg.IsGte(stride, 1, nameof(stride));

            lock (_hdf5Lock)
            {
                // Es sollen samples gelesen werden, die noch nicht geschrieben wurden
                if (startSampleIndex > _sampleIndex)
                    return 0;

                // Prüfen ob der startSampleIndex vorhanden ist, ggf. anpassen
                var actualStartSampleIndex = startSampleIndex;
                if ((_sampleIndex - startSampleIndex + 1) > _count)
                {
                    actualStartSampleIndex = _sampleIndex - _count + 1;
                }

                // Array parameter anpassen
                var actualOffset = offset;
                var actualCount = count;

                // Korrekt aufgrund eines geänderten Start-Samples
                if (startSampleIndex != actualStartSampleIndex)
                {
                    var missingSamples = (int)((actualStartSampleIndex - startSampleIndex + stride - 1) / stride);
                    actualOffset += missingSamples;
                    actualCount -= missingSamples;
                }

                // Korrektur aufgrund zu wenig Daten
                var availableSamples = (int)(Math.Min(_sampleIndex - actualStartSampleIndex + 1, _count) + stride - 1) / stride;
                if (availableSamples < actualCount)
                {
                    var missingSamples = (int)(actualCount - availableSamples);
                    actualOffset += missingSamples;
                    actualCount -= missingSamples;
                }

                if (actualCount > 0)
                {
                    ReadChannelInternal(channelName, actualStartSampleIndex, values, actualOffset, actualCount, stride);
                }

                return actualCount;
            }
        }

        private void ReadChannelInternal(string channelName, long startSampleIndex, Array values, int offset, int count, int stride)
        {
            /* alle Argumente wurden beim Aufruf vorgängig bereinigt */

            var dataSet = _dataSets[channelName];

            using (var fileDataSpace = dataSet.GetSpace())
            using (var memoryDataSpace = H5DataSpace.CreateSimpleFixed(new[] { (ulong)values.Length }))
            {
                var startPart1 = _currentWritePos - (_sampleIndex - startSampleIndex) - 1;
                if (startPart1 < 0)
                {
                    startPart1 = _ringBufferInfo.Size + startPart1;
                }

                var countPart1 = Math.Min(count, (_ringBufferInfo.Size - startPart1 + stride - 1) / stride);

                var startPart2 = (startPart1 + (countPart1 * stride)) % _ringBufferInfo.Size;
                var countPart2 = count - countPart1;

                if (stride > 1)
                {
                    fileDataSpace.Select(
                        new[] { (ulong)startPart1 },
                        new[] { (ulong)countPart1 },
                        new[] { (ulong)stride },
                        new[] { 1UL });
                }
                else
                {
                    fileDataSpace.Select(new[] { (ulong)startPart1 }, new[] { (ulong)countPart1 });
                }

                H5DataSpace.CreateSelectionBuilder()
                    .Start((ulong)offset)
                    .Count((ulong)countPart1)
                    .ApplyTo(memoryDataSpace);

                dataSet.Read(values, fileDataSpace, memoryDataSpace);

                if (countPart2 > 0)
                {
                    if (stride > 1)
                    {
                        fileDataSpace.Select(
                            new[] { (ulong)startPart2 },
                            new[] { (ulong)countPart2 },
                            new[] { (ulong)stride },
                            new[] { 1UL });
                    }
                    else
                    {
                        fileDataSpace.Select(new[] { (ulong)startPart2 }, new[] { (ulong)countPart2 });
                    }

                    H5DataSpace.CreateSelectionBuilder()
                        .Start((ulong)(offset + countPart1))
                        .Count((ulong)countPart2)
                        .ApplyTo(memoryDataSpace);

                    dataSet.Read(values, fileDataSpace, memoryDataSpace);
                }
            }
        }

        /// <summary>
        /// Liest alle Kanäle ausgehend vom angegeben Start-Sample bis die angegebene Anzahl Samples
        /// erreicht wurde oder keine Samples mehr verfügbar sind.
        /// </summary>
        /// <param name="startSampleIndex">Das Start-Sample, ab dem die Kanäle ausgegeben werden.</param>
        /// <param name="count">Die max. Anzahl an Samples die gelesen wurden.</param>
        /// <param name="readCallback">Eine Methode die für jeden Kanal zusammen mit den Samples aufgerufen wird.</param>
        /// <returns>Die tatsächliche Anzahl gelesener Samples.</returns>
        public int ReadAllChannels(long startSampleIndex, int count, Action<string, Array> readCallback)
        {
            return 0;
        }

        private void ExecuteAndLogDuration(string actionMessage, Action act)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                var sw = Stopwatch.StartNew();
                act();
                sw.Stop();
                _logger.LogTrace("{action} lasts {duration}.", actionMessage, sw.Elapsed);
            }
            else
            {
                act();
            }
        }
    }
}
