#region License
/*
 * WebSocketBehavior.cs
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

using HttpSiraStatus.WebSockets.Net;
using HttpSiraStatus.WebSockets.Net.WebSockets;
using System;
using System.Collections.Specialized;
using System.IO;
using System.Security.Principal;

namespace HttpSiraStatus.WebSockets.Server
{
    /// <summary>
    /// Exposes a set of methods and properties used to define the behavior of
    /// a WebSocket service provided by the <see cref="WebSocketServer"/> or
    /// <see cref="HttpServer"/> class.
    /// </summary>
    /// <remarks>
    /// This class is an abstract class.
    /// </remarks>
    public abstract class WebSocketBehavior : IWebSocketSession
    {
        #region Private Fields

        private WebSocketContext _context;
        private bool _emitOnPing;
        private string _protocol;
        private WebSocketSessionManager _sessions;
        private WebSocket _websocket;

        #endregion

        #region Protected Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketBehavior"/> class.
        /// </summary>
        protected WebSocketBehavior()
        {
            this.StartTime = DateTime.MaxValue;
        }

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets the HTTP headers for a session.
        /// </summary>
        /// <value>
        /// A <see cref="NameValueCollection"/> that contains the headers
        /// included in the WebSocket handshake request.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected NameValueCollection Headers
        {
            get
            {
                if (this._context == null) {
                    var msg = "The session has not started yet.";

                    throw new InvalidOperationException(msg);
                }

                return this._context.Headers;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the communication is possible for
        /// a session.
        /// </summary>
        /// <value>
        /// <c>true</c> if the communication is possible; otherwise, <c>false</c>.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected bool IsAlive
        {
            get
            {
                if (this._websocket == null) {
                    var msg = "The session has not started yet.";

                    throw new InvalidOperationException(msg);
                }

                return this._websocket.IsAlive;
            }
        }

        /// <summary>
        /// Gets the query string for a session.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="NameValueCollection"/> that contains the query
        ///   parameters included in the WebSocket handshake request.
        ///   </para>
        ///   <para>
        ///   An empty collection if not included.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected NameValueCollection QueryString
        {
            get
            {
                if (this._context == null) {
                    var msg = "The session has not started yet.";

                    throw new InvalidOperationException(msg);
                }

                return this._context.QueryString;
            }
        }

        /// <summary>
        /// Gets the current state of the WebSocket interface for a session.
        /// </summary>
        /// <value>
        ///   <para>
        ///   One of the <see cref="WebSocketState"/> enum values.
        ///   </para>
        ///   <para>
        ///   It indicates the current state of the interface.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected WebSocketState ReadyState
        {
            get
            {
                if (this._websocket == null) {
                    var msg = "The session has not started yet.";

                    throw new InvalidOperationException(msg);
                }

                return this._websocket.ReadyState;
            }
        }

        /// <summary>
        /// Gets the management function for the sessions in the service.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketSessionManager"/> that manages the sessions in
        /// the service.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected WebSocketSessionManager Sessions
        {
            get
            {
                if (this._sessions == null) {
                    var msg = "The session has not started yet.";

                    throw new InvalidOperationException(msg);
                }

                return this._sessions;
            }
        }

        /// <summary>
        /// Gets the client information for a session.
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
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected IPrincipal User
        {
            get
            {
                if (this._context == null) {
                    var msg = "The session has not started yet.";

                    throw new InvalidOperationException(msg);
                }

                return this._context.User;
            }
        }

        /// <summary>
        /// Gets the client endpoint for a session.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPEndPoint"/> that represents the client
        /// IP address and port number.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected System.Net.IPEndPoint UserEndPoint
        {
            get
            {
                if (this._context == null) {
                    var msg = "The session has not started yet.";

                    throw new InvalidOperationException(msg);
                }

                return this._context.UserEndPoint;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the delegate used to validate the HTTP cookies.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="T:System.Func{CookieCollection, CookieCollection, bool}"/>
        ///   delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the WebSocket interface
        ///   for a session validates the handshake request.
        ///   </para>
        ///   <para>
        ///   1st <see cref="CookieCollection"/> parameter passed to the method
        ///   contains the cookies to validate.
        ///   </para>
        ///   <para>
        ///   2nd <see cref="CookieCollection"/> parameter passed to the method
        ///   receives the cookies to send to the client.
        ///   </para>
        ///   <para>
        ///   The method must return <c>true</c> if the cookies are valid.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        public Func<CookieCollection, CookieCollection, bool> CookiesValidator { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the message event is emitted
        /// when the WebSocket interface for a session receives a ping.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the interface emits the message event when receives
        ///   a ping; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool EmitOnPing
        {
            get => this._websocket != null ? this._websocket.EmitOnPing : this._emitOnPing;

            set
            {
                if (this._websocket != null) {
                    this._websocket.EmitOnPing = value;

                    return;
                }

                this._emitOnPing = value;
            }
        }

        /// <summary>
        /// Gets or sets the delegate used to validate the Host header.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="T:System.Func{string, bool}"/> delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the WebSocket interface
        ///   for a session validates the handshake request.
        ///   </para>
        ///   <para>
        ///   The <see cref="string"/> parameter passed to the method is the value
        ///   of the Host header.
        ///   </para>
        ///   <para>
        ///   The method must return <c>true</c> if the header value is valid.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        public Func<string, bool> HostValidator { get; set; }

        /// <summary>
        /// Gets the unique ID of a session.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the unique ID of the session.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the session has not started yet.
        ///   </para>
        /// </value>
        public string ID { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the WebSocket interface for
        /// a session ignores the Sec-WebSocket-Extensions header.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the interface ignores the extensions requested
        ///   from the client; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool IgnoreExtensions { get; set; }

        /// <summary>
        /// Gets or sets the delegate used to validate the Origin header.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="T:System.Func{string, bool}"/> delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the WebSocket interface
        ///   for a session validates the handshake request.
        ///   </para>
        ///   <para>
        ///   The <see cref="string"/> parameter passed to the method is the value
        ///   of the Origin header or <see langword="null"/> if the header is not
        ///   present.
        ///   </para>
        ///   <para>
        ///   The method must return <c>true</c> if the header value is valid.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        public Func<string, bool> OriginValidator { get; set; }

        /// <summary>
        /// Gets or sets the name of the WebSocket subprotocol for a session.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the name of the subprotocol.
        ///   </para>
        ///   <para>
        ///   The value specified for a set operation must be a token defined in
        ///   <see href="http://tools.ietf.org/html/rfc2616#section-2.2">
        ///   RFC 2616</see>.
        ///   </para>
        ///   <para>
        ///   The default value is an empty string.
        ///   </para>
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The set operation is not available if the session has already started.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The value specified for a set operation is not a token.
        /// </exception>
        public string Protocol
        {
            get => this._websocket != null
                       ? this._websocket.Protocol
                       : this._protocol ?? string.Empty;

            set
            {
                if (this._websocket != null) {
                    var msg = "The session has already started.";

                    throw new InvalidOperationException(msg);
                }

                if (value == null || value.Length == 0) {
                    this._protocol = null;

                    return;
                }

                if (!value.IsToken()) {
                    var msg = "Not a token.";

                    throw new ArgumentException(msg, "value");
                }

                this._protocol = value;
            }
        }

        /// <summary>
        /// Gets the time that a session has started.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="DateTime"/> that represents the time that the session
        ///   has started.
        ///   </para>
        ///   <para>
        ///   <see cref="DateTime.MaxValue"/> if the session has not started yet.
        ///   </para>
        /// </value>
        public DateTime StartTime { get; private set; }

        #endregion

        #region Private Methods

        private string checkHandshakeRequest(WebSocketContext context)
        {
            if (this.HostValidator != null) {
                if (!this.HostValidator(context.Host)) {
                    var msg = "The Host header is invalid.";

                    return msg;
                }
            }

            if (this.OriginValidator != null) {
                if (!this.OriginValidator(context.Origin)) {
                    var msg = "The Origin header is non-existent or invalid.";

                    return msg;
                }
            }

            if (this.CookiesValidator != null) {
                var req = context.CookieCollection;
                var res = context.WebSocket.CookieCollection;

                if (!this.CookiesValidator(req, res)) {
                    var msg = "The Cookie header is non-existent or invalid.";

                    return msg;
                }
            }

            return null;
        }

        private void onClose(object sender, CloseEventArgs e)
        {
            if (this.ID == null) {
                return;
            }

            _ = this._sessions.Remove(this.ID);

            this.OnClose(e);
        }

        private void onError(object sender, ErrorEventArgs e)
        {
            this.OnError(e);
        }

        private void onMessage(object sender, MessageEventArgs e)
        {
            this.OnMessage(e);
        }

        private void onOpen(object sender, EventArgs e)
        {
            this.ID = this._sessions.Add(this);

            if (this.ID == null) {
                this._websocket.Close(CloseStatusCode.Away);

                return;
            }

            this.StartTime = DateTime.Now;

            this.OnOpen();
        }

        #endregion

        #region Internal Methods

        internal void Start(
          WebSocketContext context, WebSocketSessionManager sessions
        )
        {
            this._context = context;
            this._sessions = sessions;

            this._websocket = context.WebSocket;
            this._websocket.CustomHandshakeRequestChecker = this.checkHandshakeRequest;
            this._websocket.EmitOnPing = this._emitOnPing;
            this._websocket.IgnoreExtensions = this.IgnoreExtensions;
            this._websocket.Protocol = this._protocol;

            var waitTime = sessions.WaitTime;

            if (waitTime != this._websocket.WaitTime) {
                this._websocket.WaitTime = waitTime;
            }

            this._websocket.OnOpen += this.onOpen;
            this._websocket.OnMessage += this.onMessage;
            this._websocket.OnError += this.onError;
            this._websocket.OnClose += this.onClose;

            this._websocket.Accept();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Closes the WebSocket connection for a session.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the current state of the WebSocket
        /// interface is Closing or Closed.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected void Close()
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.Close();
        }

        /// <summary>
        /// Closes the WebSocket connection for a session with the specified
        /// code and reason.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the current state of the WebSocket
        /// interface is Closing or Closed.
        /// </remarks>
        /// <param name="code">
        ///   <para>
        ///   A <see cref="ushort"/> that specifies the status code indicating
        ///   the reason for the close.
        ///   </para>
        ///   <para>
        ///   The status codes are defined in
        ///   <see href="http://tools.ietf.org/html/rfc6455#section-7.4">
        ///   Section 7.4</see> of RFC 6455.
        ///   </para>
        /// </param>
        /// <param name="reason">
        ///   <para>
        ///   A <see cref="string"/> that specifies the reason for the close.
        ///   </para>
        ///   <para>
        ///   Its size must be 123 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///   <paramref name="code"/> is less than 1000 or greater than 4999.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The size of <paramref name="reason"/> is greater than 123 bytes.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="code"/> is 1010 (mandatory extension).
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is 1005 (no status) and
        ///   <paramref name="reason"/> is specified.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        protected void Close(ushort code, string reason)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.Close(code, reason);
        }

        /// <summary>
        /// Closes the WebSocket connection for a session with the specified
        /// code and reason.
        /// </summary>
        /// <remarks>
        /// This method does nothing if the current state of the WebSocket
        /// interface is Closing or Closed.
        /// </remarks>
        /// <param name="code">
        ///   <para>
        ///   One of the <see cref="CloseStatusCode"/> enum values.
        ///   </para>
        ///   <para>
        ///   It specifies the status code indicating the reason for the close.
        ///   </para>
        /// </param>
        /// <param name="reason">
        ///   <para>
        ///   A <see cref="string"/> that specifies the reason for the close.
        ///   </para>
        ///   <para>
        ///   Its size must be 123 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="code"/> is <see cref="CloseStatusCode.MandatoryExtension"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is <see cref="CloseStatusCode.NoStatus"/> and
        ///   <paramref name="reason"/> is specified.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="reason"/> is greater than 123 bytes.
        /// </exception>
        protected void Close(CloseStatusCode code, string reason)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.Close(code, reason);
        }

        /// <summary>
        /// Closes the WebSocket connection for a session asynchronously.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method does not wait for the close to be complete.
        ///   </para>
        ///   <para>
        ///   This method does nothing if the current state of the WebSocket
        ///   interface is Closing or Closed.
        ///   </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected void CloseAsync()
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.CloseAsync();
        }

        /// <summary>
        /// Closes the WebSocket connection for a session asynchronously with
        /// the specified code and reason.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method does not wait for the close to be complete.
        ///   </para>
        ///   <para>
        ///   This method does nothing if the current state of the WebSocket
        ///   interface is Closing or Closed.
        ///   </para>
        /// </remarks>
        /// <param name="code">
        ///   <para>
        ///   A <see cref="ushort"/> that specifies the status code indicating
        ///   the reason for the close.
        ///   </para>
        ///   <para>
        ///   The status codes are defined in
        ///   <see href="http://tools.ietf.org/html/rfc6455#section-7.4">
        ///   Section 7.4</see> of RFC 6455.
        ///   </para>
        /// </param>
        /// <param name="reason">
        ///   <para>
        ///   A <see cref="string"/> that specifies the reason for the close.
        ///   </para>
        ///   <para>
        ///   Its size must be 123 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <para>
        ///   <paramref name="code"/> is less than 1000 or greater than 4999.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The size of <paramref name="reason"/> is greater than 123 bytes.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="code"/> is 1010 (mandatory extension).
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is 1005 (no status) and
        ///   <paramref name="reason"/> is specified.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        protected void CloseAsync(ushort code, string reason)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.CloseAsync(code, reason);
        }

        /// <summary>
        /// Closes the WebSocket connection for a session asynchronously with
        /// the specified code and reason.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method does not wait for the close to be complete.
        ///   </para>
        ///   <para>
        ///   This method does nothing if the current state of the WebSocket
        ///   interface is Closing or Closed.
        ///   </para>
        /// </remarks>
        /// <param name="code">
        ///   <para>
        ///   One of the <see cref="CloseStatusCode"/> enum values.
        ///   </para>
        ///   <para>
        ///   It specifies the status code indicating the reason for the close.
        ///   </para>
        /// </param>
        /// <param name="reason">
        ///   <para>
        ///   A <see cref="string"/> that specifies the reason for the close.
        ///   </para>
        ///   <para>
        ///   Its size must be 123 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="code"/> is <see cref="CloseStatusCode.MandatoryExtension"/>.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="code"/> is <see cref="CloseStatusCode.NoStatus"/> and
        ///   <paramref name="reason"/> is specified.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="reason"/> could not be UTF-8-encoded.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="reason"/> is greater than 123 bytes.
        /// </exception>
        protected void CloseAsync(CloseStatusCode code, string reason)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.CloseAsync(code, reason);
        }

        /// <summary>
        /// Called when the WebSocket connection for a session has been closed.
        /// </summary>
        /// <param name="e">
        /// A <see cref="CloseEventArgs"/> that represents the event data passed
        /// from a <see cref="WebSocket.OnClose"/> event.
        /// </param>
        protected virtual void OnClose(CloseEventArgs e)
        {
        }

        /// <summary>
        /// Called when the WebSocket interface for a session gets an error.
        /// </summary>
        /// <param name="e">
        /// A <see cref="ErrorEventArgs"/> that represents the event data passed
        /// from a <see cref="WebSocket.OnError"/> event.
        /// </param>
        protected virtual void OnError(ErrorEventArgs e)
        {
        }

        /// <summary>
        /// Called when the WebSocket interface for a session receives a message.
        /// </summary>
        /// <param name="e">
        /// A <see cref="MessageEventArgs"/> that represents the event data passed
        /// from a <see cref="WebSocket.OnMessage"/> event.
        /// </param>
        protected virtual void OnMessage(MessageEventArgs e)
        {
        }

        /// <summary>
        /// Called when the WebSocket connection for a session has been established.
        /// </summary>
        protected virtual void OnOpen()
        {
        }

        /// <summary>
        /// Sends a ping to the client for a session.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the send has successfully done and a pong has been
        /// received within a time; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        protected bool Ping()
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            return this._websocket.Ping();
        }

        /// <summary>
        /// Sends a ping with the specified message to the client for a session.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the send has successfully done and a pong has been
        /// received within a time; otherwise, <c>false</c>.
        /// </returns>
        /// <param name="message">
        ///   <para>
        ///   A <see cref="string"/> that specifies the message to send.
        ///   </para>
        ///   <para>
        ///   Its size must be 125 bytes or less in UTF-8.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The session has not started yet.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="message"/> could not be UTF-8-encoded.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The size of <paramref name="message"/> is greater than 125 bytes.
        /// </exception>
        protected bool Ping(string message)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            return this._websocket.Ping(message);
        }

        /// <summary>
        /// Sends the specified data to the client for a session.
        /// </summary>
        /// <param name="data">
        /// An array of <see cref="byte"/> that specifies the binary data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session has not started yet.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket interface is not Open.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        protected void Send(byte[] data)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.Send(data);
        }

        /// <summary>
        /// Sends the specified file to the client for a session.
        /// </summary>
        /// <param name="fileInfo">
        ///   <para>
        ///   A <see cref="FileInfo"/> that specifies the file to send.
        ///   </para>
        ///   <para>
        ///   The file is sent as the binary data.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session has not started yet.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket interface is not Open.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The file does not exist.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The file could not be opened.
        ///   </para>
        /// </exception>
        protected void Send(FileInfo fileInfo)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.Send(fileInfo);
        }

        /// <summary>
        /// Sends the specified data to the client for a session.
        /// </summary>
        /// <param name="data">
        /// A <see cref="string"/> that specifies the text data to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session has not started yet.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket interface is not Open.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="data"/> could not be UTF-8-encoded.
        /// </exception>
        protected void Send(string data)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.Send(data);
        }

        /// <summary>
        /// Sends the data from the specified stream instance to the client for
        /// a session.
        /// </summary>
        /// <param name="stream">
        ///   <para>
        ///   A <see cref="Stream"/> instance from which to read the data to send.
        ///   </para>
        ///   <para>
        ///   The data is sent as the binary data.
        ///   </para>
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session has not started yet.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket interface is not Open.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="stream"/> cannot be read.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="length"/> is less than 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   No data could be read from <paramref name="stream"/>.
        ///   </para>
        /// </exception>
        protected void Send(Stream stream, int length)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.Send(stream, length);
        }

        /// <summary>
        /// Sends the specified data to the client for a session asynchronously.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// An array of <see cref="byte"/> that specifies the binary data to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="T:System.Action{bool}"/> delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   The <see cref="bool"/> parameter passed to the method is <c>true</c>
        ///   if the send has successfully done; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session has not started yet.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket interface is not Open.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        protected void SendAsync(byte[] data, Action<bool> completed)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.SendAsync(data, completed);
        }

        /// <summary>
        /// Sends the specified file to the client for a session asynchronously.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="fileInfo">
        ///   <para>
        ///   A <see cref="FileInfo"/> that specifies the file to send.
        ///   </para>
        ///   <para>
        ///   The file is sent as the binary data.
        ///   </para>
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="T:System.Action{bool}"/> delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   The <see cref="bool"/> parameter passed to the method is <c>true</c>
        ///   if the send has successfully done; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session has not started yet.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket interface is not Open.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="fileInfo"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The file does not exist.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The file could not be opened.
        ///   </para>
        /// </exception>
        protected void SendAsync(FileInfo fileInfo, Action<bool> completed)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.SendAsync(fileInfo, completed);
        }

        /// <summary>
        /// Sends the specified data to the client for a session asynchronously.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="data">
        /// A <see cref="string"/> that specifies the text data to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="T:System.Action{bool}"/> delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   The <see cref="bool"/> parameter passed to the method is <c>true</c>
        ///   if the send has successfully done; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session has not started yet.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket interface is not Open.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="data"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="data"/> could not be UTF-8-encoded.
        /// </exception>
        protected void SendAsync(string data, Action<bool> completed)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.SendAsync(data, completed);
        }

        /// <summary>
        /// Sends the data from the specified stream instance to the client for
        /// a session asynchronously.
        /// </summary>
        /// <remarks>
        /// This method does not wait for the send to be complete.
        /// </remarks>
        /// <param name="stream">
        ///   <para>
        ///   A <see cref="Stream"/> instance from which to read the data to send.
        ///   </para>
        ///   <para>
        ///   The data is sent as the binary data.
        ///   </para>
        /// </param>
        /// <param name="length">
        /// An <see cref="int"/> that specifies the number of bytes to send.
        /// </param>
        /// <param name="completed">
        ///   <para>
        ///   An <see cref="T:System.Action{bool}"/> delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the send is complete.
        ///   </para>
        ///   <para>
        ///   The <see cref="bool"/> parameter passed to the method is <c>true</c>
        ///   if the send has successfully done; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   The session has not started yet.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The current state of the WebSocket interface is not Open.
        ///   </para>
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="stream"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="stream"/> cannot be read.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="length"/> is less than 1.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   No data could be read from <paramref name="stream"/>.
        ///   </para>
        /// </exception>
        protected void SendAsync(Stream stream, int length, Action<bool> completed)
        {
            if (this._websocket == null) {
                var msg = "The session has not started yet.";

                throw new InvalidOperationException(msg);
            }

            this._websocket.SendAsync(stream, length, completed);
        }

        #endregion

        #region Explicit Interface Implementations

        /// <summary>
        /// Gets the WebSocket interface for a session.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="WebSocket"/> that represents
        ///   the interface.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if the session has not started yet.
        ///   </para>
        /// </value>
        WebSocket IWebSocketSession.WebSocket => this._websocket;

        #endregion
    }
}
