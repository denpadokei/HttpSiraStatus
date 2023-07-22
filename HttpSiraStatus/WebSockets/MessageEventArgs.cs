#region License
/*
 * MessageEventArgs.cs
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

namespace HttpSiraStatus.WebSockets
{
    /// <summary>
    /// Represents the event data for the <see cref="WebSocket.OnMessage"/> event.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   The message event occurs when the <see cref="WebSocket"/> interface
    ///   receives a message or a ping if the <see cref="WebSocket.EmitOnPing"/>
    ///   property is set to <c>true</c>.
    ///   </para>
    ///   <para>
    ///   If you would like to get the message data, you should access
    ///   the <see cref="Data"/> or <see cref="RawData"/> property.
    ///   </para>
    /// </remarks>
    public class MessageEventArgs : EventArgs
    {
        #region Private Fields

        private string _data;
        private bool _dataSet;
        private readonly byte[] _rawData;

        #endregion

        #region Internal Constructors

        internal MessageEventArgs(WebSocketFrame frame)
        {
            this.Opcode = frame.Opcode;
            this._rawData = frame.PayloadData.ApplicationData;
        }

        internal MessageEventArgs(Opcode opcode, byte[] rawData)
        {
            if ((ulong)rawData.LongLength > PayloadData.MaxLength) {
                throw new WebSocketException(CloseStatusCode.TooBig);
            }

            this.Opcode = opcode;
            this._rawData = rawData;
        }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the opcode for the message.
        /// </summary>
        /// <value>
        /// <see cref="Opcode.Text"/>, <see cref="Opcode.Binary"/>,
        /// or <see cref="Opcode.Ping"/>.
        /// </value>
        internal Opcode Opcode { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the message data as a <see cref="string"/>.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the message data
        ///   if the message type is text or ping.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the message type is binary or
        ///   the message data could not be UTF-8-decoded.
        ///   </para>
        /// </value>
        public string Data
        {
            get
            {
                this.setData();

                return this._data;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the message type is binary.
        /// </summary>
        /// <value>
        /// <c>true</c> if the message type is binary; otherwise, <c>false</c>.
        /// </value>
        public bool IsBinary => this.Opcode == Opcode.Binary;

        /// <summary>
        /// Gets a value indicating whether the message type is ping.
        /// </summary>
        /// <value>
        /// <c>true</c> if the message type is ping; otherwise, <c>false</c>.
        /// </value>
        public bool IsPing => this.Opcode == Opcode.Ping;

        /// <summary>
        /// Gets a value indicating whether the message type is text.
        /// </summary>
        /// <value>
        /// <c>true</c> if the message type is text; otherwise, <c>false</c>.
        /// </value>
        public bool IsText => this.Opcode == Opcode.Text;

        /// <summary>
        /// Gets the message data as an array of <see cref="byte"/>.
        /// </summary>
        /// <value>
        /// An array of <see cref="byte"/> that represents the message data.
        /// </value>
        public byte[] RawData
        {
            get
            {
                this.setData();

                return this._rawData;
            }
        }

        #endregion

        #region Private Methods

        private void setData()
        {
            if (this._dataSet) {
                return;
            }

            if (this.Opcode == Opcode.Binary) {
                this._dataSet = true;

                return;
            }

            if (this._rawData.TryGetUTF8DecodedString(out var data)) {
                this._data = data;
            }

            this._dataSet = true;
        }

        #endregion
    }
}
