//-----------------------------------------------------------------------------
// Copyright (c) 2019 by mbc engineering GmbH, CH-6015 Luzern
// Licensed under the Apache License, Version 2.0
//-----------------------------------------------------------------------------

using EnsureThat;
using Mbc.AsyncUtils;
using Mbc.Common.IO;
using Optional.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;

namespace Mbc.Pcs.Net.DataRecorder
{
    /// <summary>
    /// Verwaltet serialisierbare Objekt in einem File-Buffer als Backend.
    /// <p>Mit dieser Klasse können serialisierbare Objekte in einem Ring-Buffer
    /// persistiert werden. Der Ringbuffer ist organisiert in Segmente. Jedes
    /// Segment hat eine max. Anzahl an Objekten und wird als eigenes File
    /// persisitert.</p>
    /// <p>Die max. Anzahl der Segmente kann begrenzt werden. Wird diese überschritten,
    /// wird das älteste Segment gelöscht.</p>
    /// <p>Die beiden Methoden <see cref="AppendData(object)"/> und
    /// <see cref="ReadData(int, int)"/> sind zueinander Thread-Safe. Bestimmte
    /// Operationen können zu kurzzeiten gegenseiten blockieren führen. Es gilt,
    /// dass die Daten die mit <see cref="AppendData(object)"/> geschrieben
    /// worden sind, sofort mit <see cref="ReadData(int, int)"/> gelesen werden
    /// können.</p>
    /// </summary>
    public class FileRingBuffer : IDisposable
    {
        /*
            # Implementierungsdetails

            Alle zu serialisierbaren Objekte werden zunächst im Memory serialisiert
            bis die max. Anzahl erreicht wird. Dann wird das komplete Segment als
            Datei geschrieben.

            Jedes Segment enthält einen Einträg in der `_segments`-List. Neben der
            Sequenznummer kann hier auch der interne Start-Index mit abgelegt werden.
            Dieser wird bei neu geschriebenen Segmenten automatisch hinzugefügt, bei
            bereits vorhandenen Segmente nur bei Bedarf bestimmt.

            Der interne Index ist eine Abstraktion um zur Laufzeit ein Objekt eindeutig
            und abhängig vom konkurrierenden Zugriffen referenzieren zu können. Der Index
            ist so definiert, dass das erste geschriebene Objekt *zur Laufzeit* den
            Index 0 enthält. Jedes weitere Objekt inkrementiert den Index. Der Index kann
            auch negativ werden, sofern bereits von einem vorherigen Lauf Daten geschrieben
            worden sind.

            Der interne `MemoryStream`, der die serialisierten Daten bis zum Schreiben in
            die Datei zwischenspeichert, wird zu beginn dyamisch angelegt mit einer
            Initialgrösse von 8K. Nachdem ein Segment geschrieben wurde, wird das nächste
            Segment mit der gleichen Grösse (Capacity) angelegt, mit der Annahme, das die
            Segmente ungefähr gleich gross werden und damit der Buffer nicht ständig
            vergrössert werden muss.

            Der binäre Serialisierungsmechanismus scheint nicht immer optimal zu sein, daher
            wird die Persistierung über die Schnittstelle `IObjectPersister` abstrahiert.

            Es wird garantiert, das Daten, die mit `AppendData()` geschrieben worden sind
            sofort mit `ReadData()` gelesen werden können. Daher werden alle IO-Zugriffe
            in `AppendData()` (kritischer Pfad) auf das Dateisystem asynchron ausgeführt.

            Die Filezugriffe im Hintergrund (Abspeichern der Daten, Löschen von alten
            Files) müssen serialisiert ausgeführt werden, damit die Reihenfolge der
            Operationen eingehaltet wird. Dies ist besonders beim Speichern und Löschen
            wichtig, damit es im Extremfall nicht dazu kommt, dass ein noch nicht geschriebenes
            File gelöscht wird.
        */

        private readonly AsyncSerializedTaskExecutor _backgroundExecutor = new AsyncSerializedTaskExecutor();
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private readonly List<SegmentInfo> _segments = new List<SegmentInfo>();
        private readonly string _baseDirectory;
        private readonly int _maxSegmentSize;
        private readonly int _maxSegments;
        private readonly IObjectPersister _persister;
        private bool _disposed;
        private long _nextIndex;
        private ulong _currentSequence;
        private int _segmentCount;
        private AppendableSegmentBuffer _currentBuffer;

        public FileRingBuffer(string baseDirectory, int maxSegmentSize, int maxSegments, IObjectPersister persister = null)
        {
            Ensure.Comparable.IsGt(maxSegmentSize, 0, nameof(maxSegmentSize));
            Ensure.Comparable.IsGt(maxSegments, 1, nameof(maxSegments));
            if (!Directory.Exists(baseDirectory))
                throw new ArgumentException("Directory must exists.", nameof(baseDirectory));

            _baseDirectory = baseDirectory;
            _maxSegmentSize = maxSegmentSize;
            _maxSegments = maxSegments;
            _persister = persister ?? new SerializationObjectPersister();
            Init();
        }

        public void Dispose()
        {
            _disposed = true;

            if (_segmentCount > 0)
            {
                FlushSegment(createNewSegment: false);
            }

            _backgroundExecutor.WaitForExecution();
            _backgroundExecutor.Dispose();
        }

        private static string SequenceToFilename(ulong sequence) => sequence.ToString("X16");

        private void Init()
        {
            // alle vorhandenen Daten-Files erfassen
            var sequence = Directory.EnumerateFiles(_baseDirectory)
                .Select(x => Path.GetFileName(x))
                .Select(x => Convert.ToUInt64(x, 16))
                .OrderBy(x => x)
                .Select(x => new SegmentInfo(x, Path.Combine(_baseDirectory, SequenceToFilename(x))));

            _segments.AddRange(sequence);

            // nächste Sequenz-Nummer bestimmen
            _currentSequence = _segments.Select(x => x.Sequence + 1).DefaultIfEmpty(0UL).Last();

            _currentBuffer = new AppendableSegmentBuffer(Path.Combine(_baseDirectory, SequenceToFilename(_currentSequence)), 8192);
            _segments.Add(new SegmentInfo(_currentSequence, _nextIndex, _currentBuffer));
        }

        public void AppendData(object data)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileRingBuffer));

            _lock.EnterWriteLock();
            try
            {
                _persister.Serialize(data, _currentBuffer.AppendableStream);
                _segmentCount++;
                _nextIndex++;

                if (_segmentCount >= _maxSegmentSize)
                {
                    FlushSegment(createNewSegment: true);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        private void FlushSegment(bool createNewSegment = false)
        {
            _currentSequence++;
            _segmentCount = 0;

            var oldBuffer = _currentBuffer;

            if (createNewSegment)
            {
                _currentBuffer = new AppendableSegmentBuffer(Path.Combine(_baseDirectory, SequenceToFilename(_currentSequence)), _currentBuffer.Capacaity);
                _segments.Add(new SegmentInfo(_currentSequence, _nextIndex, _currentBuffer));
            }
            else
            {
                _currentBuffer = null;
            }

            _backgroundExecutor.Execute(() => oldBuffer.Flush());

            // +1 => das aktuelle Segment zählt nicht mit
            while (_segments.Count > (_maxSegments + 1))
            {
                var segment = _segments[0];
                _segments.RemoveAt(0);
                _backgroundExecutor.Execute(() => File.Delete(Path.Combine(_baseDirectory, SequenceToFilename(segment.Sequence))));
            }
        }

        public IEnumerable<object> ReadData(int start, int count = int.MaxValue)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(FileRingBuffer));

            int remainingCount = count;

            // start auf index abgleichen
            long currentIndex;
            _lock.EnterReadLock();
            try
            {
                currentIndex = _nextIndex - start - 1;
            }
            finally
            {
                _lock.ExitReadLock();
            }

            while (remainingCount > 0)
            {
                List<object> segmentData;
                _lock.EnterReadLock();
                try
                {
                    segmentData = ReadSegment(currentIndex);
                }
                finally
                {
                    _lock.ExitReadLock();
                }

                if (segmentData.Count == 0)
                {
                    // keine weiteren Daten vorhanden
                    break;
                }

                foreach (var data in segmentData)
                {
                    remainingCount--;
                    currentIndex--;
                    yield return data;

                    if (remainingCount == 0)
                    {
                        break;
                    }
                }
            }
        }

        private List<object> ReadSegment(long index)
        {
            foreach (var segment in _segments.AsEnumerable().Reverse())
            {
                if (!segment.HasStartIndex)
                {
                    var data = UpdateStartIndex(segment);
                    if (segment.StartIndex <= index)
                    {
                        // Die Daten wurden schon bereits für den Start-Index
                        // gelesen und können daher direkt wiederverwendet werden
                        return data;
                    }
                }
                else
                {
                    if (segment.StartIndex <= index)
                    {
                        using (var eofStream = new EofStream(segment.Buffer.OpenReadableStream()))
                        {
                            var data = ReadSegmentData(eofStream, (int)(index - segment.StartIndex + 1));
                            return data;
                        }
                    }
                }
            }

            return new List<object>();
        }

        private List<object> UpdateStartIndex(SegmentInfo segmentInfo)
        {
            var nextStartIndex = _segments
                .SkipWhile(x => x != segmentInfo)
                .Skip(1)
                .Select(x => x.StartIndex)
                .First(); // es muss immer einen geben

            List<object> data;
            using (var eofStream = new EofStream(segmentInfo.Buffer.OpenReadableStream()))
            {
                data = ReadSegmentData(eofStream, int.MaxValue);
            }

            segmentInfo.StartIndex = nextStartIndex - data.Count;

            return data;
        }

        private List<object> ReadSegmentData(EofStream stream, int maxCount)
        {
            var data = new List<object>(_maxSegmentSize);
            while (data.Count < maxCount && !stream.IsEof)
            {
                data.Add(_persister.Deserialize(stream));
            }

            data.Reverse();
            return data;
        }

        /// <summary>
        /// Enthält Informationen zu einem Segment.
        /// </summary>
        private class SegmentInfo
        {
            private long _startIndex;

            public SegmentInfo(ulong sequence, string filename)
            {
                Sequence = sequence;
                Buffer = new SegmentBuffer(filename);
            }

            public SegmentInfo(ulong sequence, long startIndex, SegmentBuffer buffer)
            {
                Sequence = sequence;
                StartIndex = startIndex;
                Buffer = buffer;
            }

            /// <summary>
            /// Sequenz-Nummer des Segments.
            /// </summary>
            public ulong Sequence { get; private set; }

            public SegmentBuffer Buffer { get; }

            /// <summary>
            /// Interner Start-Index des Segments, sofern gesetzt. Muss
            /// über <see cref="HasStartIndex"/> abgefragt werden.
            /// </summary>
            public long StartIndex
            {
                get
                {
                    if (!HasStartIndex)
                        throw new InvalidOperationException("No start index set yet.");

                    return _startIndex;
                }
                set
                {
                    _startIndex = value;
                    HasStartIndex = true;
                }
            }

            public bool HasStartIndex { get; private set; }
        }

        private class SegmentBuffer
        {
            public SegmentBuffer(string filename)
            {
                Filename = filename;
            }

            protected string Filename { get; }

            public virtual Stream OpenReadableStream()
            {
                return new DeflateStream(new FileStream(Filename, FileMode.Open, FileAccess.Read), CompressionMode.Decompress);
            }
        }

#pragma warning disable CA1001 // Types that own disposable fields should be disposable
        private class AppendableSegmentBuffer : SegmentBuffer
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
        {
            private volatile MemoryStream _buffer;
            private volatile bool _appendFinished;

            public AppendableSegmentBuffer(string filename, int capacity)
                : base(filename)
            {
                _buffer = new MemoryStream(capacity);
            }

            public int Capacaity => _buffer.Capacity;

            public Stream AppendableStream
            {
                get
                {
                    if (_appendFinished)
                        throw new InvalidOperationException("Buffer is finished for appending.");
                    return _buffer;
                }
            }

            public void Flush()
            {
                _appendFinished = true;
                _buffer.Position = 0;

                using (var stream = new FileStream(Filename, FileMode.CreateNew, FileAccess.Write))
                using (var zipStream = new DeflateStream(stream, CompressionLevel.Fastest))
                {
                    _buffer.CopyTo(zipStream);
                }

                _buffer = null;
            }

            public override Stream OpenReadableStream()
            {
                var buffer = _buffer;
                if (buffer != null)
                {
                    return new MemoryStream(_buffer.GetBuffer(), 0, (int)_buffer.Length);
                }

                return base.OpenReadableStream();
            }
        }
    }
}
