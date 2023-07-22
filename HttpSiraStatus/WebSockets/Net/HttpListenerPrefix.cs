#region License
/*
 * HttpListenerPrefix.cs
 *
 * This code is derived from ListenerPrefix.cs (System.Net) of Mono
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
 * - Oleg Mihailik <mihailik@gmail.com>
 */
#endregion

using System;

namespace HttpSiraStatus.WebSockets.Net
{
    internal sealed class HttpListenerPrefix
    {
        #region Private Fields

        private string _prefix;

        #endregion

        #region Internal Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpListenerPrefix"/> class
        /// with the specified URI prefix and HTTP listener.
        /// </summary>
        /// <remarks>
        /// This constructor must be called after calling the CheckPrefix method.
        /// </remarks>
        /// <param name="uriPrefix">
        /// A <see cref="string"/> that specifies the URI prefix.
        /// </param>
        /// <param name="listener">
        /// A <see cref="HttpListener"/> that specifies the HTTP listener.
        /// </param>
        internal HttpListenerPrefix(string uriPrefix, HttpListener listener)
        {
            this.Original = uriPrefix;
            this.Listener = listener;

            this.parse(uriPrefix);
        }

        #endregion

        #region Public Properties

        public string Host { get; private set; }

        public bool IsSecure { get; private set; }

        public HttpListener Listener { get; }

        public string Original { get; }

        public string Path { get; private set; }

        public string Port { get; private set; }

        #endregion

        #region Private Methods

        private void parse(string uriPrefix)
        {
            if (uriPrefix.StartsWith("https")) {
                this.IsSecure = true;
            }

            var len = uriPrefix.Length;
            var host = uriPrefix.IndexOf(':') + 3;
            var root = uriPrefix.IndexOf('/', host + 1, len - host - 1);

            var colon = uriPrefix.LastIndexOf(':', root - 1, root - host - 1);

            if (uriPrefix[root - 1] != ']' && colon > host) {
                this.Host = uriPrefix.Substring(host, colon - host);
                this.Port = uriPrefix.Substring(colon + 1, root - colon - 1);
            }
            else {
                this.Host = uriPrefix.Substring(host, root - host);
                this.Port = this.IsSecure ? "443" : "80";
            }

            this.Path = uriPrefix.Substring(root);

            this._prefix = string.Format(
                  "{0}://{1}:{2}{3}",
                  this.IsSecure ? "https" : "http",
                  this.Host,
                  this.Port,
                  this.Path
                );
        }

        #endregion

        #region Public Methods

        public static void CheckPrefix(string uriPrefix)
        {
            if (uriPrefix == null) {
                throw new ArgumentNullException("uriPrefix");
            }

            var len = uriPrefix.Length;

            if (len == 0) {
                var msg = "An empty string.";

                throw new ArgumentException(msg, "uriPrefix");
            }

            var schm = uriPrefix.StartsWith("http://")
                       || uriPrefix.StartsWith("https://");

            if (!schm) {
                var msg = "The scheme is not 'http' or 'https'.";

                throw new ArgumentException(msg, "uriPrefix");
            }

            var end = len - 1;

            if (uriPrefix[end] != '/') {
                var msg = "It ends without '/'.";

                throw new ArgumentException(msg, "uriPrefix");
            }

            var host = uriPrefix.IndexOf(':') + 3;

            if (host >= end) {
                var msg = "No host is specified.";

                throw new ArgumentException(msg, "uriPrefix");
            }

            if (uriPrefix[host] == ':') {
                var msg = "No host is specified.";

                throw new ArgumentException(msg, "uriPrefix");
            }

            var root = uriPrefix.IndexOf('/', host, len - host);

            if (root == host) {
                var msg = "No host is specified.";

                throw new ArgumentException(msg, "uriPrefix");
            }

            if (uriPrefix[root - 1] == ':') {
                var msg = "No port is specified.";

                throw new ArgumentException(msg, "uriPrefix");
            }

            if (root == end - 1) {
                var msg = "No path is specified.";

                throw new ArgumentException(msg, "uriPrefix");
            }
        }

        /// <summary>
        /// Determines whether the current instance is equal to the specified
        /// <see cref="object"/> instance.
        /// </summary>
        /// <remarks>
        /// This method will be required to detect duplicates in any collection.
        /// </remarks>
        /// <param name="obj">
        ///   <para>
        ///   An <see cref="object"/> instance to compare to the current instance.
        ///   </para>
        ///   <para>
        ///   An reference to a <see cref="HttpListenerPrefix"/> instance.
        ///   </para>
        /// </param>
        /// <returns>
        /// <c>true</c> if the current instance and <paramref name="obj"/> have
        /// the same URI prefix; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is HttpListenerPrefix pref && this._prefix.Equals(pref._prefix);
        }

        /// <summary>
        /// Gets the hash code for the current instance.
        /// </summary>
        /// <remarks>
        /// This method will be required to detect duplicates in any collection.
        /// </remarks>
        /// <returns>
        /// An <see cref="int"/> that represents the hash code.
        /// </returns>
        public override int GetHashCode()
        {
            return this._prefix.GetHashCode();
        }

        public override string ToString()
        {
            return this._prefix;
        }

        #endregion
    }
}
