#region License
/*
 * WebSocketFrame.cs
 *
 * The MIT License
 *
 * Copyright (c) 2012-2023 sta.blockhead
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

#region Contributors
/*
 * Contributors:
 * - Chris Swiedler
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HttpSiraStatus.WebSockets
{
    internal class WebSocketFrame : IEnumerable<byte>
    {
        #region Private Fields

        private static readonly int _defaultHeaderLength;
        private static readonly int _defaultMaskingKeyLength;

        #endregion

        #region Static Constructor

        static WebSocketFrame()
        {
            _defaultHeaderLength = 2;
            _defaultMaskingKeyLength = 4;
        }

        #endregion

        #region Private Constructors

        private WebSocketFrame()
        {
        }

        #endregion

        #region Internal Constructors

        internal WebSocketFrame(
          Fin fin, Opcode opcode, byte[] data, bool compressed, bool mask
        )
          : this(fin, opcode, new PayloadData(data), compressed, mask)
        {
        }

        internal WebSocketFrame(
          Fin fin,
          Opcode opcode,
          PayloadData payloadData,
          bool compressed,
          bool mask
        )
        {
            this.Fin = fin;
            this.Opcode = opcode;

            this.Rsv1 = compressed ? Rsv.On : Rsv.Off;
            this.Rsv2 = Rsv.Off;
            this.Rsv3 = Rsv.Off;

            var len = payloadData.Length;

            if (len < 126) {
                this.PayloadLength = (byte)len;
                this.ExtendedPayloadLength = WebSocket.EmptyBytes;
            }
            else if (len < 0x010000) {
                this.PayloadLength = 126;
                this.ExtendedPayloadLength = ((ushort)len).ToByteArray(ByteOrder.Big);
            }
            else {
                this.PayloadLength = 127;
                this.ExtendedPayloadLength = len.ToByteArray(ByteOrder.Big);
            }

            if (mask) {
                this.Mask = Mask.On;
                this.MaskingKey = createMaskingKey();

                payloadData.Mask(this.MaskingKey);
            }
            else {
                this.Mask = Mask.Off;
                this.MaskingKey = WebSocket.EmptyBytes;
            }

            this.PayloadData = payloadData;
        }

        #endregion

        #region Internal Properties

        internal ulong ExactPayloadLength => this.PayloadLength < 126
                       ? this.PayloadLength
                       : this.PayloadLength == 126
                         ? this.ExtendedPayloadLength.ToUInt16(ByteOrder.Big)
                         : this.ExtendedPayloadLength.ToUInt64(ByteOrder.Big);

        internal int ExtendedPayloadLengthWidth => this.PayloadLength < 126
                       ? 0
                       : this.PayloadLength == 126
                         ? 2
                         : 8;

        #endregion

        #region Public Properties

        public byte[] ExtendedPayloadLength { get; private set; }

        public Fin Fin { get; private set; }

        public bool IsBinary => this.Opcode == Opcode.Binary;

        public bool IsClose => this.Opcode == Opcode.Close;

        public bool IsCompressed => this.Rsv1 == Rsv.On;

        public bool IsContinuation => this.Opcode == Opcode.Cont;

        public bool IsControl => this.Opcode >= Opcode.Close;

        public bool IsData => this.Opcode == Opcode.Text || this.Opcode == Opcode.Binary;

        public bool IsFinal => this.Fin == Fin.Final;

        public bool IsFragment => this.Fin == Fin.More || this.Opcode == Opcode.Cont;

        public bool IsMasked => this.Mask == Mask.On;

        public bool IsPing => this.Opcode == Opcode.Ping;

        public bool IsPong => this.Opcode == Opcode.Pong;

        public bool IsText => this.Opcode == Opcode.Text;

        public ulong Length => (ulong)(
                         _defaultHeaderLength
                         + this.ExtendedPayloadLength.Length
                         + this.MaskingKey.Length
                       )
                       + this.PayloadData.Length;

        public Mask Mask { get; private set; }

        public byte[] MaskingKey { get; private set; }

        public Opcode Opcode { get; private set; }

        public PayloadData PayloadData { get; private set; }

        public byte PayloadLength { get; private set; }

        public Rsv Rsv1 { get; private set; }

        public Rsv Rsv2 { get; private set; }

        public Rsv Rsv3 { get; private set; }

        #endregion

        #region Private Methods

        private static byte[] createMaskingKey()
        {
            var key = new byte[_defaultMaskingKeyLength];

            WebSocket.RandomNumber.GetBytes(key);

            return key;
        }

        private static WebSocketFrame processHeader(byte[] header)
        {
            if (header.Length != _defaultHeaderLength) {
                var msg = "The header part of a frame could not be read.";

                throw new WebSocketException(msg);
            }

            // FIN
            var fin = (header[0] & 0x80) == 0x80 ? Fin.Final : Fin.More;

            // RSV1
            var rsv1 = (header[0] & 0x40) == 0x40 ? Rsv.On : Rsv.Off;

            // RSV2
            var rsv2 = (header[0] & 0x20) == 0x20 ? Rsv.On : Rsv.Off;

            // RSV3
            var rsv3 = (header[0] & 0x10) == 0x10 ? Rsv.On : Rsv.Off;

            // Opcode
            var opcode = (byte)(header[0] & 0x0f);

            // MASK
            var mask = (header[1] & 0x80) == 0x80 ? Mask.On : Mask.Off;

            // Payload Length
            var payloadLen = (byte)(header[1] & 0x7f);

            if (!opcode.IsSupportedOpcode()) {
                var msg = "The opcode of a frame is not supported.";

                throw new WebSocketException(CloseStatusCode.UnsupportedData, msg);
            }

            var frame = new WebSocketFrame
            {
                Fin = fin,
                Rsv1 = rsv1,
                Rsv2 = rsv2,
                Rsv3 = rsv3,
                Opcode = (Opcode)opcode,
                Mask = mask,
                PayloadLength = payloadLen
            };

            return frame;
        }

        private static WebSocketFrame readExtendedPayloadLength(
          Stream stream, WebSocketFrame frame
        )
        {
            var len = frame.ExtendedPayloadLengthWidth;

            if (len == 0) {
                frame.ExtendedPayloadLength = WebSocket.EmptyBytes;

                return frame;
            }

            var bytes = stream.ReadBytes(len);

            if (bytes.Length != len) {
                var msg = "The extended payload length of a frame could not be read.";

                throw new WebSocketException(msg);
            }

            frame.ExtendedPayloadLength = bytes;

            return frame;
        }

        private static void readExtendedPayloadLengthAsync(
          Stream stream,
          WebSocketFrame frame,
          Action<WebSocketFrame> completed,
          Action<Exception> error
        )
        {
            var len = frame.ExtendedPayloadLengthWidth;

            if (len == 0) {
                frame.ExtendedPayloadLength = WebSocket.EmptyBytes;

                completed(frame);

                return;
            }

            stream.ReadBytesAsync(
              len,
                            bytes =>
              {
                  if (bytes.Length != len) {
                      var msg = "The extended payload length of a frame could not be read.";

                      throw new WebSocketException(msg);
                  }

                  frame.ExtendedPayloadLength = bytes;

                  completed(frame);
              },
              error
            );
        }

        private static WebSocketFrame readHeader(Stream stream)
        {
            var bytes = stream.ReadBytes(_defaultHeaderLength);

            return processHeader(bytes);
        }

        private static void readHeaderAsync(
          Stream stream, Action<WebSocketFrame> completed, Action<Exception> error
        )
        {
            stream.ReadBytesAsync(
              _defaultHeaderLength,
              bytes =>
              {
                  var frame = processHeader(bytes);

                  completed(frame);
              },
              error
            );
        }

        private static WebSocketFrame readMaskingKey(
          Stream stream, WebSocketFrame frame
        )
        {
            if (!frame.IsMasked) {
                frame.MaskingKey = WebSocket.EmptyBytes;

                return frame;
            }

            var bytes = stream.ReadBytes(_defaultMaskingKeyLength);

            if (bytes.Length != _defaultMaskingKeyLength) {
                var msg = "The masking key of a frame could not be read.";

                throw new WebSocketException(msg);
            }

            frame.MaskingKey = bytes;

            return frame;
        }

        private static void readMaskingKeyAsync(
          Stream stream,
          WebSocketFrame frame,
          Action<WebSocketFrame> completed,
          Action<Exception> error
        )
        {
            if (!frame.IsMasked) {
                frame.MaskingKey = WebSocket.EmptyBytes;

                completed(frame);

                return;
            }

            stream.ReadBytesAsync(
              _defaultMaskingKeyLength,
                            bytes =>
              {
                  if (bytes.Length != _defaultMaskingKeyLength) {
                      var msg = "The masking key of a frame could not be read.";

                      throw new WebSocketException(msg);
                  }

                  frame.MaskingKey = bytes;

                  completed(frame);
              },
              error
            );
        }

        private static WebSocketFrame readPayloadData(
          Stream stream, WebSocketFrame frame
        )
        {
            var exactPayloadLen = frame.ExactPayloadLength;

            if (exactPayloadLen > PayloadData.MaxLength) {
                var msg = "The payload data of a frame is too big.";

                throw new WebSocketException(CloseStatusCode.TooBig, msg);
            }

            if (exactPayloadLen == 0) {
                frame.PayloadData = PayloadData.Empty;

                return frame;
            }

            var len = (long)exactPayloadLen;
            var bytes = frame.PayloadLength > 126
                        ? stream.ReadBytes(len, 1024)
                        : stream.ReadBytes((int)len);

            if (bytes.LongLength != len) {
                var msg = "The payload data of a frame could not be read.";

                throw new WebSocketException(msg);
            }

            frame.PayloadData = new PayloadData(bytes, len);

            return frame;
        }

        private static void readPayloadDataAsync(
          Stream stream,
          WebSocketFrame frame,
          Action<WebSocketFrame> completed,
          Action<Exception> error
        )
        {
            var exactPayloadLen = frame.ExactPayloadLength;

            if (exactPayloadLen > PayloadData.MaxLength) {
                var msg = "The payload data of a frame is too big.";

                throw new WebSocketException(CloseStatusCode.TooBig, msg);
            }

            if (exactPayloadLen == 0) {
                frame.PayloadData = PayloadData.Empty;

                completed(frame);

                return;
            }

            var len = (long)exactPayloadLen;

            void comp(byte[] bytes)
            {
                if (bytes.LongLength != len) {
                    var msg = "The payload data of a frame could not be read.";

                    throw new WebSocketException(msg);
                }

                frame.PayloadData = new PayloadData(bytes, len);

                completed(frame);
            }

            if (frame.PayloadLength > 126) {
                stream.ReadBytesAsync(len, 1024, comp, error);

                return;
            }

            stream.ReadBytesAsync((int)len, comp, error);
        }

        private string toDumpString()
        {
            var len = this.Length;
            var cnt = (long)(len / 4);
            var rem = (int)(len % 4);

            string spFmt;
            string cntFmt;

            if (cnt < 10000) {
                spFmt = "{0,4}";
                cntFmt = "{0,4}";
            }
            else if (cnt < 0x010000) {
                spFmt = "{0,4}";
                cntFmt = "{0,4:X}";
            }
            else if (cnt < 0x0100000000) {
                spFmt = "{0,8}";
                cntFmt = "{0,8:X}";
            }
            else {
                spFmt = "{0,16}";
                cntFmt = "{0,16:X}";
            }

            var baseFmt = @"{0} 01234567 89ABCDEF 01234567 89ABCDEF
{0}+--------+--------+--------+--------+
";
            var headerFmt = string.Format(baseFmt, spFmt);

            baseFmt = "{0}|{{1,8}} {{2,8}} {{3,8}} {{4,8}}|\n";
            var lineFmt = string.Format(baseFmt, cntFmt);

            baseFmt = "{0}+--------+--------+--------+--------+";
            var footerFmt = string.Format(baseFmt, spFmt);

            var buff = new StringBuilder(64);

            Action<string, string, string, string> lineWriter()
            {
                long lineCnt = 0;

                return (arg1, arg2, arg3, arg4) =>
                {
                    _ = buff.AppendFormat(
                lineFmt, ++lineCnt, arg1, arg2, arg3, arg4
              );
                };
            }

            var writeLine = lineWriter();
            var bytes = this.ToArray();

            _ = buff.AppendFormat(headerFmt, string.Empty);

            for (long i = 0; i <= cnt; i++) {
                var j = i * 4;

                if (i < cnt) {
                    var arg1 = Convert.ToString(bytes[j], 2).PadLeft(8, '0');
                    var arg2 = Convert.ToString(bytes[j + 1], 2).PadLeft(8, '0');
                    var arg3 = Convert.ToString(bytes[j + 2], 2).PadLeft(8, '0');
                    var arg4 = Convert.ToString(bytes[j + 3], 2).PadLeft(8, '0');

                    writeLine(arg1, arg2, arg3, arg4);

                    continue;
                }

                if (rem > 0) {
                    var arg1 = Convert.ToString(bytes[j], 2).PadLeft(8, '0');
                    var arg2 = rem >= 2
                               ? Convert.ToString(bytes[j + 1], 2).PadLeft(8, '0')
                               : string.Empty;

                    var arg3 = rem == 3
                               ? Convert.ToString(bytes[j + 2], 2).PadLeft(8, '0')
                               : string.Empty;

                    writeLine(arg1, arg2, arg3, string.Empty);
                }
            }

            _ = buff.AppendFormat(footerFmt, string.Empty);

            return buff.ToString();
        }

        private string toString()
        {
            var extPayloadLen = this.PayloadLength >= 126
                                ? this.ExactPayloadLength.ToString()
                                : string.Empty;

            var maskingKey = this.Mask == Mask.On
                             ? BitConverter.ToString(this.MaskingKey)
                             : string.Empty;

            var payloadData = this.PayloadLength >= 126
                              ? "***"
                              : this.PayloadLength > 0
                                ? this.PayloadData.ToString()
                                : string.Empty;

            var fmt = @"                    FIN: {0}
                   RSV1: {1}
                   RSV2: {2}
                   RSV3: {3}
                 Opcode: {4}
                   MASK: {5}
         Payload Length: {6}
Extended Payload Length: {7}
            Masking Key: {8}
           Payload Data: {9}";

            return string.Format(
                     fmt,
                     this.Fin,
                     this.Rsv1,
                     this.Rsv2,
                     this.Rsv3,
                     this.Opcode,
                     this.Mask,
                     this.PayloadLength,
                     extPayloadLen,
                     maskingKey,
                     payloadData
                   );
        }

        #endregion

        #region Internal Methods

        internal static WebSocketFrame CreateCloseFrame(
          PayloadData payloadData, bool mask
        )
        {
            return new WebSocketFrame(
                     Fin.Final, Opcode.Close, payloadData, false, mask
                   );
        }

        internal static WebSocketFrame CreatePingFrame(bool mask)
        {
            return new WebSocketFrame(
                     Fin.Final, Opcode.Ping, PayloadData.Empty, false, mask
                   );
        }

        internal static WebSocketFrame CreatePingFrame(byte[] data, bool mask)
        {
            return new WebSocketFrame(
                     Fin.Final, Opcode.Ping, new PayloadData(data), false, mask
                   );
        }

        internal static WebSocketFrame CreatePongFrame(
          PayloadData payloadData, bool mask
        )
        {
            return new WebSocketFrame(
                     Fin.Final, Opcode.Pong, payloadData, false, mask
                   );
        }

        internal static WebSocketFrame ReadFrame(Stream stream, bool unmask)
        {
            var frame = readHeader(stream);

            _ = readExtendedPayloadLength(stream, frame);
            _ = readMaskingKey(stream, frame);
            _ = readPayloadData(stream, frame);

            if (unmask) {
                frame.Unmask();
            }

            return frame;
        }

        internal static void ReadFrameAsync(
          Stream stream,
          bool unmask,
          Action<WebSocketFrame> completed,
          Action<Exception> error
        )
        {
            readHeaderAsync(
              stream,
              frame =>
                readExtendedPayloadLengthAsync(
                  stream,
                  frame,
                  frame1 =>
                    readMaskingKeyAsync(
                      stream,
                      frame1,
                      frame2 =>
                        readPayloadDataAsync(
                          stream,
                          frame2,
                          frame3 =>
                          {
                              if (unmask) {
                                  frame3.Unmask();
                              }

                              completed(frame3);
                          },
                          error
                        ),
                      error
                    ),
                  error
                ),
              error
            );
        }

        internal string ToString(bool dump)
        {
            return dump ? this.toDumpString() : this.toString();
        }

        internal void Unmask()
        {
            if (this.Mask == Mask.Off) {
                return;
            }

            this.PayloadData.Mask(this.MaskingKey);

            this.MaskingKey = WebSocket.EmptyBytes;
            this.Mask = Mask.Off;
        }

        #endregion

        #region Public Methods

        public IEnumerator<byte> GetEnumerator()
        {
            foreach (var b in this.ToArray()) {
                yield return b;
            }
        }

        public byte[] ToArray()
        {
            using (var buff = new MemoryStream()) {
                var header = (int)this.Fin;
                header = (header << 1) + (int)this.Rsv1;
                header = (header << 1) + (int)this.Rsv2;
                header = (header << 1) + (int)this.Rsv3;
                header = (header << 4) + (int)this.Opcode;
                header = (header << 1) + (int)this.Mask;
                header = (header << 7) + this.PayloadLength;

                var uint16Header = (ushort)header;
                var rawHeader = uint16Header.ToByteArray(ByteOrder.Big);

                buff.Write(rawHeader, 0, _defaultHeaderLength);

                if (this.PayloadLength >= 126) {
                    buff.Write(this.ExtendedPayloadLength, 0, this.ExtendedPayloadLength.Length);
                }

                if (this.Mask == Mask.On) {
                    buff.Write(this.MaskingKey, 0, _defaultMaskingKeyLength);
                }

                if (this.PayloadLength > 0) {
                    var bytes = this.PayloadData.ToArray();

                    if (this.PayloadLength > 126) {
                        buff.WriteBytes(bytes, 1024);
                    }
                    else {
                        buff.Write(bytes, 0, bytes.Length);
                    }
                }

                buff.Close();

                return buff.ToArray();
            }
        }

        public override string ToString()
        {
            var val = this.ToArray();

            return BitConverter.ToString(val);
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
