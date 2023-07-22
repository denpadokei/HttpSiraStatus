#region License
/*
 * PayloadData.cs
 *
 * The MIT License
 *
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

using System;
using System.Collections;
using System.Collections.Generic;

namespace HttpSiraStatus.WebSockets
{
    internal class PayloadData : IEnumerable<byte>
    {
        #region Private Fields

        private readonly byte[] _data;
        private readonly long _length;

        #endregion

        #region Public Fields

        /// <summary>
        /// Represents the empty payload data.
        /// </summary>
        public static readonly PayloadData Empty;

        /// <summary>
        /// Represents the allowable max length of payload data.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   A <see cref="WebSocketException"/> is thrown when the length of
        ///   incoming payload data is greater than the value of this field.
        ///   </para>
        ///   <para>
        ///   If you would like to change the value of this field, it must be
        ///   a number between <see cref="WebSocket.FragmentLength"/> and
        ///   <see cref="long.MaxValue"/> inclusive.
        ///   </para>
        /// </remarks>
        public static readonly ulong MaxLength;

        #endregion

        #region Static Constructor

        static PayloadData()
        {
            Empty = new PayloadData(WebSocket.EmptyBytes, 0);
            MaxLength = long.MaxValue;
        }

        #endregion

        #region Internal Constructors

        internal PayloadData(byte[] data)
          : this(data, data.LongLength)
        {
        }

        internal PayloadData(byte[] data, long length)
        {
            this._data = data;
            this._length = length;
        }

        internal PayloadData(ushort code, string reason)
        {
            this._data = code.Append(reason);
            this._length = this._data.LongLength;
        }

        #endregion

        #region Internal Properties

        internal ushort Code => this._length >= 2
                       ? this._data.SubArray(0, 2).ToUInt16(ByteOrder.Big)
                       : (ushort)1005;

        internal long ExtensionDataLength { get; set; }

        internal bool HasReservedCode => this._length >= 2 && this.Code.IsReservedStatusCode();

        internal string Reason
        {
            get
            {
                if (this._length <= 2) {
                    return string.Empty;
                }

                var bytes = this._data.SubArray(2, this._length - 2);

                return bytes.TryGetUTF8DecodedString(out var reason)
                       ? reason
                       : string.Empty;
            }
        }

        #endregion

        #region Public Properties

        public byte[] ApplicationData => this.ExtensionDataLength > 0
                       ? this._data.SubArray(this.ExtensionDataLength, this._length - this.ExtensionDataLength)
                       : this._data;

        public byte[] ExtensionData => this.ExtensionDataLength > 0
                       ? this._data.SubArray(0, this.ExtensionDataLength)
                       : WebSocket.EmptyBytes;

        public ulong Length => (ulong)this._length;

        #endregion

        #region Internal Methods

        internal void Mask(byte[] key)
        {
            for (long i = 0; i < this._length; i++) {
                this._data[i] = (byte)(this._data[i] ^ key[i % 4]);
            }
        }

        #endregion

        #region Public Methods

        public IEnumerator<byte> GetEnumerator()
        {
            foreach (var b in this._data) {
                yield return b;
            }
        }

        public byte[] ToArray()
        {
            return this._data;
        }

        public override string ToString()
        {
            return BitConverter.ToString(this._data);
        }

        #endregion

        #region Explicit Interface Implementations

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
    }
}
