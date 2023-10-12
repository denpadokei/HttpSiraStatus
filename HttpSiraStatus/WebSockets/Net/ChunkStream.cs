#region License
/*
 * ChunkStream.cs
 *
 * This code is derived from ChunkStream.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2003 Ximian, Inc (http://www.ximian.com)
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
 * - Gonzalo Paniagua Javier <gonzalo@ximian.com>
 */
#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace HttpSiraStatus.WebSockets.Net
{
    internal class ChunkStream
    {
        #region Private Fields

        private int _chunkRead;
        private int _chunkSize;
        private readonly List<Chunk> _chunks;
        private bool _gotIt;
        private readonly StringBuilder _saved;
        private bool _sawCr;
        private InputChunkState _state;
        private int _trailerState;

        #endregion

        #region Public Constructors

        public ChunkStream(WebHeaderCollection headers)
        {
            this.Headers = headers;

            this._chunkSize = -1;
            this._chunks = new List<Chunk>();
            this._saved = new StringBuilder();
        }

        #endregion

        #region Internal Properties

        internal int Count { get; private set; }

        internal byte[] EndBuffer { get; private set; }

        internal int Offset { get; private set; }

        #endregion

        #region Public Properties

        public WebHeaderCollection Headers { get; }

        public bool WantsMore => this._state < InputChunkState.End;

        #endregion

        #region Private Methods

        private int read(byte[] buffer, int offset, int count)
        {
            var nread = 0;
            var cnt = this._chunks.Count;

            for (var i = 0; i < cnt; i++) {
                var chunk = this._chunks[i];

                if (chunk == null) {
                    continue;
                }

                if (chunk.ReadLeft == 0) {
                    this._chunks[i] = null;

                    continue;
                }

                nread += chunk.Read(buffer, offset + nread, count - nread);

                if (nread == count) {
                    break;
                }
            }

            return nread;
        }

        private InputChunkState seekCrLf(byte[] buffer, ref int offset, int length)
        {
            if (!this._sawCr) {
                if (buffer[offset++] != 13) {
                    throwProtocolViolation("CR is expected.");
                }

                this._sawCr = true;

                if (offset == length) {
                    return InputChunkState.DataEnded;
                }
            }

            if (buffer[offset++] != 10) {
                throwProtocolViolation("LF is expected.");
            }

            return InputChunkState.None;
        }

        private InputChunkState setChunkSize(
          byte[] buffer, ref int offset, int length
        )
        {
            byte b = 0;

            while (offset < length) {
                b = buffer[offset++];

                if (this._sawCr) {
                    if (b != 10) {
                        throwProtocolViolation("LF is expected.");
                    }

                    break;
                }

                if (b == 13) {
                    this._sawCr = true;

                    continue;
                }

                if (b == 10) {
                    throwProtocolViolation("LF is unexpected.");
                }

                if (this._gotIt) {
                    continue;
                }

                if (b == 32 || b == 59) { // SP or ';'
                    this._gotIt = true;

                    continue;
                }

                _ = this._saved.Append((char)b);
            }

            if (this._saved.Length > 20) {
                throwProtocolViolation("The chunk size is too big.");
            }

            if (b != 10) {
                return InputChunkState.None;
            }

            var s = this._saved.ToString();

            try {
                this._chunkSize = int.Parse(s, NumberStyles.HexNumber);
            }
            catch {
                throwProtocolViolation("The chunk size cannot be parsed.");
            }

            this._chunkRead = 0;

            if (this._chunkSize == 0) {
                this._trailerState = 2;

                return InputChunkState.Trailer;
            }

            return InputChunkState.Data;
        }

        private InputChunkState setTrailer(
          byte[] buffer, ref int offset, int length
        )
        {
            while (offset < length) {
                if (this._trailerState == 4) // CR LF CR LF
{
                    break;
                }

                var b = buffer[offset++];

                _ = this._saved.Append((char)b);

                if (this._trailerState == 1 || this._trailerState == 3) { // CR or CR LF CR
                    if (b != 10) {
                        throwProtocolViolation("LF is expected.");
                    }

                    this._trailerState++;

                    continue;
                }

                if (b == 13) {
                    this._trailerState++;

                    continue;
                }

                if (b == 10) {
                    throwProtocolViolation("LF is unexpected.");
                }

                this._trailerState = 0;
            }

            var len = this._saved.Length;

            if (len > 4196) {
                throwProtocolViolation("The trailer is too long.");
            }

            if (this._trailerState < 4) {
                return InputChunkState.Trailer;
            }

            if (len == 2) {
                return InputChunkState.End;
            }

            this._saved.Length = len - 2;

            var val = this._saved.ToString();
            var reader = new StringReader(val);

            while (true) {
                var line = reader.ReadLine();

                if (line == null || line.Length == 0) {
                    break;
                }

                this.Headers.Add(line);
            }

            return InputChunkState.End;
        }

        private static void throwProtocolViolation(string message)
        {
            throw new WebException(
                    message, null, WebExceptionStatus.ServerProtocolViolation, null
                  );
        }

        private void write(byte[] buffer, int offset, int length)
        {
            if (this._state == InputChunkState.End) {
                throwProtocolViolation("The chunks were ended.");
            }

            if (this._state == InputChunkState.None) {
                this._state = this.setChunkSize(buffer, ref offset, length);

                if (this._state == InputChunkState.None) {
                    return;
                }

                this._saved.Length = 0;
                this._sawCr = false;
                this._gotIt = false;
            }

            if (this._state == InputChunkState.Data) {
                if (offset >= length) {
                    return;
                }

                this._state = this.writeData(buffer, ref offset, length);

                if (this._state == InputChunkState.Data) {
                    return;
                }
            }

            if (this._state == InputChunkState.DataEnded) {
                if (offset >= length) {
                    return;
                }

                this._state = this.seekCrLf(buffer, ref offset, length);

                if (this._state == InputChunkState.DataEnded) {
                    return;
                }

                this._sawCr = false;
            }

            if (this._state == InputChunkState.Trailer) {
                if (offset >= length) {
                    return;
                }

                this._state = this.setTrailer(buffer, ref offset, length);

                if (this._state == InputChunkState.Trailer) {
                    return;
                }

                this._saved.Length = 0;
            }

            if (this._state == InputChunkState.End) {
                this.EndBuffer = buffer;
                this.Offset = offset;
                this.Count = length - offset;

                return;
            }

            if (offset >= length) {
                return;
            }

            this.write(buffer, offset, length);
        }

        private InputChunkState writeData(
          byte[] buffer, ref int offset, int length
        )
        {
            var cnt = length - offset;
            var left = this._chunkSize - this._chunkRead;

            if (cnt > left) {
                cnt = left;
            }

            var data = new byte[cnt];
            Buffer.BlockCopy(buffer, offset, data, 0, cnt);

            var chunk = new Chunk(data);
            this._chunks.Add(chunk);

            offset += cnt;
            this._chunkRead += cnt;

            return this._chunkRead == this._chunkSize
                   ? InputChunkState.DataEnded
                   : InputChunkState.Data;
        }

        #endregion

        #region Internal Methods

        internal void ResetChunkStore()
        {
            this._chunkRead = 0;
            this._chunkSize = -1;

            this._chunks.Clear();
        }

        #endregion

        #region Public Methods

        public int Read(byte[] buffer, int offset, int count)
        {
            return count <= 0 ? 0 : this.read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (count <= 0) {
                return;
            }

            this.write(buffer, offset, offset + count);
        }

        #endregion
    }
}
