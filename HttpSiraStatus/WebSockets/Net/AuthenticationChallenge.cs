#region License
/*
 * AuthenticationChallenge.cs
 *
 * The MIT License
 *
 * Copyright (c) 2013-2023 sta.blockhead
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
using System.Collections.Specialized;
using System.Text;

namespace HttpSiraStatus.WebSockets.Net
{
    internal class AuthenticationChallenge
    {
        #region Private Fields

        #endregion

        #region Private Constructors

        private AuthenticationChallenge(
          AuthenticationSchemes scheme, NameValueCollection parameters
        )
        {
            this.Scheme = scheme;
            this.Parameters = parameters;
        }

        #endregion

        #region Internal Constructors

        internal AuthenticationChallenge(
          AuthenticationSchemes scheme, string realm
        )
          : this(scheme, new NameValueCollection())
        {
            this.Parameters["realm"] = realm;

            if (scheme == AuthenticationSchemes.Digest) {
                this.Parameters["nonce"] = CreateNonceValue();
                this.Parameters["algorithm"] = "MD5";
                this.Parameters["qop"] = "auth";
            }
        }

        #endregion

        #region Internal Properties

        internal NameValueCollection Parameters { get; }

        #endregion

        #region Public Properties

        public string Algorithm => this.Parameters["algorithm"];

        public string Domain => this.Parameters["domain"];

        public string Nonce => this.Parameters["nonce"];

        public string Opaque => this.Parameters["opaque"];

        public string Qop => this.Parameters["qop"];

        public string Realm => this.Parameters["realm"];

        public AuthenticationSchemes Scheme { get; }

        public string Stale => this.Parameters["stale"];

        #endregion

        #region Internal Methods

        internal static AuthenticationChallenge CreateBasicChallenge(string realm)
        {
            return new AuthenticationChallenge(AuthenticationSchemes.Basic, realm);
        }

        internal static AuthenticationChallenge CreateDigestChallenge(string realm)
        {
            return new AuthenticationChallenge(AuthenticationSchemes.Digest, realm);
        }

        internal static string CreateNonceValue()
        {
            var rand = new Random();
            var bytes = new byte[16];

            rand.NextBytes(bytes);

            var buff = new StringBuilder(32);

            foreach (var b in bytes) {
                _ = buff.Append(b.ToString("x2"));
            }

            return buff.ToString();
        }

        internal static AuthenticationChallenge Parse(string value)
        {
            var chal = value.Split(new[] { ' ' }, 2);

            if (chal.Length != 2) {
                return null;
            }

            var schm = chal[0].ToLower();

            if (schm == "basic") {
                var parameters = ParseParameters(chal[1]);

                return new AuthenticationChallenge(
                         AuthenticationSchemes.Basic, parameters
                       );
            }

            if (schm == "digest") {
                var parameters = ParseParameters(chal[1]);

                return new AuthenticationChallenge(
                         AuthenticationSchemes.Digest, parameters
                       );
            }

            return null;
        }

        internal static NameValueCollection ParseParameters(string value)
        {
            var ret = new NameValueCollection();

            foreach (var param in value.SplitHeaderValue(',')) {
                var i = param.IndexOf('=');

                var name = i > 0 ? param.Substring(0, i).Trim() : null;
                var val = i < 0
                          ? param.Trim().Trim('"')
                          : i < param.Length - 1
                            ? param.Substring(i + 1).Trim().Trim('"')
                            : string.Empty;

                ret.Add(name, val);
            }

            return ret;
        }

        internal string ToBasicString()
        {
            return string.Format("Basic realm=\"{0}\"", this.Parameters["realm"]);
        }

        internal string ToDigestString()
        {
            var buff = new StringBuilder(128);

            var domain = this.Parameters["domain"];
            var realm = this.Parameters["realm"];
            var nonce = this.Parameters["nonce"];

            _ = domain != null
                ? buff.AppendFormat(
                  "Digest realm=\"{0}\", domain=\"{1}\", nonce=\"{2}\"",
                  realm,
                  domain,
                  nonce
                )
                : buff.AppendFormat("Digest realm=\"{0}\", nonce=\"{1}\"", realm, nonce);

            var opaque = this.Parameters["opaque"];

            if (opaque != null) {
                _ = buff.AppendFormat(", opaque=\"{0}\"", opaque);
            }

            var stale = this.Parameters["stale"];

            if (stale != null) {
                _ = buff.AppendFormat(", stale={0}", stale);
            }

            var algo = this.Parameters["algorithm"];

            if (algo != null) {
                _ = buff.AppendFormat(", algorithm={0}", algo);
            }

            var qop = this.Parameters["qop"];

            if (qop != null) {
                _ = buff.AppendFormat(", qop=\"{0}\"", qop);
            }

            return buff.ToString();
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            return this.Scheme == AuthenticationSchemes.Basic
                ? this.ToBasicString()
                : this.Scheme == AuthenticationSchemes.Digest ? this.ToDigestString() : string.Empty;
        }

        #endregion
    }
}
