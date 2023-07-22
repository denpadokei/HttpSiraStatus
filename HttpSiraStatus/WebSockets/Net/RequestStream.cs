#region License
/*
 * RequestStream.cs
 *
 * This code is derived from RequestStream.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2022 sta.blockhead
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

namespace HttpSiraStatus.WebSockets.Net
{
    internal class RequestStream : Stream
    {
        #region Private Fields

        private long _bodyLeft;
        private bool _disposed;
        private readonly Stream _innerStream;

        #endregion

        #region Internal Constructors

        internal RequestStream(
          Stream innerStream,
          byte[] initialBuffer,
          int offset,
          int count,
          long contentLength
        )
        {
            this._innerStream = innerStream;
            this.InitialBuffer = initialBuffer;
            this.Offset = offset;
            this.Count = count;
            this._bodyLeft = contentLength;
        }

        #endregion

        #region Internal Properties

        internal int Count { get; private set; }

        internal byte[] InitialBuffer { get; }

        internal int Offset { get; private set; }

        #endregion

        #region Public Properties

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();

            set => throw new NotSupportedException();
        }

        #endregion

        #region Private Methods

        private int fillFromInitialBuffer(byte[] buffer, int offset, int count)
        {
            // This method returns a int:
            // - > 0 The number of bytes read from the initial buffer
            // - 0   No more bytes read from the initial buffer
            // - -1  No more content data

            if (this._bodyLeft == 0) {
                return -1;
            }

            if (this.Count == 0) {
                return 0;
            }

            if (count > this.Count) {
                count = this.Count;
            }

            if (this._bodyLeft > 0 && this._bodyLeft < count) {
                count = (int)this._bodyLeft;
            }

            Buffer.BlockCopy(this.InitialBuffer, this.Offset, buffer, offset, count);

            this.Offset += count;
            this.Count -= count;

            if (this._bodyLeft > 0) {
                this._bodyLeft -= count;
            }

            return count;
        }

        #endregion

        #region Public Methods

        public override IAsyncResult BeginRead(
          byte[] buffer, int offset, int count, AsyncCallback callback, object state
        )
        {
            if (this._disposed) {
                var name = this.GetType().ToString();

                throw new ObjectDisposedException(name);
            }

            if (buffer == null) {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0) {
                var msg = "A negative value.";

                throw new ArgumentOutOfRangeException("offset", msg);
            }

            if (count < 0) {
                var msg = "A negative value.";

                throw new ArgumentOutOfRangeException("count", msg);
            }

            var len = buffer.Length;

            if (offset + count > len) {
                var msg = "The sum of 'offset' and 'count' is greater than the length of 'buffer'.";

                throw new ArgumentException(msg);
            }

            if (count == 0) {
                return this._innerStream.BeginRead(buffer, offset, 0, callback, state);
            }

            var nread = this.fillFromInitialBuffer(buffer, offset, count);

            if (nread != 0) {
                var ares = new HttpStreamAsyncResult(callback, state)
                {
                    Buffer = buffer,
                    Offset = offset,
                    Count = count,
                    SyncRead = nread > 0 ? nread : 0
                };

                ares.Complete();

                return ares;
            }

            if (this._bodyLeft > 0 && this._bodyLeft < count) {
                count = (int)this._bodyLeft;
            }

            return this._innerStream.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(
          byte[] buffer, int offset, int count, AsyncCallback callback, object state
        )
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            this._disposed = true;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (this._disposed) {
                var name = this.GetType().ToString();

                throw new ObjectDisposedException(name);
            }

            if (asyncResult == null) {
                throw new ArgumentNullException("asyncResult");
            }

            if (asyncResult is HttpStreamAsyncResult ares) {
                if (!ares.IsCompleted) {
                    _ = ares.AsyncWaitHandle.WaitOne();
                }

                return ares.SyncRead;
            }

            var nread = this._innerStream.EndRead(asyncResult);

            if (nread > 0 && this._bodyLeft > 0) {
                this._bodyLeft -= nread;
            }

            return nread;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this._disposed) {
                var name = this.GetType().ToString();

                throw new ObjectDisposedException(name);
            }

            if (buffer == null) {
                throw new ArgumentNullException("buffer");
            }

            if (offset < 0) {
                var msg = "A negative value.";

                throw new ArgumentOutOfRangeException("offset", msg);
            }

            if (count < 0) {
                var msg = "A negative value.";

                throw new ArgumentOutOfRangeException("count", msg);
            }

            var len = buffer.Length;

            if (offset + count > len) {
                var msg = "The sum of 'offset' and 'count' is greater than the length of 'buffer'.";

                throw new ArgumentException(msg);
            }

            if (count == 0) {
                return 0;
            }

            var nread = this.fillFromInitialBuffer(buffer, offset, count);

            if (nread == -1) {
                return 0;
            }

            if (nread > 0) {
                return nread;
            }

            if (this._bodyLeft > 0 && this._bodyLeft < count) {
                count = (int)this._bodyLeft;
            }

            nread = this._innerStream.Read(buffer, offset, count);

            if (nread > 0 && this._bodyLeft > 0) {
                this._bodyLeft -= nread;
            }

            return nread;
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

        #endregion
    }
}
