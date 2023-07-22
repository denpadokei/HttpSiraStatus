#region License
/*
 * WebSocketServer.cs
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
 * - Juan Manuel Lallana <juan.manuel.lallana@gmail.com>
 * - Jonas Hovgaard <j@jhovgaard.dk>
 * - Liryna <liryna.stark@gmail.com>
 * - Rohan Singh <rohan-singh@hotmail.com>
 */
#endregion

using HttpSiraStatus.WebSockets.Net;
using HttpSiraStatus.WebSockets.Net.WebSockets;
using System;
using System.Net.Sockets;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace HttpSiraStatus.WebSockets.Server
{
    /// <summary>
    /// Provides a WebSocket protocol server.
    /// </summary>
    /// <remarks>
    /// This class can provide multiple WebSocket services.
    /// </remarks>
    public class WebSocketServer
    {
        #region Private Fields

        private AuthenticationSchemes _authSchemes;
        private static readonly string _defaultRealm;
        private bool _dnsStyle;
        private string _hostname;
        private TcpListener _listener;
        private string _realm;
        private string _realmInUse;
        private Thread _receiveThread;
        private bool _reuseAddress;
        private ServerSslConfiguration _sslConfig;
        private ServerSslConfiguration _sslConfigInUse;
        private volatile ServerState _state;
        private object _sync;
        private Func<IIdentity, NetworkCredential> _userCredFinder;

        #endregion

        #region Static Constructor

        static WebSocketServer()
        {
            _defaultRealm = "SECRET AREA";
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class.
        /// </summary>
        /// <remarks>
        /// The new instance listens for incoming handshake requests on
        /// <see cref="System.Net.IPAddress.Any"/> and port 80.
        /// </remarks>
        public WebSocketServer()
        {
            var addr = System.Net.IPAddress.Any;

            this.init(addr.ToString(), addr, 80, false);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified port.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The new instance listens for incoming handshake requests on
        ///   <see cref="System.Net.IPAddress.Any"/> and <paramref name="port"/>.
        ///   </para>
        ///   <para>
        ///   It provides secure connections if <paramref name="port"/> is 443.
        ///   </para>
        /// </remarks>
        /// <param name="port">
        /// An <see cref="int"/> that specifies the number of the port on which
        /// to listen.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="port"/> is less than 1 or greater than 65535.
        /// </exception>
        public WebSocketServer(int port)
          : this(port, port == 443)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified URL.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The new instance listens for incoming handshake requests on
        ///   the IP address and port of <paramref name="url"/>.
        ///   </para>
        ///   <para>
        ///   Either port 80 or 443 is used if <paramref name="url"/> includes
        ///   no port. Port 443 is used if the scheme of <paramref name="url"/>
        ///   is wss; otherwise, port 80 is used.
        ///   </para>
        ///   <para>
        ///   The new instance provides secure connections if the scheme of
        ///   <paramref name="url"/> is wss.
        ///   </para>
        /// </remarks>
        /// <param name="url">
        /// A <see cref="string"/> that specifies the WebSocket URL of the server.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="url"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="url"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="url"/> is invalid.
        ///   </para>
        /// </exception>
        public WebSocketServer(string url)
        {
            if (url == null) {
                throw new ArgumentNullException("url");
            }

            if (url.Length == 0) {
                throw new ArgumentException("An empty string.", "url");
            }

            if (!tryCreateUri(url, out var uri, out var msg)) {
                throw new ArgumentException(msg, "url");
            }

            var host = uri.DnsSafeHost;
            var addr = host.ToIPAddress();

            if (addr == null) {
                msg = "The host part could not be converted to an IP address.";

                throw new ArgumentException(msg, "url");
            }

            if (!addr.IsLocal()) {
                msg = "The IP address of the host is not a local IP address.";

                throw new ArgumentException(msg, "url");
            }

            this.init(host, addr, uri.Port, uri.Scheme == "wss");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified port and boolean if secure or not.
        /// </summary>
        /// <remarks>
        /// The new instance listens for incoming handshake requests on
        /// <see cref="System.Net.IPAddress.Any"/> and <paramref name="port"/>.
        /// </remarks>
        /// <param name="port">
        /// An <see cref="int"/> that specifies the number of the port on which
        /// to listen.
        /// </param>
        /// <param name="secure">
        /// A <see cref="bool"/>: <c>true</c> if the new instance provides
        /// secure connections; otherwise, <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="port"/> is less than 1 or greater than 65535.
        /// </exception>
        public WebSocketServer(int port, bool secure)
        {
            if (!port.IsPortNumber()) {
                var msg = "Less than 1 or greater than 65535.";

                throw new ArgumentOutOfRangeException("port", msg);
            }

            var addr = System.Net.IPAddress.Any;

            this.init(addr.ToString(), addr, port, secure);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified IP address and port.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   The new instance listens for incoming handshake requests on
        ///   <paramref name="address"/> and <paramref name="port"/>.
        ///   </para>
        ///   <para>
        ///   It provides secure connections if <paramref name="port"/> is 443.
        ///   </para>
        /// </remarks>
        /// <param name="address">
        /// A <see cref="System.Net.IPAddress"/> that specifies the local IP
        /// address on which to listen.
        /// </param>
        /// <param name="port">
        /// An <see cref="int"/> that specifies the number of the port on which
        /// to listen.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="address"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="address"/> is not a local IP address.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="port"/> is less than 1 or greater than 65535.
        /// </exception>
        public WebSocketServer(System.Net.IPAddress address, int port)
          : this(address, port, port == 443)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class
        /// with the specified IP address, port, and boolean if secure or not.
        /// </summary>
        /// <remarks>
        /// The new instance listens for incoming handshake requests on
        /// <paramref name="address"/> and <paramref name="port"/>.
        /// </remarks>
        /// <param name="address">
        /// A <see cref="System.Net.IPAddress"/> that specifies the local IP
        /// address on which to listen.
        /// </param>
        /// <param name="port">
        /// An <see cref="int"/> that specifies the number of the port on which
        /// to listen.
        /// </param>
        /// <param name="secure">
        /// A <see cref="bool"/>: <c>true</c> if the new instance provides
        /// secure connections; otherwise, <c>false</c>.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="address"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="address"/> is not a local IP address.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="port"/> is less than 1 or greater than 65535.
        /// </exception>
        public WebSocketServer(System.Net.IPAddress address, int port, bool secure)
        {
            if (address == null) {
                throw new ArgumentNullException("address");
            }

            if (!address.IsLocal()) {
                var msg = "Not a local IP address.";

                throw new ArgumentException(msg, "address");
            }

            if (!port.IsPortNumber()) {
                var msg = "Less than 1 or greater than 65535.";

                throw new ArgumentOutOfRangeException("port", msg);
            }

            this.init(address.ToString(), address, port, secure);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the IP address of the server.
        /// </summary>
        /// <value>
        /// A <see cref="System.Net.IPAddress"/> that represents the local IP
        /// address on which to listen for incoming handshake requests.
        /// </value>
        public System.Net.IPAddress Address { get; private set; }

        /// <summary>
        /// Gets or sets the scheme used to authenticate the clients.
        /// </summary>
        /// <remarks>
        /// The set operation works if the current state of the server is
        /// Ready or Stop.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   One of the <see cref="Net.AuthenticationSchemes"/>
        ///   enum values.
        ///   </para>
        ///   <para>
        ///   It represents the scheme used to authenticate the clients.
        ///   </para>
        ///   <para>
        ///   The default value is
        ///   <see cref="AuthenticationSchemes.Anonymous"/>.
        ///   </para>
        /// </value>
        public AuthenticationSchemes AuthenticationSchemes
        {
            get => this._authSchemes;

            set
            {
                lock (this._sync) {
                    if (!this.canSet()) {
                        return;
                    }

                    this._authSchemes = value;
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the server has started.
        /// </summary>
        /// <value>
        /// <c>true</c> if the server has started; otherwise, <c>false</c>.
        /// </value>
        public bool IsListening => this._state == ServerState.Start;

        /// <summary>
        /// Gets a value indicating whether the server provides secure connections.
        /// </summary>
        /// <value>
        /// <c>true</c> if the server provides secure connections; otherwise,
        /// <c>false</c>.
        /// </value>
        public bool IsSecure { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the server cleans up
        /// the inactive sessions periodically.
        /// </summary>
        /// <remarks>
        /// The set operation works if the current state of the server is
        /// Ready or Stop.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the server cleans up the inactive sessions
        ///   every 60 seconds; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>true</c>.
        ///   </para>
        /// </value>
        public bool KeepClean
        {
            get => this.WebSocketServices.KeepClean;

            set => this.WebSocketServices.KeepClean = value;
        }

        /// <summary>
        /// Gets the logging function for the server.
        /// </summary>
        /// <remarks>
        /// The default logging level is <see cref="LogLevel.Error"/>.
        /// </remarks>
        /// <value>
        /// A <see cref="Logger"/> that provides the logging function.
        /// </value>
        public Logger Log { get; private set; }

        /// <summary>
        /// Gets the port of the server.
        /// </summary>
        /// <value>
        /// An <see cref="int"/> that represents the number of the port on which
        /// to listen for incoming handshake requests.
        /// </value>
        public int Port { get; private set; }

        /// <summary>
        /// Gets or sets the name of the realm associated with the server.
        /// </summary>
        /// <remarks>
        /// The set operation works if the current state of the server is
        /// Ready or Stop.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the name of the realm.
        ///   </para>
        ///   <para>
        ///   "SECRET AREA" is used as the name of the realm if the value is
        ///   <see langword="null"/> or an empty string.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        public string Realm
        {
            get => this._realm;

            set
            {
                lock (this._sync) {
                    if (!this.canSet()) {
                        return;
                    }

                    this._realm = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the server is allowed to
        /// be bound to an address that is already in use.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   You should set this property to <c>true</c> if you would like to
        ///   resolve to wait for socket in TIME_WAIT state.
        ///   </para>
        ///   <para>
        ///   The set operation works if the current state of the server is
        ///   Ready or Stop.
        ///   </para>
        /// </remarks>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the server is allowed to be bound to an address
        ///   that is already in use; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool ReuseAddress
        {
            get => this._reuseAddress;

            set
            {
                lock (this._sync) {
                    if (!this.canSet()) {
                        return;
                    }

                    this._reuseAddress = value;
                }
            }
        }

        /// <summary>
        /// Gets the configuration for secure connection.
        /// </summary>
        /// <remarks>
        /// The configuration is used when the server attempts to start,
        /// so it must be configured before the start method is called.
        /// </remarks>
        /// <value>
        /// A <see cref="ServerSslConfiguration"/> that represents the
        /// configuration used to provide secure connections.
        /// </value>
        /// <exception cref="InvalidOperationException">
        /// The server does not provide secure connections.
        /// </exception>
        public ServerSslConfiguration SslConfiguration
        {
            get
            {
                if (!this.IsSecure) {
                    var msg = "The server does not provide secure connections.";

                    throw new InvalidOperationException(msg);
                }

                return this.getSslConfiguration();
            }
        }

        /// <summary>
        /// Gets or sets the delegate used to find the credentials for an identity.
        /// </summary>
        /// <remarks>
        /// The set operation works if the current state of the server is
        /// Ready or Stop.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="T:System.Func{IIdentity, NetworkCredential}"/>
        ///   delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the server finds
        ///   the credentials used to authenticate a client.
        ///   </para>
        ///   <para>
        ///   The method must return <see langword="null"/> if the credentials
        ///   are not found.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        public Func<IIdentity, NetworkCredential> UserCredentialsFinder
        {
            get => this._userCredFinder;

            set
            {
                lock (this._sync) {
                    if (!this.canSet()) {
                        return;
                    }

                    this._userCredFinder = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the time to wait for the response to the WebSocket
        /// Ping or Close.
        /// </summary>
        /// <remarks>
        /// The set operation works if the current state of the server is
        /// Ready or Stop.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="TimeSpan"/> that represents the time to wait for
        ///   the response.
        ///   </para>
        ///   <para>
        ///   The default value is the same as 1 second.
        ///   </para>
        /// </value>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The value specified for a set operation is zero or less.
        /// </exception>
        public TimeSpan WaitTime
        {
            get => this.WebSocketServices.WaitTime;

            set => this.WebSocketServices.WaitTime = value;
        }

        /// <summary>
        /// Gets the management function for the WebSocket services provided by
        /// the server.
        /// </summary>
        /// <value>
        /// A <see cref="WebSocketServiceManager"/> that manages the WebSocket
        /// services provided by the server.
        /// </value>
        public WebSocketServiceManager WebSocketServices { get; private set; }

        #endregion

        #region Private Methods

        private void abort()
        {
            lock (this._sync) {
                if (this._state != ServerState.Start) {
                    return;
                }

                this._state = ServerState.ShuttingDown;
            }

            try {
                this._listener.Stop();
            }
            catch (Exception ex) {
                this.Log.Fatal(ex.Message);
                this.Log.Debug(ex.ToString());
            }

            try {
                this.WebSocketServices.Stop(1006, string.Empty);
            }
            catch (Exception ex) {
                this.Log.Fatal(ex.Message);
                this.Log.Debug(ex.ToString());
            }

            this._state = ServerState.Stop;
        }

        private bool authenticateClient(TcpListenerWebSocketContext context)
        {
            if (this._authSchemes == AuthenticationSchemes.Anonymous) {
                return true;
            }

            if (this._authSchemes == AuthenticationSchemes.None) {
                return false;
            }

            var chal = new AuthenticationChallenge(this._authSchemes, this._realmInUse)
                       .ToString();

            var retry = -1;
            bool auth()
            {
                retry++;

                if (retry > 99) {
                    return false;
                }

                if (context.SetUser(this._authSchemes, this._realmInUse, this._userCredFinder)) {
                    return true;
                }

                context.SendAuthenticationChallenge(chal);

                return auth();
            }

            return auth();
        }

        private bool canSet()
        {
            return this._state == ServerState.Ready || this._state == ServerState.Stop;
        }

        private bool checkHostNameForRequest(string name)
        {
            return !this._dnsStyle
                   || Uri.CheckHostName(name) != UriHostNameType.Dns
                   || name == this._hostname;
        }

        private string getRealm()
        {
            var realm = this._realm;

            return realm != null && realm.Length > 0 ? realm : _defaultRealm;
        }

        private ServerSslConfiguration getSslConfiguration()
        {
            this._sslConfig ??= new ServerSslConfiguration();

            return this._sslConfig;
        }

        private void init(
          string hostname, System.Net.IPAddress address, int port, bool secure
        )
        {
            this._hostname = hostname;
            this.Address = address;
            this.Port = port;
            this.IsSecure = secure;

            this._authSchemes = AuthenticationSchemes.Anonymous;
            this._dnsStyle = Uri.CheckHostName(hostname) == UriHostNameType.Dns;
            this._listener = new TcpListener(address, port);
            this.Log = new Logger();
            this.WebSocketServices = new WebSocketServiceManager(this.Log);
            this._sync = new object();
        }

        private void processRequest(TcpListenerWebSocketContext context)
        {
            if (!this.authenticateClient(context)) {
                context.Close(HttpStatusCode.Forbidden);

                return;
            }

            var uri = context.RequestUri;

            if (uri == null) {
                context.Close(HttpStatusCode.BadRequest);

                return;
            }

            var name = uri.DnsSafeHost;

            if (!this.checkHostNameForRequest(name)) {
                context.Close(HttpStatusCode.NotFound);

                return;
            }

            var path = uri.AbsolutePath;

            if (path.IndexOfAny(new[] { '%', '+' }) > -1) {
                path = HttpUtility.UrlDecode(path, Encoding.UTF8);
            }

            if (!this.WebSocketServices.InternalTryGetServiceHost(path, out var host)) {
                context.Close(HttpStatusCode.NotImplemented);

                return;
            }

            host.StartSession(context);
        }

        private void receiveRequest()
        {
            while (true) {
                TcpClient cl = null;

                try {
                    cl = this._listener.AcceptTcpClient();

                    _ = ThreadPool.QueueUserWorkItem(
                      state =>
                      {
                          try {
                              var ctx = new TcpListenerWebSocketContext(
                                cl, null, this.IsSecure, this._sslConfigInUse, this.Log
                              );

                              this.processRequest(ctx);
                          }
                          catch (Exception ex) {
                              this.Log.Error(ex.Message);
                              this.Log.Debug(ex.ToString());

                              cl.Close();
                          }
                      }
                    );
                }
                catch (SocketException ex) {
                    if (this._state == ServerState.ShuttingDown) {
                        return;
                    }

                    this.Log.Fatal(ex.Message);
                    this.Log.Debug(ex.ToString());

                    break;
                }
                catch (InvalidOperationException ex) {
                    if (this._state == ServerState.ShuttingDown) {
                        return;
                    }

                    this.Log.Fatal(ex.Message);
                    this.Log.Debug(ex.ToString());

                    break;
                }
                catch (Exception ex) {
                    this.Log.Fatal(ex.Message);
                    this.Log.Debug(ex.ToString());

                    cl?.Close();

                    if (this._state == ServerState.ShuttingDown) {
                        return;
                    }

                    break;
                }
            }

            this.abort();
        }

        private void start()
        {
            lock (this._sync) {
                if (this._state == ServerState.Start || this._state == ServerState.ShuttingDown) {
                    return;
                }

                if (this.IsSecure) {
                    var src = this.getSslConfiguration();
                    var conf = new ServerSslConfiguration(src);

                    if (conf.ServerCertificate == null) {
                        var msg = "There is no server certificate for secure connection.";

                        throw new InvalidOperationException(msg);
                    }

                    this._sslConfigInUse = conf;
                }

                this._realmInUse = this.getRealm();

                this.WebSocketServices.Start();

                try {
                    this.startReceiving();
                }
                catch {
                    this.WebSocketServices.Stop(1011, string.Empty);

                    throw;
                }

                this._state = ServerState.Start;
            }
        }

        private void startReceiving()
        {
            if (this._reuseAddress) {
                this._listener.Server.SetSocketOption(
          SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true
        );
            }

            try {
                this._listener.Start();
            }
            catch (Exception ex) {
                var msg = "The underlying listener has failed to start.";

                throw new InvalidOperationException(msg, ex);
            }

            var receiver = new ThreadStart(this.receiveRequest);
            this._receiveThread = new Thread(receiver)
            {
                IsBackground = true
            };

            this._receiveThread.Start();
        }

        private void stop(ushort code, string reason)
        {
            lock (this._sync) {
                if (this._state != ServerState.Start) {
                    return;
                }

                this._state = ServerState.ShuttingDown;
            }

            try {
                var timeout = 5000;

                this.stopReceiving(timeout);
            }
            catch (Exception ex) {
                this.Log.Fatal(ex.Message);
                this.Log.Debug(ex.ToString());
            }

            try {
                this.WebSocketServices.Stop(code, reason);
            }
            catch (Exception ex) {
                this.Log.Fatal(ex.Message);
                this.Log.Debug(ex.ToString());
            }

            this._state = ServerState.Stop;
        }

        private void stopReceiving(int millisecondsTimeout)
        {
            this._listener.Stop();
            _ = this._receiveThread.Join(millisecondsTimeout);
        }

        private static bool tryCreateUri(
          string uriString, out Uri result, out string message
        )
        {
            if (!uriString.TryCreateWebSocketUri(out result, out message)) {
                return false;
            }

            if (result.PathAndQuery != "/") {
                result = null;
                message = "It includes either or both path and query components.";

                return false;
            }

            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a WebSocket service with the specified behavior and path.
        /// </summary>
        /// <param name="path">
        ///   <para>
        ///   A <see cref="string"/> that specifies an absolute path to
        ///   the service to add.
        ///   </para>
        ///   <para>
        ///   / is trimmed from the end of the string if present.
        ///   </para>
        /// </param>
        /// <typeparam name="TBehavior">
        ///   <para>
        ///   The type of the behavior for the service.
        ///   </para>
        ///   <para>
        ///   It must inherit the <see cref="WebSocketBehavior"/> class.
        ///   </para>
        ///   <para>
        ///   Also it must have a public parameterless constructor.
        ///   </para>
        /// </typeparam>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="path"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is not an absolute path.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> includes either or both
        ///   query and fragment components.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is already in use.
        ///   </para>
        /// </exception>
        public void AddWebSocketService<TBehavior>(string path)
          where TBehavior : WebSocketBehavior, new()
        {
            this.WebSocketServices.AddService<TBehavior>(path, null);
        }

        /// <summary>
        /// Adds a WebSocket service with the specified behavior, path,
        /// and initializer.
        /// </summary>
        /// <param name="path">
        ///   <para>
        ///   A <see cref="string"/> that specifies an absolute path to
        ///   the service to add.
        ///   </para>
        ///   <para>
        ///   / is trimmed from the end of the string if present.
        ///   </para>
        /// </param>
        /// <param name="initializer">
        ///   <para>
        ///   An <see cref="T:System.Action{TBehavior}"/> delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the service
        ///   initializes a new session instance.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not necessary.
        ///   </para>
        /// </param>
        /// <typeparam name="TBehavior">
        ///   <para>
        ///   The type of the behavior for the service.
        ///   </para>
        ///   <para>
        ///   It must inherit the <see cref="WebSocketBehavior"/> class.
        ///   </para>
        ///   <para>
        ///   Also it must have a public parameterless constructor.
        ///   </para>
        /// </typeparam>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="path"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is not an absolute path.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> includes either or both
        ///   query and fragment components.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is already in use.
        ///   </para>
        /// </exception>
        public void AddWebSocketService<TBehavior>(
          string path, Action<TBehavior> initializer
        )
          where TBehavior : WebSocketBehavior, new()
        {
            this.WebSocketServices.AddService(path, initializer);
        }

        /// <summary>
        /// Removes a WebSocket service with the specified path.
        /// </summary>
        /// <remarks>
        /// The service is stopped with close status 1001 (going away)
        /// if the current state of the service is Start.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the service is successfully found and removed;
        /// otherwise, <c>false</c>.
        /// </returns>
        /// <param name="path">
        ///   <para>
        ///   A <see cref="string"/> that specifies an absolute path to
        ///   the service to remove.
        ///   </para>
        ///   <para>
        ///   / is trimmed from the end of the string if present.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="path"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> is not an absolute path.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="path"/> includes either or both
        ///   query and fragment components.
        ///   </para>
        /// </exception>
        public bool RemoveWebSocketService(string path)
        {
            return this.WebSocketServices.RemoveService(path);
        }

        /// <summary>
        /// Starts receiving incoming handshake requests.
        /// </summary>
        /// <remarks>
        /// This method works if the current state of the server is Ready or Stop.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        ///   <para>
        ///   There is no server certificate for secure connection.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The underlying <see cref="TcpListener"/> has failed to start.
        ///   </para>
        /// </exception>
        public void Start()
        {
            if (this._state == ServerState.Start || this._state == ServerState.ShuttingDown) {
                return;
            }

            this.start();
        }

        /// <summary>
        /// Stops receiving incoming handshake requests.
        /// </summary>
        /// <remarks>
        /// This method works if the current state of the server is Start.
        /// </remarks>
        public void Stop()
        {
            if (this._state != ServerState.Start) {
                return;
            }

            this.stop(1001, string.Empty);
        }

        #endregion
    }
}
