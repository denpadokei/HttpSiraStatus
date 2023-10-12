#region License
/*
 * AuthenticationResponse.cs
 *
 * ParseBasicCredentials is derived from HttpListenerContext.cs (System.Net) of
 * Mono (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Novell, Inc. (http://www.novell.com)
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
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace HttpSiraStatus.WebSockets.Net
{
    internal class AuthenticationResponse
    {
        #region Private Fields

        private uint _nonceCount;

        #endregion

        #region Private Constructors

        private AuthenticationResponse(
          AuthenticationSchemes scheme, NameValueCollection parameters
        )
        {
            this.Scheme = scheme;
            this.Parameters = parameters;
        }

        #endregion

        #region Internal Constructors

        internal AuthenticationResponse(NetworkCredential credentials)
          : this(
              AuthenticationSchemes.Basic,
              new NameValueCollection(),
              credentials,
              0
            )
        {
        }

        internal AuthenticationResponse(
          AuthenticationChallenge challenge,
          NetworkCredential credentials,
          uint nonceCount
        )
          : this(challenge.Scheme, challenge.Parameters, credentials, nonceCount)
        {
        }

        internal AuthenticationResponse(
          AuthenticationSchemes scheme,
          NameValueCollection parameters,
          NetworkCredential credentials,
          uint nonceCount
        )
          : this(scheme, parameters)
        {
            this.Parameters["username"] = credentials.Username;
            this.Parameters["password"] = credentials.Password;
            this.Parameters["uri"] = credentials.Domain;
            this._nonceCount = nonceCount;

            if (scheme == AuthenticationSchemes.Digest) {
                this.initAsDigest();
            }
        }

        #endregion

        #region Internal Properties

        internal uint NonceCount => this._nonceCount < uint.MaxValue
                       ? this._nonceCount
                       : 0;

        internal NameValueCollection Parameters { get; }

        #endregion

        #region Public Properties

        public string Algorithm => this.Parameters["algorithm"];

        public string Cnonce => this.Parameters["cnonce"];

        public string Nc => this.Parameters["nc"];

        public string Nonce => this.Parameters["nonce"];

        public string Opaque => this.Parameters["opaque"];

        public string Password => this.Parameters["password"];

        public string Qop => this.Parameters["qop"];

        public string Realm => this.Parameters["realm"];

        public string Response => this.Parameters["response"];

        public AuthenticationSchemes Scheme { get; }

        public string Uri => this.Parameters["uri"];

        public string UserName => this.Parameters["username"];

        #endregion

        #region Private Methods

        private static string createA1(
          string username, string password, string realm
        )
        {
            return string.Format("{0}:{1}:{2}", username, realm, password);
        }

        private static string createA1(
          string username,
          string password,
          string realm,
          string nonce,
          string cnonce
        )
        {
            var a1 = createA1(username, password, realm);

            return string.Format("{0}:{1}:{2}", hash(a1), nonce, cnonce);
        }

        private static string createA2(string method, string uri)
        {
            return string.Format("{0}:{1}", method, uri);
        }

        private static string createA2(string method, string uri, string entity)
        {
            return string.Format("{0}:{1}:{2}", method, uri, hash(entity));
        }

        private static string hash(string value)
        {
            var md5 = MD5.Create();

            var bytes = Encoding.UTF8.GetBytes(value);
            var res = md5.ComputeHash(bytes);

            var buff = new StringBuilder(64);

            foreach (var b in res) {
                _ = buff.Append(b.ToString("x2"));
            }

            return buff.ToString();
        }

        private void initAsDigest()
        {
            var qops = this.Parameters["qop"];

            if (qops != null) {
                var auth = qops.Split(',').Contains(
                             qop => qop.Trim().ToLower() == "auth"
                           );

                if (auth) {
                    this.Parameters["qop"] = "auth";
                    this.Parameters["cnonce"] = AuthenticationChallenge.CreateNonceValue();
                    this.Parameters["nc"] = string.Format("{0:x8}", ++this._nonceCount);
                }
                else {
                    this.Parameters["qop"] = null;
                }
            }

            this.Parameters["method"] = "GET";
            this.Parameters["response"] = CreateRequestDigest(this.Parameters);
        }

        #endregion

        #region Internal Methods

        internal static string CreateRequestDigest(NameValueCollection parameters)
        {
            var user = parameters["username"];
            var pass = parameters["password"];
            var realm = parameters["realm"];
            var nonce = parameters["nonce"];
            var uri = parameters["uri"];
            var algo = parameters["algorithm"];
            var qop = parameters["qop"];
            var cnonce = parameters["cnonce"];
            var nc = parameters["nc"];
            var method = parameters["method"];

            var a1 = algo != null && algo.ToLower() == "md5-sess"
                     ? createA1(user, pass, realm, nonce, cnonce)
                     : createA1(user, pass, realm);

            var a2 = qop != null && qop.ToLower() == "auth-int"
                     ? createA2(method, uri, parameters["entity"])
                     : createA2(method, uri);

            var secret = hash(a1);
            var data = qop != null
                       ? string.Format(
                           "{0}:{1}:{2}:{3}:{4}", nonce, nc, cnonce, qop, hash(a2)
                         )
                       : string.Format("{0}:{1}", nonce, hash(a2));

            var keyed = string.Format("{0}:{1}", secret, data);

            return hash(keyed);
        }

        internal static AuthenticationResponse Parse(string value)
        {
            try {
                var cred = value.Split(new[] { ' ' }, 2);

                if (cred.Length != 2) {
                    return null;
                }

                var schm = cred[0].ToLower();

                if (schm == "basic") {
                    var parameters = ParseBasicCredentials(cred[1]);

                    return new AuthenticationResponse(
                             AuthenticationSchemes.Basic, parameters
                           );
                }

                if (schm == "digest") {
                    var parameters = AuthenticationChallenge.ParseParameters(cred[1]);

                    return new AuthenticationResponse(
                             AuthenticationSchemes.Digest, parameters
                           );
                }

                return null;
            }
            catch {
                return null;
            }
        }

        internal static NameValueCollection ParseBasicCredentials(string value)
        {
            var ret = new NameValueCollection();

            // Decode the basic-credentials (a Base64 encoded string).

            var bytes = Convert.FromBase64String(value);
            var userPass = Encoding.Default.GetString(bytes);

            // The format is [<domain>\]<username>:<password>.

            var i = userPass.IndexOf(':');
            var user = userPass.Substring(0, i);
            var pass = i < userPass.Length - 1
                       ? userPass.Substring(i + 1)
                       : string.Empty;

            // Check if <domain> exists.

            i = user.IndexOf('\\');

            if (i > -1) {
                user = user.Substring(i + 1);
            }

            ret["username"] = user;
            ret["password"] = pass;

            return ret;
        }

        internal string ToBasicString()
        {
            var user = this.Parameters["username"];
            var pass = this.Parameters["password"];
            var userPass = string.Format("{0}:{1}", user, pass);

            var bytes = Encoding.UTF8.GetBytes(userPass);
            var cred = Convert.ToBase64String(bytes);

            return "Basic " + cred;
        }

        internal string ToDigestString()
        {
            var buff = new StringBuilder(256);

            var user = this.Parameters["username"];
            var realm = this.Parameters["realm"];
            var nonce = this.Parameters["nonce"];
            var uri = this.Parameters["uri"];
            var res = this.Parameters["response"];

            _ = buff.AppendFormat(
              "Digest username=\"{0}\", realm=\"{1}\", nonce=\"{2}\", uri=\"{3}\", response=\"{4}\"",
              user,
              realm,
              nonce,
              uri,
              res
            );

            var opaque = this.Parameters["opaque"];

            if (opaque != null) {
                _ = buff.AppendFormat(", opaque=\"{0}\"", opaque);
            }

            var algo = this.Parameters["algorithm"];

            if (algo != null) {
                _ = buff.AppendFormat(", algorithm={0}", algo);
            }

            var qop = this.Parameters["qop"];

            if (qop != null) {
                var cnonce = this.Parameters["cnonce"];
                var nc = this.Parameters["nc"];

                _ = buff.AppendFormat(
                  ", qop={0}, cnonce=\"{1}\", nc={2}", qop, cnonce, nc
                );
            }

            return buff.ToString();
        }

        #endregion

        #region Public Methods

        public IIdentity ToIdentity()
        {
            if (this.Scheme == AuthenticationSchemes.Basic) {
                var user = this.Parameters["username"];
                var pass = this.Parameters["password"];

                return new HttpBasicIdentity(user, pass);
            }

            return this.Scheme == AuthenticationSchemes.Digest ? new HttpDigestIdentity(this.Parameters) : (IIdentity)null;
        }

        public override string ToString()
        {
            return this.Scheme == AuthenticationSchemes.Basic
                ? this.ToBasicString()
                : this.Scheme == AuthenticationSchemes.Digest ? this.ToDigestString() : string.Empty;
        }

        #endregion
    }
}
