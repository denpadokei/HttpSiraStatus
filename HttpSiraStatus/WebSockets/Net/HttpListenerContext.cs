#region License
/*
 * HttpListenerContext.cs
 *
 * This code is derived from HttpListenerContext.cs (System.Net) of Mono
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

using HttpSiraStatus.WebSockets.Net.WebSockets;
using System;
using System.Security.Principal;
using System.Text;

namespace HttpSiraStatus.WebSockets.Net
{
    /// <summary>
    /// Provides the access to the HTTP request and response objects used by
    /// the <see cref="HttpListener"/> class.
    /// </summary>
    /// <remarks>
    /// This class cannot be inherited.
    /// </remarks>
    public sealed class HttpListenerContext
    {
        #region Private Fields

        private HttpListenerWebSocketContext _websocketContext;

        #endregion

        #region Internal Constructors

        internal HttpListenerContext(HttpConnection connection)
        {
            this.Connection = connection;

            this.ErrorStatusCode = 400;
            this.Request = new HttpListenerRequest(this);
            this.Response = new HttpListenerResponse(this);
        }

        #endregion

        #region Internal Properties

        internal HttpConnection Connection { get; }

        internal string ErrorMessage { get; set; }

        internal int ErrorStatusCode { get; set; }

        internal bool HasErrorMessage => this.ErrorMessage != null;

        internal HttpListener Listener { get; set; }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the HTTP request object that represents a client request.
        /// </summary>
        /// <value>
        /// A <see cref="HttpListenerRequest"/> that represents the client request.
        /// </value>
        public HttpListenerRequest Request { get; }

        /// <summary>
        /// Gets the HTTP response object used to send a response to the client.
        /// </summary>
        /// <value>
        /// A <see cref="HttpListenerResponse"/> that represents a response to
        /// the client request.
        /// </value>
        public HttpListenerResponse Response { get; }

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
        public IPrincipal User { get; private set; }

        #endregion

        #region Private Methods

        private static string createErrorContent(
          int statusCode, string statusDescription, string message
        )
        {
            return message != null && message.Length > 0
                   ? string.Format(
                       "<html><body><h1>{0} {1} ({2})</h1></body></html>",
                       statusCode,
                       statusDescription,
                       message
                     )
                   : string.Format(
                       "<html><body><h1>{0} {1}</h1></body></html>",
                       statusCode,
                       statusDescription
                     );
        }

        #endregion

        #region Internal Methods

        internal HttpListenerWebSocketContext GetWebSocketContext(string protocol)
        {
            this._websocketContext = new HttpListenerWebSocketContext(this, protocol);

            return this._websocketContext;
        }

        internal void SendAuthenticationChallenge(
          AuthenticationSchemes scheme, string realm
        )
        {
            this.Response.StatusCode = 401;

            var chal = new AuthenticationChallenge(scheme, realm).ToString();
            this.Response.Headers.InternalSet("WWW-Authenticate", chal, true);

            this.Response.Close();
        }

        internal void SendError()
        {
            try {
                this.Response.StatusCode = this.ErrorStatusCode;
                this.Response.ContentType = "text/html";

                var content = createErrorContent(
                                this.ErrorStatusCode,
                                this.Response.StatusDescription,
                                this.ErrorMessage
                              );

                var enc = Encoding.UTF8;
                var entity = enc.GetBytes(content);

                this.Response.ContentEncoding = enc;
                this.Response.ContentLength64 = entity.LongLength;

                this.Response.Close(entity, true);
            }
            catch {
                this.Connection.Close(true);
            }
        }

        internal void SendError(int statusCode)
        {
            this.ErrorStatusCode = statusCode;

            this.SendError();
        }

        internal void SendError(int statusCode, string message)
        {
            this.ErrorStatusCode = statusCode;
            this.ErrorMessage = message;

            this.SendError();
        }

        internal bool SetUser(
          AuthenticationSchemes scheme,
          string realm,
          Func<IIdentity, NetworkCredential> credentialsFinder
        )
        {
            var user = HttpUtility.CreateUser(
                         this.Request.Headers["Authorization"],
                         scheme,
                         realm,
                         this.Request.HttpMethod,
                         credentialsFinder
                       );

            if (user == null) {
                return false;
            }

            if (!user.Identity.IsAuthenticated) {
                return false;
            }

            this.User = user;

            return true;
        }

        internal void Unregister()
        {
            if (this.Listener == null) {
                return;
            }

            this.Listener.UnregisterContext(this);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Accepts a WebSocket connection.
        /// </summary>
        /// <returns>
        /// A <see cref="HttpListenerWebSocketContext"/> that represents
        /// the WebSocket handshake request.
        /// </returns>
        /// <param name="protocol">
        ///   <para>
        ///   A <see cref="string"/> that specifies the name of the subprotocol
        ///   supported on the WebSocket connection.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   This method has already been done.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The client request is not a WebSocket handshake request.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="protocol"/> is empty.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="protocol"/> contains an invalid character.
        ///   </para>
        /// </exception>
        public HttpListenerWebSocketContext AcceptWebSocket(string protocol)
        {
            return this.AcceptWebSocket(protocol, null);
        }

        /// <summary>
        /// Accepts a WebSocket connection with initializing the WebSocket
        /// interface.
        /// </summary>
        /// <returns>
        /// A <see cref="HttpListenerWebSocketContext"/> that represents
        /// the WebSocket handshake request.
        /// </returns>
        /// <param name="protocol">
        ///   <para>
        ///   A <see cref="string"/> that specifies the name of the subprotocol
        ///   supported on the WebSocket connection.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        /// </param>
        /// <param name="initializer">
        ///   <para>
        ///   An <see cref="T:System.Action{WebSocket}"/> delegate.
        ///   </para>
        ///   <para>
        ///   It specifies the delegate that invokes the method called when
        ///   initializing a new WebSocket instance.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   This method has already been done.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The client request is not a WebSocket handshake request.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="protocol"/> is empty.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="protocol"/> contains an invalid character.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="initializer"/> caused an exception.
        ///   </para>
        /// </exception>
        public HttpListenerWebSocketContext AcceptWebSocket(
          string protocol, Action<WebSocket> initializer
        )
        {
            if (this._websocketContext != null) {
                var msg = "The method has already been done.";

                throw new InvalidOperationException(msg);
            }

            if (!this.Request.IsWebSocketRequest) {
                var msg = "The request is not a WebSocket handshake request.";

                throw new InvalidOperationException(msg);
            }

            if (protocol != null) {
                if (protocol.Length == 0) {
                    var msg = "An empty string.";

                    throw new ArgumentException(msg, "protocol");
                }

                if (!protocol.IsToken()) {
                    var msg = "It contains an invalid character.";

                    throw new ArgumentException(msg, "protocol");
                }
            }

            var ret = this.GetWebSocketContext(protocol);

            var ws = ret.WebSocket;

            if (initializer != null) {
                try {
                    initializer(ws);
                }
                catch (Exception ex) {
                    if (ws.ReadyState == WebSocketState.New) {
                        this._websocketContext = null;
                    }

                    var msg = "It caused an exception.";

                    throw new ArgumentException(msg, "initializer", ex);
                }
            }

            ws.Accept();

            return ret;
        }

        #endregion
    }
}
