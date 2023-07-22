#region License
/*
 * TcpListenerWebSocketContext.cs
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
 * - Liryna <liryna.stark@gmail.com>
 */
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;

namespace HttpSiraStatus.WebSockets.Net.WebSockets
{
    /// <summary>
    /// Provides the access to the information in a WebSocket handshake request
    /// to a <see cref="TcpListener"/> instance.
    /// </summary>
    internal class TcpListenerWebSocketContext : WebSocketContext
    {
        #region Private Fields

        private NameValueCollection _queryString;
        private HttpRequest _request;
        private Uri _requestUri;
        private readonly bool _secure;
        private readonly System.Net.EndPoint _serverEndPoint;
        private readonly TcpClient _tcpClient;
        private IPrincipal _user;
        private readonly System.Net.EndPoint _userEndPoint;
        private readonly WebSocket _websocket;

        #endregion

        #region Internal Constructors

        internal TcpListenerWebSocketContext(
          TcpClient tcpClient,
          string protocol,
          bool secure,
          ServerSslConfiguration sslConfig,
          Logger log
        )
        {
            this._tcpClient = tcpClient;
            this._secure = secure;
            this.Log = log;

            var netStream = tcpClient.GetStream();

            if (secure) {
                var sslStream = new SslStream(
                                  netStream,
                                  false,
                                  sslConfig.ClientCertificateValidationCallback
                                );

                sslStream.AuthenticateAsServer(
                  sslConfig.ServerCertificate,
                  sslConfig.ClientCertificateRequired,
                  sslConfig.EnabledSslProtocols,
                  sslConfig.CheckCertificateRevocation
                );

                this.Stream = sslStream;
            }
            else {
                this.Stream = netStream;
            }

            var sock = tcpClient.Client;
            this._serverEndPoint = sock.LocalEndPoint;
            this._userEndPoint = sock.RemoteEndPoint;

            this._request = HttpRequest.ReadRequest(this.Stream, 90000);
            this._websocket = new WebSocket(this, protocol);
        }

        #endregion

        #region Internal Properties

        internal Logger Log { get; }

        internal Stream Stream { get; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the HTTP cookies included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Net.CookieCollection"/> that contains
        ///   the cookies.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        public override CookieCollection CookieCollection => this._request.Cookies;

        /// <summary>
        /// Gets the HTTP headers included in the handshake request.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers.
        /// </value>
        public override NameValueCollection Headers => this._request.Headers;

        /// <summary>
        /// Gets the value of the Host header included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the server host name requested
        ///   by the client.
        ///   </para>
        ///   <para>
        ///   It includes the port number if provided.
        ///   </para>
        /// </value>
        public override string Host => this._request.Headers["Host"];

        /// <summary>
        /// Gets a value indicating whether the client is authenticated.
        /// </summary>
        /// <value>
        /// <c>true</c> if the client is authenticated; otherwise, <c>false</c>.
        /// </value>
        public override bool IsAuthenticated => this._user != null;

        /// <summary>
        /// Gets a value indicating whether the handshake request is sent from
        /// the local computer.
        /// </summary>
        /// <value>
        /// <c>true</c> if the handshake request is sent from the same computer
        /// as the server; otherwise, <c>false</c>.
        /// </value>
        public override bool IsLocal => this.UserEndPoint.Address.IsLocal();

        /// <summary>
        /// Gets a value indicating whether a secure connection is used to send
        /// the handshake request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the connection is secure; otherwise, <c>false</c>.
        /// </value>
        public override bool IsSecureConnection => this._secure;

        /// <summary>
        /// Gets a value indicating whether the request is a WebSocket handshake
        /// request.
        /// </summary>
        /// <value>
        /// <c>true</c> if the request is a WebSocket handshake request; otherwise,
        /// <c>false</c>.
        /// </value>
        public override bool IsWebSocketRequest => this._request.IsWebSocketRequest;

        /// <summary>
        /// Gets the value of the Origin header included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of the Origin header.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not included.
        ///   </para>
        /// </value>
        public override string Origin => this._request.Headers["Origin"];

        /// <summary>
        /// Gets the query string included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="NameValueCollection"/> that contains the query
        ///   parameters.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        public override NameValueCollection QueryString
        {
            get
            {
                if (this._queryString == null) {
                    var uri = this.RequestUri;
                    var query = uri?.Query;

                    this._queryString = QueryStringCollection.Parse(query, Encoding.UTF8);
                }

                return this._queryString;
            }
        }

        /// <summary>
        /// Gets the URI requested by the client.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Uri"/> that represents the URI parsed from the request.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the URI cannot be parsed.
        ///   </para>
        /// </value>
        public override Uri RequestUri
        {
            get
            {
                if (this._requestUri == null) {
                    this._requestUri = HttpUtility.CreateRequestUrl(
                          this._request.RequestTarget,
                          this._request.Headers["Host"],
                          this._request.IsWebSocketRequest,
                          this._secure
                        );
                }

                return this._requestUri;
            }
        }

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Key header included in
        /// the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the value of
        ///   the Sec-WebSocket-Key header.
        ///   </para>
        ///   <para>
        ///   The value is used to prove that the server received
        ///   a valid WebSocket handshake request.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not included.
        ///   </para>
        /// </value>
        public override string SecWebSocketKey => this._request.Headers["Sec-WebSocket-Key"];

        /// <summary>
        /// Gets the names of the subprotocols from the Sec-WebSocket-Protocol
        /// header included in the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <see cref="T:System.Collections.Generic.IEnumerable{string}"/>
        ///   instance.
        ///   </para>
        ///   <para>
        ///   It provides an enumerator which supports the iteration over
        ///   the collection of the names of the subprotocols.
        ///   </para>
        /// </value>
        public override IEnumerable<string> SecWebSocketProtocols
        {
            get
            {
                var val = this._request.Headers["Sec-WebSocket-Protocol"];

                if (val == null || val.Length == 0) {
                    yield break;
                }

                foreach (var elm in val.Split(',')) {
                    var protocol = elm.Trim();

                    if (protocol.Length == 0) {
                        continue;
                    }

                    yield return protocol;
                }
            }
        }

        /// <summary>
        /// Gets the value of the Sec-WebSocket-Version header included in
        /// the handshake request.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the WebSocket protocol
        ///   version specified by the client.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not included.
        ///   </para>
        /// </value>
        public override string SecWebSocketVersion => this._request.Headers["Sec-WebSocket-Version"];

        /// <summary>
        /// Gets the endpoint to which the handshake request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the server
        /// IP address and port number.
        /// </value>
        public override System.Net.IPEndPoint ServerEndPoint => (System.Net.IPEndPoint)this._serverEndPoint;

        /// <summary>
        /// Gets the client information.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="IPrincipal"/> instance that represents identity,
        ///   authentication, and security roles for the client.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the client is not authenticated.
        ///   </para>
        /// </value>
        public override IPrincipal User => this._user;

        /// <summary>
        /// Gets the endpoint from which the handshake request is sent.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client
        /// IP address and port number.
        /// </value>
        public override System.Net.IPEndPoint UserEndPoint => (System.Net.IPEndPoint)this._userEndPoint;

        /// <summary>
        /// Gets the WebSocket interface used for two-way communication between
        /// the client and server.
        /// </summary>
        /// <value>
        /// A <see cref="HttpSiraStatus.WebSockets.WebSocket"/> that represents the interface.
        /// </value>
        public override WebSocket WebSocket => this._websocket;

        #endregion

        #region Internal Methods

        internal void Close()
        {
            this.Stream.Close();
            this._tcpClient.Close();
        }

        internal void Close(HttpStatusCode code)
        {
            HttpResponse.CreateCloseResponse(code).WriteTo(this.Stream);

            this.Stream.Close();
            this._tcpClient.Close();
        }

        internal void SendAuthenticationChallenge(string challenge)
        {
            HttpResponse.CreateUnauthorizedResponse(challenge).WriteTo(this.Stream);

            this._request = HttpRequest.ReadRequest(this.Stream, 15000);
        }

        internal bool SetUser(
          AuthenticationSchemes scheme,
          string realm,
          Func<IIdentity, NetworkCredential> credentialsFinder
        )
        {
            var user = HttpUtility.CreateUser(
                         this._request.Headers["Authorization"],
                         scheme,
                         realm,
                         this._request.HttpMethod,
                         credentialsFinder
                       );

            if (user == null) {
                return false;
            }

            if (!user.Identity.IsAuthenticated) {
                return false;
            }

            this._user = user;

            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that contains the request line and headers
        /// included in the handshake request.
        /// </returns>
        public override string ToString()
        {
            return this._request.ToString();
        }

        #endregion
    }
}
