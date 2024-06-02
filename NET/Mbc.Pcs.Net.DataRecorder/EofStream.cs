using EnsureThat;
using System;
using System.IO;

namespace Mbc.Pcs.Net.DataRecorder
{
    /// <summary>
    /// Ein Stream-Wrapper um festzustellen, ob das EOF eines Streams
    /// erreicht wurde ohne zu lesen.
    /// <para>Normalerweise kann dies auch über einen Vergleich der
    /// Eigenschaften <see cref="Stream.Position"/> und
    /// <see cref="Stream.Length"/> erreicht werden. Allerdings werden diese
    /// Properties nicht bei allen Streams wie z.B.
    /// see System.IO.CompressionDeflateStream unterstützt.</para>
    /// </summary>
    public class EofStream : Stream
    {
        private readonly Stream _stream;
        private bool _eof;
        private bool _peeked;
        private byte _peek;

        public EofStream(Stream stream)
        {
            Ensure.Bool.IsTrue(stream.CanRead);
            _stream = stream;
        }

        protected override void Dispose(bool disposing)
        {
            _stream.Dispose();
        }

        public bool IsEof
        {
            get
            {
                if (_eof)
                    return true;

                if (_peeked)
                    return false;

                var value = _stream.ReadByte();
                if (value == -1)
                {
                    _eof = true;
                    return true;
                }

                _peeked = true;
                _peek = (byte)value;
                return false;
            }
        }

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            _stream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count == 0)
                return 0;

            int read = 0;

            if (_peeked)
            {
                buffer[offset] = _peek;
                _peeked = false;
                read++;
            }

            read += _stream.Read(buffer, offset + read, count - read);
            if (read < count)
            {
                _eof = true;
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
