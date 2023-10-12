#region License
/*
 * ResponseStream.cs
 *
 * This code is derived from ResponseStream.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2020 sta.blockhead
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion

#region Authors
/*
 * Authors:
 * - Gonzalo Paniagua Javier <gonzalo@novell.com>
 */
#endregion

using System;
using System.IO;
using System.Text;

namespace HttpSiraStatus.WebSockets.Net
{
    internal class ResponseStream : Stream
    {
        #region Private Fields

        private MemoryStream _bodyBuffer;
        private static readonly byte[] _crlf;
        private bool _disposed;
        private Stream _innerStream;
        private static readonly byte[] _lastChunk;
        private static readonly int _maxHeadersLength;
        private HttpListenerResponse _response;
        private bool _sendChunked;
        private readonly Action<byte[], int, int> _write;
        private Action<byte[], int, int> _writeBody;
        private readonly Action<byte[], int, int> _writeChunked;

        #endregion

        #region Static Constructor

        static ResponseStream()
        {
            _crlf = new byte[] { 13, 10 }; // "\r\n"
            _lastChunk = new byte[] { 48, 13, 10, 13, 10 }; // "0\r\n\r\n"
            _maxHeadersLength = 32768;
        }

        #endregion

        #region Internal Constructors

        internal ResponseStream(
          Stream innerStream,
          HttpListenerResponse response,
          bool ignoreWriteExceptions
        )
        {
            this._innerStream = innerStream;
            this._response = response;

            if (ignoreWriteExceptions) {
                this._write = this.writeWithoutThrowingException;
                this._writeChunked = this.writeChunkedWithoutThrowingException;
            }
            else {
                this._write = innerStream.Write;
                this._writeChunked = this.writeChunked;
            }

            this._bodyBuffer = new MemoryStream();
        }

        #endregion

        #region Public Properties

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => !this._disposed;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();

            set => throw new NotSupportedException();
        }

        #endregion

        #region Private Methods

        private bool flush(bool closing)
        {
            if (!this._response.HeadersSent) {
                if (!this.flushHeaders()) {
                    return false;
                }

                this._response.HeadersSent = true;

                this._sendChunked = this._response.SendChunked;
                this._writeBody = this._sendChunked ? this._writeChunked : this._write;
            }

            this.flushBody(closing);

            return true;
        }

        private void flushBody(bool closing)
        {
            using (this._bodyBuffer) {
                var len = this._bodyBuffer.Length;

                if (len > int.MaxValue) {
                    this._bodyBuffer.Position = 0;

                    var buffLen = 1024;
                    var buff = new byte[buffLen];
                    while (true) {
                        var nread = this._bodyBuffer.Read(buff, 0, buffLen);
                        if (nread <= 0) {
                            break;
                        }

                        this._writeBody(buff, 0, nread);
                    }
                }
                else if (len > 0) {
                    this._writeBody(this._bodyBuffer.GetBuffer(), 0, (int)len);
                }
            }

            if (!closing) {
                this._bodyBuffer = new MemoryStream();
                return;
            }

            if (this._sendChunked) {
                this._write(_lastChunk, 0, 5);
            }

            this._bodyBuffer = null;
        }

        private bool flushHeaders()
        {
            if (!this._response.SendChunked) {
                if (this._response.ContentLength64 != this._bodyBuffer.Length) {
                    return false;
                }
            }

            var statusLine = this._response.StatusLine;
            var headers = this._response.FullHeaders;

            var buff = new MemoryStream();
            var enc = Encoding.UTF8;

            using (var writer = new StreamWriter(buff, enc, 256)) {
                writer.Write(statusLine);
                writer.Write(headers.ToStringMultiValue(true));
                writer.Flush();

                var start = enc.GetPreamble().Length;
                var len = buff.Length - start;

                if (len > _maxHeadersLength) {
                    return false;
                }

                this._write(buff.GetBuffer(), start, (int)len);
            }

            this._response.CloseConnection = headers["Connection"] == "close";

            return true;
        }

        private static byte[] getChunkSizeBytes(int size)
        {
            var chunkSize = string.Format("{0:x}\r\n", size);

            return Encoding.ASCII.GetBytes(chunkSize);
        }

        private void writeChunked(byte[] buffer, int offset, int count)
        {
            var size = getChunkSizeBytes(count);

            this._innerStream.Write(size, 0, size.Length);
            this._innerStream.Write(buffer, offset, count);
            this._innerStream.Write(_crlf, 0, 2);
        }

        private void writeChunkedWithoutThrowingException(
          byte[] buffer, int offset, int count
        )
        {
            try {
                this.writeChunked(buffer, offset, count);
            }
            catch {
            }
        }

        private void writeWithoutThrowingException(
          byte[] buffer, int offset, int count
        )
        {
            try {
                this._innerStream.Write(buffer, offset, count);
            }
            catch {
            }
        }

        #endregion

        #region Internal Methods

        internal void Close(bool force)
        {
            if (this._disposed) {
                return;
            }

            this._disposed = true;

            if (!force) {
                if (this.flush(true)) {
                    this._response.Close();

                    this._response = null;
                    this._innerStream = null;

                    return;
                }

                this._response.CloseConnection = true;
            }

            if (this._sendChunked) {
                this._write(_lastChunk, 0, 5);
            }

            this._bodyBuffer.Dispose();
            this._response.Abort();

            this._bodyBuffer = null;
            this._response = null;
            this._innerStream = null;
        }

        internal void InternalWrite(byte[] buffer, int offset, int count)
        {
            this._write(buffer, offset, count);
        }

        #endregion

        #region Public Methods

        public override IAsyncResult BeginRead(
          byte[] buffer,
          int offset,
          int count,
          AsyncCallback callback,
          object state
        )
        {
            throw new NotSupportedException();
        }

        public override IAsyncResult BeginWrite(
          byte[] buffer,
          int offset,
          int count,
          AsyncCallback callback,
          object state
        )
        {
            if (this._disposed) {
                var name = this.GetType().ToString();

                throw new ObjectDisposedException(name);
            }

            return this._bodyBuffer.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void Close()
        {
            this.Close(false);
        }

        protected override void Dispose(bool disposing)
        {
            this.Close(!disposing);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            if (this._disposed) {
                var name = this.GetType().ToString();

                throw new ObjectDisposedException(name);
            }

            this._bodyBuffer.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            if (this._disposed) {
                return;
            }

            var sendChunked = this._sendChunked || this._response.SendChunked;

            if (!sendChunked) {
                return;
            }

            _ = this.flush(false);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
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
            if (this._disposed) {
                var name = this.GetType().ToString();

                throw new ObjectDisposedException(name);
            }

            this._bodyBuffer.Write(buffer, offset, count);
        }

        #endregion
    }
}
