using System;
using System.Text;
using System.Runtime.InteropServices;

namespace BusDriver.Utils
{
    // copy of System.IO.StreamReader
    public class StreamReader : IDisposable
    {
        internal static int DefaultBufferSize = 1024;
        private const int MinBufferSize = 128;

        private System.IO.Stream _stream;
        private Encoding _encoding;
        private Decoder _decoder;
        private byte[] _byteBuffer;
        private char[] _charBuffer;
        private byte[] _preamble;
        private int _charPos;
        private int _charLen;
        private int _byteLen;
        private int _bytePos;
        private int _maxCharsPerBuffer;
        private bool _detectEncoding;
        private bool _checkPreamble;
        private bool _isBlocked;
        private bool _closable;

        internal StreamReader() { }

        public StreamReader(System.IO.Stream stream)
            : this(stream, true)
        {
        }

        public StreamReader(System.IO.Stream stream, bool detectEncodingFromByteOrderMarks)
            : this(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks, DefaultBufferSize, false)
        {
        }

        public StreamReader(System.IO.Stream stream, Encoding encoding)
            : this(stream, encoding, true, DefaultBufferSize, false)
        {
        }

        public StreamReader(System.IO.Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
            : this(stream, encoding, detectEncodingFromByteOrderMarks, DefaultBufferSize, false)
        {
        }

        public StreamReader(System.IO.Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
            : this(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, false)
        {
        }

        public StreamReader(System.IO.Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
        {
            if (stream == null || encoding == null)
                throw new ArgumentNullException(stream == null ? nameof(stream) : nameof(encoding));
            if (!stream.CanRead)
                throw new ArgumentException("Argument_System.IO.StreamNotReadable");
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "ArgumentOutOfRange_NeedPosNum");

            Init(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen);
        }

        private void Init(System.IO.Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize, bool leaveOpen)
        {
            _stream = stream;
            _encoding = encoding;
            _decoder = encoding.GetDecoder();
            if (bufferSize < MinBufferSize)
                bufferSize = MinBufferSize;

            _byteBuffer = new byte[bufferSize];
            _maxCharsPerBuffer = encoding.GetMaxCharCount(bufferSize);
            _charBuffer = new char[_maxCharsPerBuffer];
            _byteLen = 0;
            _bytePos = 0;
            _detectEncoding = detectEncodingFromByteOrderMarks;
            _preamble = encoding.GetPreamble();
            _checkPreamble = (_preamble.Length > 0);
            _isBlocked = false;
            _closable = !leaveOpen;
        }

        internal void Init(System.IO.Stream Stream)
        {
            this._stream = Stream;
            _closable = true;
        }

        public void Close()
        {
            Dispose(true);
        }

        protected void Dispose(bool disposing)
        {
            try
            {
                if (!LeaveOpen && disposing && (_stream != null))
                    _stream.Close();
            }
            finally
            {
                if (!LeaveOpen && _stream != null)
                {
                    _stream = null;
                    _encoding = null;
                    _decoder = null;
                    _byteBuffer = null;
                    _charBuffer = null;
                    _charPos = 0;
                    _charLen = 0;
                }
            }
        }

        public virtual Encoding CurrentEncoding
        {
            get { return _encoding; }
        }

        public virtual System.IO.Stream BaseStream
        {
            get { return _stream; }
        }

        internal bool LeaveOpen
        {
            get { return !_closable; }
        }

        public void DiscardBufferedData()
        {
            _byteLen = 0;
            _charLen = 0;
            _charPos = 0;

            if (_encoding != null)
            {
                _decoder = _encoding.GetDecoder();
            }
            _isBlocked = false;
        }

        public bool EndOfStream
        {
            get
            {
                if (_stream == null)
                    throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

                if (_charPos < _charLen)
                    return false;

                var numRead = ReadBuffer();
                return numRead == 0;
            }
        }

        public int Peek()
        {
            if (_stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            if (_charPos == _charLen)
            {
                if (_isBlocked || ReadBuffer() == 0) return -1;
            }
            return _charBuffer[_charPos];
        }

        public int Read()
        {
            if (_stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            if (_charPos == _charLen)
            {
                if (ReadBuffer() == 0) return -1;
            }
            int result = _charBuffer[_charPos];
            _charPos++;
            return result;
        }

        public int Read([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "ArgumentNull_Buffer");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? "index" : "count", "ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - index < count)
                throw new ArgumentException("Argument_InvalidOffLen");

            if (_stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            var charsRead = 0;

            var readToUserBuffer = false;
            while (count > 0)
            {
                var n = _charLen - _charPos;
                if (n == 0) n = ReadBuffer(buffer, index + charsRead, count, out readToUserBuffer);
                if (n == 0) break;
                if (n > count) n = count;
                if (!readToUserBuffer)
                {
                    Buffer.BlockCopy(_charBuffer, _charPos * 2, buffer, (index + charsRead) * 2, n * 2);
                    _charPos += n;
                }
                charsRead += n;
                count -= n;

                if (_isBlocked)
                    break;
            }

            return charsRead;
        }

        public int ReadBlock([In, Out] char[] buffer, int index, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer), "ArgumentNull_Buffer");
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException(index < 0 ? "index" : "count", "ArgumentOutOfRange_NeedNonNegNum");
            if (buffer.Length - index < count)
                throw new ArgumentException("Argument_InvalidOffLen");

            if (_stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            int i, n = 0;
            do
            {
                n += (i = Read(buffer, index + n, count - n));
            } while (i > 0 && n < count);
            return n;
        }

        public string ReadToEnd()
        {
            if (_stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            var sb = new StringBuilder(_charLen - _charPos);
            do
            {
                sb.Append(_charBuffer, _charPos, _charLen - _charPos);
                _charPos = _charLen;
                ReadBuffer();
            } while (_charLen > 0);
            return sb.ToString();
        }

        private void CompressBuffer(int n)
        {
            Buffer.BlockCopy(_byteBuffer, n, _byteBuffer, 0, _byteLen - n);
            _byteLen -= n;
        }

        private void DetectEncoding()
        {
            if (_byteLen < 2)
                return;
            _detectEncoding = false;
            var changedEncoding = false;
            if (_byteBuffer[0] == 0xFE && _byteBuffer[1] == 0xFF)
            {
                _encoding = new UnicodeEncoding(true, true);
                CompressBuffer(2);
                changedEncoding = true;
            }
            else if (_byteBuffer[0] == 0xFF && _byteBuffer[1] == 0xFE)
            {
                if (_byteLen < 4 || _byteBuffer[2] != 0 || _byteBuffer[3] != 0)
                {
                    _encoding = new UnicodeEncoding(false, true);
                    CompressBuffer(2);
                    changedEncoding = true;
                }
            }
            else if (_byteLen >= 3 && _byteBuffer[0] == 0xEF && _byteBuffer[1] == 0xBB && _byteBuffer[2] == 0xBF)
            {
                _encoding = Encoding.UTF8;
                CompressBuffer(3);
                changedEncoding = true;
            }
            else if (_byteLen == 2)
            {
                _detectEncoding = true;
            }

            if (changedEncoding)
            {
                _decoder = _encoding.GetDecoder();
                _maxCharsPerBuffer = _encoding.GetMaxCharCount(_byteBuffer.Length);
                _charBuffer = new char[_maxCharsPerBuffer];
            }
        }

        private bool IsPreamble()
        {
            if (!_checkPreamble)
                return _checkPreamble;

            var len = (_byteLen >= (_preamble.Length)) ? (_preamble.Length - _bytePos) : (_byteLen - _bytePos);
            for (var i = 0; i < len; i++, _bytePos++)
            {
                if (_byteBuffer[_bytePos] != _preamble[_bytePos])
                {
                    _bytePos = 0;
                    _checkPreamble = false;
                    break;
                }
            }

            if (_checkPreamble)
            {
                if (_bytePos == _preamble.Length)
                {
                    CompressBuffer(_preamble.Length);
                    _bytePos = 0;
                    _checkPreamble = false;
                    _detectEncoding = false;
                }
            }

            return _checkPreamble;
        }

        internal virtual int ReadBuffer()
        {
            _charLen = 0;
            _charPos = 0;

            if (!_checkPreamble)
                _byteLen = 0;
            do
            {
                if (_checkPreamble)
                {
                    var len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);

                    if (len == 0)
                    {
                        if (_byteLen > 0)
                        {
                            _charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);

                            _bytePos = _byteLen = 0;
                        }

                        return _charLen;
                    }

                    _byteLen += len;
                }
                else
                {
                    _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);

                    if (_byteLen == 0)
                        return _charLen;
                }

                _isBlocked = (_byteLen < _byteBuffer.Length);

                if (IsPreamble())
                    continue;

                if (_detectEncoding && _byteLen >= 2)
                    DetectEncoding();

                _charLen += _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, _charLen);
            } while (_charLen == 0);

            return _charLen;
        }

        private int ReadBuffer(char[] userBuffer, int userOffset, int desiredChars, out bool readToUserBuffer)
        {
            _charLen = 0;
            _charPos = 0;

            if (!_checkPreamble)
                _byteLen = 0;

            var charsRead = 0;
            readToUserBuffer = desiredChars >= _maxCharsPerBuffer;

            do
            {
                if (_checkPreamble)
                {
                    var len = _stream.Read(_byteBuffer, _bytePos, _byteBuffer.Length - _bytePos);

                    if (len == 0)
                    {
                        if (_byteLen > 0)
                        {
                            if (readToUserBuffer)
                            {
                                charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, userBuffer, userOffset + charsRead);
                                _charLen = 0;
                            }
                            else
                            {
                                charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, charsRead);
                                _charLen += charsRead;
                            }
                        }

                        return charsRead;
                    }

                    _byteLen += len;
                }
                else
                {
                    _byteLen = _stream.Read(_byteBuffer, 0, _byteBuffer.Length);
                    if (_byteLen == 0)
                        break;
                }

                _isBlocked = (_byteLen < _byteBuffer.Length);

                if (IsPreamble())
                    continue;

                if (_detectEncoding && _byteLen >= 2)
                {
                    DetectEncoding();

                    readToUserBuffer = desiredChars >= _maxCharsPerBuffer;
                }

                _charPos = 0;
                if (readToUserBuffer)
                {
                    charsRead += _decoder.GetChars(_byteBuffer, 0, _byteLen, userBuffer, userOffset + charsRead);
                    _charLen = 0;
                }
                else
                {
                    charsRead = _decoder.GetChars(_byteBuffer, 0, _byteLen, _charBuffer, charsRead);
                    _charLen += charsRead;
                }
            } while (charsRead == 0);

            _isBlocked &= charsRead < desiredChars;

            return charsRead;
        }

        public string ReadLine()
        {
            if (_stream == null)
                throw new ObjectDisposedException(null, "ObjectDisposed_ReaderClosed");

            if (_charPos == _charLen)
            {
                if (ReadBuffer() == 0)
                    return null;
            }

            StringBuilder sb = null;
            do
            {
                var i = _charPos;
                do
                {
                    var ch = _charBuffer[i];

                    if (ch == '\r' || ch == '\n')
                    {
                        string s;
                        if (sb != null)
                        {
                            sb.Append(_charBuffer, _charPos, i - _charPos);
                            s = sb.ToString();
                        }
                        else
                        {
                            s = new string(_charBuffer, _charPos, i - _charPos);
                        }
                        _charPos = i + 1;
                        if (ch == '\r' && (_charPos < _charLen || ReadBuffer() > 0))
                        {
                            if (_charBuffer[_charPos] == '\n') _charPos++;
                        }
                        return s;
                    }
                    i++;
                } while (i < _charLen);

                i = _charLen - _charPos;
                if (sb == null)
                    sb = new StringBuilder(i + 80);

                sb.Append(_charBuffer, _charPos, i);
            } while (ReadBuffer() > 0);
            return sb.ToString();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
