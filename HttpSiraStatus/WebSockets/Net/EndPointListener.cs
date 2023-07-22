#region License
/*
 * EndPointListener.cs
 *
 * This code is derived from EndPointListener.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2020 sta.blockhead
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

#region Contributors
/*
 * Contributors:
 * - Liryna <liryna.stark@gmail.com>
 * - Nicholas Devenish
 */
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace HttpSiraStatus.WebSockets.Net
{
    internal sealed class EndPointListener
    {
        #region Private Fields

        private List<HttpListenerPrefix> _all; // host == '+'
        private readonly Dictionary<HttpConnection, HttpConnection> _connections;
        private readonly object _connectionsSync;
        private static readonly string _defaultCertFolderPath;
        private readonly IPEndPoint _endpoint;
        private List<HttpListenerPrefix> _prefixes;
        private readonly Socket _socket;
        private List<HttpListenerPrefix> _unhandled; // host == '*'

        #endregion

        #region Static Constructor

        static EndPointListener()
        {
            _defaultCertFolderPath = Environment.GetFolderPath(
                                       Environment.SpecialFolder.ApplicationData
                                     );
        }

        #endregion

        #region Internal Constructors

        internal EndPointListener(
          IPEndPoint endpoint,
          bool secure,
          string certificateFolderPath,
          ServerSslConfiguration sslConfig,
          bool reuseAddress
        )
        {
            this._endpoint = endpoint;

            if (secure) {
                var cert = getCertificate(
                             endpoint.Port,
                             certificateFolderPath,
                             sslConfig.ServerCertificate
                           );

                if (cert == null) {
                    var msg = "No server certificate could be found.";

                    throw new ArgumentException(msg);
                }

                this.IsSecure = true;
                this.SslConfiguration = new ServerSslConfiguration(sslConfig)
                {
                    ServerCertificate = cert
                };
            }

            this._prefixes = new List<HttpListenerPrefix>();
            this._connections = new Dictionary<HttpConnection, HttpConnection>();
            this._connectionsSync = ((ICollection)this._connections).SyncRoot;

            this._socket = new Socket(
                  endpoint.Address.AddressFamily,
                  SocketType.Stream,
                  ProtocolType.Tcp
                );

            if (reuseAddress) {
                this._socket.SetSocketOption(
          SocketOptionLevel.Socket,
          SocketOptionName.ReuseAddress,
          true
        );
            }

            this._socket.Bind(endpoint);
            this._socket.Listen(500);
            _ = this._socket.BeginAccept(onAccept, this);
        }

        #endregion

        #region Public Properties

        public IPAddress Address => this._endpoint.Address;

        public bool IsSecure { get; }

        public int Port => this._endpoint.Port;

        public ServerSslConfiguration SslConfiguration { get; }

        #endregion

        #region Private Methods

        private static void addSpecial(
          List<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix
        )
        {
            var path = prefix.Path;

            foreach (var pref in prefixes) {
                if (pref.Path == path) {
                    var msg = "The prefix is already in use.";

                    throw new HttpListenerException(87, msg);
                }
            }

            prefixes.Add(prefix);
        }

        private void clearConnections()
        {
            HttpConnection[] conns = null;

            lock (this._connectionsSync) {
                var cnt = this._connections.Count;

                if (cnt == 0) {
                    return;
                }

                conns = new HttpConnection[cnt];

                var vals = this._connections.Values;
                vals.CopyTo(conns, 0);

                this._connections.Clear();
            }

            foreach (var conn in conns) {
                conn.Close(true);
            }
        }

        private static RSACryptoServiceProvider createRSAFromFile(string path)
        {
            var rsa = new RSACryptoServiceProvider();

            var key = File.ReadAllBytes(path);
            rsa.ImportCspBlob(key);

            return rsa;
        }

        private static X509Certificate2 getCertificate(
          int port, string folderPath, X509Certificate2 defaultCertificate
        )
        {
            if (folderPath == null || folderPath.Length == 0) {
                folderPath = _defaultCertFolderPath;
            }

            try {
                var cer = Path.Combine(folderPath, string.Format("{0}.cer", port));
                var key = Path.Combine(folderPath, string.Format("{0}.key", port));

                if (File.Exists(cer) && File.Exists(key)) {
                    var cert = new X509Certificate2(cer)
                    {
                        PrivateKey = createRSAFromFile(key)
                    };

                    return cert;
                }
            }
            catch {
            }

            return defaultCertificate;
        }

        private void leaveIfNoPrefix()
        {
            if (this._prefixes.Count > 0) {
                return;
            }

            var prefs = this._unhandled;

            if (prefs != null && prefs.Count > 0) {
                return;
            }

            prefs = this._all;

            if (prefs != null && prefs.Count > 0) {
                return;
            }

            this.Close();
        }

        private static void onAccept(IAsyncResult asyncResult)
        {
            var lsnr = (EndPointListener)asyncResult.AsyncState;

            Socket sock = null;

            try {
                sock = lsnr._socket.EndAccept(asyncResult);
            }
            catch (ObjectDisposedException) {
                return;
            }
            catch (Exception) {
                // TODO: Logging.
            }

            try {
                _ = lsnr._socket.BeginAccept(onAccept, lsnr);
            }
            catch (Exception) {
                // TODO: Logging.

                sock?.Close();

                return;
            }

            if (sock == null) {
                return;
            }

            processAccepted(sock, lsnr);
        }

        private static void processAccepted(
          Socket socket, EndPointListener listener
        )
        {
            HttpConnection conn;
            try {
                conn = new HttpConnection(socket, listener);
            }
            catch (Exception) {
                // TODO: Logging.

                socket.Close();

                return;
            }

            lock (listener._connectionsSync) {
                listener._connections.Add(conn, conn);
            }

            conn.BeginReadRequest();
        }

        private static bool removeSpecial(
          List<HttpListenerPrefix> prefixes, HttpListenerPrefix prefix
        )
        {
            var path = prefix.Path;
            var cnt = prefixes.Count;

            for (var i = 0; i < cnt; i++) {
                if (prefixes[i].Path == path) {
                    prefixes.RemoveAt(i);

                    return true;
                }
            }

            return false;
        }

        private static HttpListener searchHttpListenerFromSpecial(
          string path, List<HttpListenerPrefix> prefixes
        )
        {
            if (prefixes == null) {
                return null;
            }

            HttpListener ret = null;

            var bestLen = -1;

            foreach (var pref in prefixes) {
                var prefPath = pref.Path;
                var len = prefPath.Length;

                if (len < bestLen) {
                    continue;
                }

                if (path.StartsWith(prefPath, StringComparison.Ordinal)) {
                    bestLen = len;
                    ret = pref.Listener;
                }
            }

            return ret;
        }

        #endregion

        #region Internal Methods

        internal static bool CertificateExists(int port, string folderPath)
        {
            if (folderPath == null || folderPath.Length == 0) {
                folderPath = _defaultCertFolderPath;
            }

            var cer = Path.Combine(folderPath, string.Format("{0}.cer", port));
            var key = Path.Combine(folderPath, string.Format("{0}.key", port));

            return File.Exists(cer) && File.Exists(key);
        }

        internal void RemoveConnection(HttpConnection connection)
        {
            lock (this._connectionsSync) {
                _ = this._connections.Remove(connection);
            }
        }

        internal bool TrySearchHttpListener(Uri uri, out HttpListener listener)
        {
            listener = null;

            if (uri == null) {
                return false;
            }

            var host = uri.Host;
            var dns = Uri.CheckHostName(host) == UriHostNameType.Dns;
            var port = uri.Port.ToString();
            var path = HttpUtility.UrlDecode(uri.AbsolutePath);

            if (path[path.Length - 1] != '/') {
                path += "/";
            }

            if (host != null && host.Length > 0) {
                var prefs = this._prefixes;
                var bestLen = -1;

                foreach (var pref in prefs) {
                    if (dns) {
                        var prefHost = pref.Host;
                        var prefDns = Uri.CheckHostName(prefHost) == UriHostNameType.Dns;

                        if (prefDns) {
                            if (prefHost != host) {
                                continue;
                            }
                        }
                    }

                    if (pref.Port != port) {
                        continue;
                    }

                    var prefPath = pref.Path;
                    var len = prefPath.Length;

                    if (len < bestLen) {
                        continue;
                    }

                    if (path.StartsWith(prefPath, StringComparison.Ordinal)) {
                        bestLen = len;
                        listener = pref.Listener;
                    }
                }

                if (bestLen != -1) {
                    return true;
                }
            }

            listener = searchHttpListenerFromSpecial(path, this._unhandled);

            if (listener != null) {
                return true;
            }

            listener = searchHttpListenerFromSpecial(path, this._all);

            return listener != null;
        }

        #endregion

        #region Public Methods

        public void AddPrefix(HttpListenerPrefix prefix)
        {
            List<HttpListenerPrefix> current, future;

            if (prefix.Host == "*") {
                do {
                    current = this._unhandled;
                    future = current != null
                             ? new List<HttpListenerPrefix>(current)
                             : new List<HttpListenerPrefix>();

                    addSpecial(future, prefix);
                }
                while (
                  Interlocked.CompareExchange(ref this._unhandled, future, current) != current
                );

                return;
            }

            if (prefix.Host == "+") {
                do {
                    current = this._all;
                    future = current != null
                             ? new List<HttpListenerPrefix>(current)
                             : new List<HttpListenerPrefix>();

                    addSpecial(future, prefix);
                }
                while (
                  Interlocked.CompareExchange(ref this._all, future, current) != current
                );

                return;
            }

            do {
                current = this._prefixes;
                var idx = current.IndexOf(prefix);

                if (idx > -1) {
                    if (current[idx].Listener != prefix.Listener) {
                        var msg = string.Format(
                                    "There is another listener for {0}.", prefix
                                  );

                        throw new HttpListenerException(87, msg);
                    }

                    return;
                }

                future = new List<HttpListenerPrefix>(current)
                {
                    prefix
                };
            }
            while (
              Interlocked.CompareExchange(ref this._prefixes, future, current) != current
            );
        }

        public void Close()
        {
            this._socket.Close();

            this.clearConnections();
            _ = EndPointManager.RemoveEndPoint(this._endpoint);
        }

        public void RemovePrefix(HttpListenerPrefix prefix)
        {
            List<HttpListenerPrefix> current, future;

            if (prefix.Host == "*") {
                do {
                    current = this._unhandled;

                    if (current == null) {
                        break;
                    }

                    future = new List<HttpListenerPrefix>(current);

                    if (!removeSpecial(future, prefix)) {
                        break;
                    }
                }
                while (
                  Interlocked.CompareExchange(ref this._unhandled, future, current) != current
                );

                this.leaveIfNoPrefix();

                return;
            }

            if (prefix.Host == "+") {
                do {
                    current = this._all;

                    if (current == null) {
                        break;
                    }

                    future = new List<HttpListenerPrefix>(current);

                    if (!removeSpecial(future, prefix)) {
                        break;
                    }
                }
                while (
                  Interlocked.CompareExchange(ref this._all, future, current) != current
                );

                this.leaveIfNoPrefix();

                return;
            }

            do {
                current = this._prefixes;

                if (!current.Contains(prefix)) {
                    break;
                }

                future = new List<HttpListenerPrefix>(current);
                _ = future.Remove(prefix);
            }
            while (
              Interlocked.CompareExchange(ref this._prefixes, future, current) != current
            );

            this.leaveIfNoPrefix();
        }

        #endregion
    }
}
