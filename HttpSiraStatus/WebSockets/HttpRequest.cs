#region License
/*
 * HttpRequest.cs
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

#region Contributors
/*
 * Contributors:
 * - David Burhans
 */
#endregion

using HttpSiraStatus.WebSockets.Net;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace HttpSiraStatus.WebSockets
{
    internal class HttpRequest : HttpBase
    {
        #region Private Fields

        private CookieCollection _cookies;

        #endregion

        #region Private Constructors

        private HttpRequest(
          string method,
          string target,
          Version version,
          NameValueCollection headers
        )
          : base(version, headers)
        {
            this.HttpMethod = method;
            this.RequestTarget = target;
        }

        #endregion

        #region Internal Constructors

        internal HttpRequest(string method, string target)
          : this(method, target, HttpVersion.Version11, new NameValueCollection())
        {
            this.Headers["User-Agent"] = "websocket-sharp/1.0";
        }

        #endregion

        #region Internal Properties

        internal string RequestLine => string.Format(
                         "{0} {1} HTTP/{2}{3}", this.HttpMethod, this.RequestTarget, this.ProtocolVersion, CrLf
                       );

        #endregion

        #region Public Properties

        public AuthenticationResponse AuthenticationResponse
        {
            get
            {
                var val = this.Headers["Authorization"];

                return val != null && val.Length > 0
                       ? AuthenticationResponse.Parse(val)
                       : null;
            }
        }

        public CookieCollection Cookies
        {
            get
            {
                this._cookies ??= this.Headers.GetCookies(false);

                return this._cookies;
            }
        }

        public string HttpMethod { get; }

        public bool IsWebSocketRequest => this.HttpMethod == "GET"
                       && this.ProtocolVersion > HttpVersion.Version10
                       && this.Headers.Upgrades("websocket");

        public override string MessageHeader => this.RequestLine + this.HeaderSection;

        public string RequestTarget { get; }

        #endregion

        #region Internal Methods

        internal static HttpRequest CreateConnectRequest(Uri targetUri)
        {
            var host = targetUri.DnsSafeHost;
            var port = targetUri.Port;
            var authority = string.Format("{0}:{1}", host, port);

            var ret = new HttpRequest("CONNECT", authority);

            ret.Headers["Host"] = port != 80 ? authority : host;

            return ret;
        }

        internal static HttpRequest CreateWebSocketHandshakeRequest(Uri targetUri)
        {
            var ret = new HttpRequest("GET", targetUri.PathAndQuery);

            var headers = ret.Headers;

            var port = targetUri.Port;
            var schm = targetUri.Scheme;
            var defaultPort = (port == 80 && schm == "ws")
                              || (port == 443 && schm == "wss");

            headers["Host"] = !defaultPort
                              ? targetUri.Authority
                              : targetUri.DnsSafeHost;

            headers["Upgrade"] = "websocket";
            headers["Connection"] = "Upgrade";

            return ret;
        }

        internal HttpResponse GetResponse(Stream stream, int millisecondsTimeout)
        {
            this.WriteTo(stream);

            return HttpResponse.ReadResponse(stream, millisecondsTimeout);
        }

        internal static HttpRequest Parse(string[] messageHeader)
        {
            var len = messageHeader.Length;

            if (len == 0) {
                var msg = "An empty request header.";

                throw new ArgumentException(msg);
            }

            var rlParts = messageHeader[0].Split(new[] { ' ' }, 3);

            if (rlParts.Length != 3) {
                var msg = "It includes an invalid request line.";

                throw new ArgumentException(msg);
            }

            var method = rlParts[0];
            var target = rlParts[1];
            var ver = rlParts[2].Substring(5).ToVersion();

            var headers = new WebHeaderCollection();

            for (var i = 1; i < len; i++) {
                headers.InternalSet(messageHeader[i], false);
            }

            return new HttpRequest(method, target, ver, headers);
        }

        internal static HttpRequest ReadRequest(
          Stream stream, int millisecondsTimeout
        )
        {
            return Read(stream, Parse, millisecondsTimeout);
        }

        #endregion

        #region Public Methods

        public void SetCookies(CookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0) {
                return;
            }

            var buff = new StringBuilder(64);

            foreach (var cookie in cookies.Sorted) {
                if (cookie.Expired) {
                    continue;
                }

                _ = buff.AppendFormat("{0}; ", cookie);
            }

            var len = buff.Length;

            if (len <= 2) {
                return;
            }

            buff.Length = len - 2;

            this.Headers["Cookie"] = buff.ToString();
        }

        #endregion
    }
}
