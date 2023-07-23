#region License
/*
 * Cookie.cs
 *
 * This code is derived from Cookie.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2004,2009 Novell, Inc. (http://www.novell.com)
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

#region Authors
/*
 * Authors:
 * - Lawrence Pit <loz@cable.a2000.nl>
 * - Gonzalo Paniagua Javier <gonzalo@ximian.com>
 * - Daniel Nauck <dna@mono-project.de>
 * - Sebastien Pouliot <sebastien@ximian.com>
 */
#endregion

using System;
using System.Globalization;
using System.Text;

namespace HttpSiraStatus.WebSockets.Net
{
    /// <summary>
    /// Provides a set of methods and properties used to manage an HTTP cookie.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///   This class refers to the following specifications:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///       <term>
    ///       <see href="http://web.archive.org/web/20020803110822/http://wp.netscape.com/newsref/std/cookie_spec.html">
    ///       Netscape specification</see>
    ///       </term>
    ///     </item>
    ///     <item>
    ///       <term>
    ///       <see href="https://tools.ietf.org/html/rfc2109">RFC 2109</see>
    ///       </term>
    ///     </item>
    ///     <item>
    ///       <term>
    ///       <see href="https://tools.ietf.org/html/rfc2965">RFC 2965</see>
    ///       </term>
    ///     </item>
    ///     <item>
    ///       <term>
    ///       <see href="https://tools.ietf.org/html/rfc6265">RFC 6265</see>
    ///       </term>
    ///     </item>
    ///   </list>
    ///   <para>
    ///   This class cannot be inherited.
    ///   </para>
    /// </remarks>
    [Serializable]
    public sealed class Cookie
    {
        #region Private Fields

        private string _comment;
        private Uri _commentUri;
        private bool _discard;
        private string _domain;
        private static readonly int[] _emptyPorts;
        private DateTime _expires;
        private bool _httpOnly;
        private string _name;
        private string _path;
        private string _port;
        private int[] _ports;
        private static readonly char[] _reservedCharsForValue;
        private string _sameSite;
        private bool _secure;
        private DateTime _timeStamp;
        private string _value;
        private int _version;

        #endregion

        #region Static Constructor

        static Cookie()
        {
            _emptyPorts = new int[0];
            _reservedCharsForValue = new[] { ';', ',' };
        }

        #endregion

        #region Internal Constructors

        internal Cookie()
        {
            this.init(string.Empty, string.Empty, string.Empty, string.Empty);
        }

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Cookie"/> class with
        /// the specified name and value.
        /// </summary>
        /// <param name="name">
        ///   <para>
        ///   A <see cref="string"/> that specifies the name of the cookie.
        ///   </para>
        ///   <para>
        ///   The name must be a token defined in
        ///   <see href="http://tools.ietf.org/html/rfc2616#section-2.2">
        ///   RFC 2616</see>.
        ///   </para>
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that specifies the value of the cookie.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="name"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> starts with a dollar sign.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> contains an invalid character.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="value"/> is a string not enclosed in double quotes
        ///   although it contains a reserved character.
        ///   </para>
        /// </exception>
        public Cookie(string name, string value)
          : this(name, value, string.Empty, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cookie"/> class with
        /// the specified name, value, and path.
        /// </summary>
        /// <param name="name">
        ///   <para>
        ///   A <see cref="string"/> that specifies the name of the cookie.
        ///   </para>
        ///   <para>
        ///   The name must be a token defined in
        ///   <see href="http://tools.ietf.org/html/rfc2616#section-2.2">
        ///   RFC 2616</see>.
        ///   </para>
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that specifies the value of the cookie.
        /// </param>
        /// <param name="path">
        /// A <see cref="string"/> that specifies the value of the Path
        /// attribute of the cookie.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="name"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> starts with a dollar sign.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> contains an invalid character.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="value"/> is a string not enclosed in double quotes
        ///   although it contains a reserved character.
        ///   </para>
        /// </exception>
        public Cookie(string name, string value, string path)
          : this(name, value, path, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cookie"/> class with
        /// the specified name, value, path, and domain.
        /// </summary>
        /// <param name="name">
        ///   <para>
        ///   A <see cref="string"/> that specifies the name of the cookie.
        ///   </para>
        ///   <para>
        ///   The name must be a token defined in
        ///   <see href="http://tools.ietf.org/html/rfc2616#section-2.2">
        ///   RFC 2616</see>.
        ///   </para>
        /// </param>
        /// <param name="value">
        /// A <see cref="string"/> that specifies the value of the cookie.
        /// </param>
        /// <param name="path">
        /// A <see cref="string"/> that specifies the value of the Path
        /// attribute of the cookie.
        /// </param>
        /// <param name="domain">
        /// A <see cref="string"/> that specifies the value of the Domain
        /// attribute of the cookie.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   <paramref name="name"/> is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> starts with a dollar sign.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="name"/> contains an invalid character.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   <paramref name="value"/> is a string not enclosed in double quotes
        ///   although it contains a reserved character.
        ///   </para>
        /// </exception>
        public Cookie(string name, string value, string path, string domain)
        {
            if (name == null) {
                throw new ArgumentNullException("name");
            }

            if (name.Length == 0) {
                throw new ArgumentException("An empty string.", "name");
            }

            if (name[0] == '$') {
                var msg = "It starts with a dollar sign.";

                throw new ArgumentException(msg, "name");
            }

            if (!name.IsToken()) {
                var msg = "It contains an invalid character.";

                throw new ArgumentException(msg, "name");
            }

            value ??= string.Empty;

            if (value.Contains(_reservedCharsForValue)) {
                if (!value.IsEnclosedIn('"')) {
                    var msg = "A string not enclosed in double quotes.";

                    throw new ArgumentException(msg, "value");
                }
            }

            this.init(name, value, path ?? string.Empty, domain ?? string.Empty);
        }

        #endregion

        #region Internal Properties

        internal bool ExactDomain => this._domain.Length == 0 || this._domain[0] != '.';

        internal int MaxAge
        {
            get
            {
                if (this._expires == DateTime.MinValue) {
                    return 0;
                }

                var expires = this._expires.Kind != DateTimeKind.Local
                              ? this._expires.ToLocalTime()
                              : this._expires;

                var span = expires - DateTime.Now;

                return span > TimeSpan.Zero
                       ? (int)span.TotalSeconds
                       : 0;
            }

            set => this._expires = value > 0
                   ? DateTime.Now.AddSeconds(value)
                   : DateTime.Now;
        }

        internal int[] Ports => this._ports ?? _emptyPorts;

        internal string SameSite
        {
            get => this._sameSite;

            set => this._sameSite = value;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the value of the Comment attribute of the cookie.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the comment to document
        ///   intended use of the cookie.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not present.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        public string Comment
        {
            get => this._comment;

            internal set => this._comment = value;
        }

        /// <summary>
        /// Gets the value of the CommentURL attribute of the cookie.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="Uri"/> that represents the URI that provides
        ///   the comment to document intended use of the cookie.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not present.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        public Uri CommentUri
        {
            get => this._commentUri;

            internal set => this._commentUri = value;
        }

        /// <summary>
        /// Gets a value indicating whether the client discards the cookie
        /// unconditionally when the client terminates.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the client discards the cookie unconditionally
        ///   when the client terminates; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool Discard
        {
            get => this._discard;

            internal set => this._discard = value;
        }

        /// <summary>
        /// Gets or sets the value of the Domain attribute of the cookie.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the domain name that
        ///   the cookie is valid for.
        ///   </para>
        ///   <para>
        ///   An empty string if not necessary.
        ///   </para>
        /// </value>
        public string Domain
        {
            get => this._domain;

            set => this._domain = value ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the cookie has expired.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the cookie has expired; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool Expired
        {
            get => this._expires != DateTime.MinValue && this._expires <= DateTime.Now;

            set => this._expires = value ? DateTime.Now : DateTime.MinValue;
        }

        /// <summary>
        /// Gets or sets the value of the Expires attribute of the cookie.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="DateTime"/> that represents the date and time that
        ///   the cookie expires on.
        ///   </para>
        ///   <para>
        ///   <see cref="DateTime.MinValue"/> if not necessary.
        ///   </para>
        ///   <para>
        ///   The default value is <see cref="DateTime.MinValue"/>.
        ///   </para>
        /// </value>
        public DateTime Expires
        {
            get => this._expires;

            set => this._expires = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether non-HTTP APIs can access
        /// the cookie.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if non-HTTP APIs cannot access the cookie; otherwise,
        ///   <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool HttpOnly
        {
            get => this._httpOnly;

            set => this._httpOnly = value;
        }

        /// <summary>
        /// Gets or sets the name of the cookie.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the name of the cookie.
        ///   </para>
        ///   <para>
        ///   The name must be a token defined in
        ///   <see href="http://tools.ietf.org/html/rfc2616#section-2.2">
        ///   RFC 2616</see>.
        ///   </para>
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// The value specified for a set operation is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <para>
        ///   The value specified for a set operation is an empty string.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The value specified for a set operation starts with a dollar sign.
        ///   </para>
        ///   <para>
        ///   -or-
        ///   </para>
        ///   <para>
        ///   The value specified for a set operation contains an invalid character.
        ///   </para>
        /// </exception>
        public string Name
        {
            get => this._name;

            set
            {
                if (value == null) {
                    throw new ArgumentNullException("value");
                }

                if (value.Length == 0) {
                    throw new ArgumentException("An empty string.", "value");
                }

                if (value[0] == '$') {
                    var msg = "It starts with a dollar sign.";

                    throw new ArgumentException(msg, "value");
                }

                if (!value.IsToken()) {
                    var msg = "It contains an invalid character.";

                    throw new ArgumentException(msg, "value");
                }

                this._name = value;
            }
        }

        /// <summary>
        /// Gets or sets the value of the Path attribute of the cookie.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the subset of URI on
        /// the origin server that the cookie applies to.
        /// </value>
        public string Path
        {
            get => this._path;

            set => this._path = value ?? string.Empty;
        }

        /// <summary>
        /// Gets the value of the Port attribute of the cookie.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="string"/> that represents the list of TCP ports
        ///   that the cookie applies to.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not present.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        public string Port
        {
            get => this._port;

            internal set
            {

                if (!tryCreatePorts(value, out var ports)) {
                    return;
                }

                this._ports = ports;
                this._port = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the security level of
        /// the cookie is secure.
        /// </summary>
        /// <remarks>
        /// When this property is <c>true</c>, the cookie may be included in
        /// the request only if the request is transmitted over HTTPS.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the security level of the cookie is secure;
        ///   otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool Secure
        {
            get => this._secure;

            set => this._secure = value;
        }

        /// <summary>
        /// Gets the time when the cookie was issued.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> that represents the time when
        /// the cookie was issued.
        /// </value>
        public DateTime TimeStamp => this._timeStamp;

        /// <summary>
        /// Gets or sets the value of the cookie.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the value of the cookie.
        /// </value>
        /// <exception cref="ArgumentException">
        /// The value specified for a set operation is a string not enclosed in
        /// double quotes although it contains a reserved character.
        /// </exception>
        public string Value
        {
            get => this._value;

            set
            {
                value ??= string.Empty;

                if (value.Contains(_reservedCharsForValue)) {
                    if (!value.IsEnclosedIn('"')) {
                        var msg = "A string not enclosed in double quotes.";

                        throw new ArgumentException(msg, "value");
                    }
                }

                this._value = value;
            }
        }

        /// <summary>
        /// Gets the value of the Version attribute of the cookie.
        /// </summary>
        /// <value>
        ///   <para>
        ///   An <see cref="int"/> that represents the version of HTTP state
        ///   management that the cookie conforms to.
        ///   </para>
        ///   <para>
        ///   0 or 1.
        ///   </para>
        ///   <para>
        ///   0 if not present.
        ///   </para>
        ///   <para>
        ///   The default value is 0.
        ///   </para>
        /// </value>
        public int Version
        {
            get => this._version;

            internal set
            {
                if (value < 0 || value > 1) {
                    return;
                }

                this._version = value;
            }
        }

        #endregion

        #region Private Methods

        private static int hash(int i, int j, int k, int l, int m)
        {
            return i
                   ^ ((j << 13) | (j >> 19))
                   ^ ((k << 26) | (k >> 6))
                   ^ ((l << 7) | (l >> 25))
                   ^ ((m << 20) | (m >> 12));
        }

        private void init(string name, string value, string path, string domain)
        {
            this._name = name;
            this._value = value;
            this._path = path;
            this._domain = domain;

            this._expires = DateTime.MinValue;
            this._timeStamp = DateTime.Now;
        }

        private string toResponseStringVersion0()
        {
            var buff = new StringBuilder(64);

            _ = buff.AppendFormat("{0}={1}", this._name, this._value);

            if (this._expires != DateTime.MinValue) {
                var expires = this._expires
                              .ToUniversalTime()
                              .ToString(
                                "ddd, dd'-'MMM'-'yyyy HH':'mm':'ss 'GMT'",
                                CultureInfo.CreateSpecificCulture("en-US")
                              );

                _ = buff.AppendFormat("; Expires={0}", expires);
            }

            if (!this._path.IsNullOrEmpty()) {
                _ = buff.AppendFormat("; Path={0}", this._path);
            }

            if (!this._domain.IsNullOrEmpty()) {
                _ = buff.AppendFormat("; Domain={0}", this._domain);
            }

            if (!this._sameSite.IsNullOrEmpty()) {
                _ = buff.AppendFormat("; SameSite={0}", this._sameSite);
            }

            if (this._secure) {
                _ = buff.Append("; Secure");
            }

            if (this._httpOnly) {
                _ = buff.Append("; HttpOnly");
            }

            return buff.ToString();
        }

        private string toResponseStringVersion1()
        {
            var buff = new StringBuilder(64);

            _ = buff.AppendFormat("{0}={1}; Version={2}", this._name, this._value, this._version);

            if (this._expires != DateTime.MinValue) {
                _ = buff.AppendFormat("; Max-Age={0}", this.MaxAge);
            }

            if (!this._path.IsNullOrEmpty()) {
                _ = buff.AppendFormat("; Path={0}", this._path);
            }

            if (!this._domain.IsNullOrEmpty()) {
                _ = buff.AppendFormat("; Domain={0}", this._domain);
            }

            if (this._port != null) {
                _ = this._port != "\"\"" ? buff.AppendFormat("; Port={0}", this._port) : buff.Append("; Port");
            }

            if (this._comment != null) {
                var comment = HttpUtility.UrlEncode(this._comment);

                _ = buff.AppendFormat("; Comment={0}", comment);
            }

            if (this._commentUri != null) {
                var url = this._commentUri.OriginalString;

                _ = buff.AppendFormat(
                  "; CommentURL={0}", !url.IsToken() ? url.Quote() : url
                );
            }

            if (this._discard) {
                _ = buff.Append("; Discard");
            }

            if (this._secure) {
                _ = buff.Append("; Secure");
            }

            return buff.ToString();
        }

        private static bool tryCreatePorts(string value, out int[] result)
        {
            result = null;

            var arr = value.Trim('"').Split(',');
            var len = arr.Length;
            var res = new int[len];

            for (var i = 0; i < len; i++) {
                var s = arr[i].Trim();

                if (s.Length == 0) {
                    res[i] = int.MinValue;

                    continue;
                }

                if (!int.TryParse(s, out res[i])) {
                    return false;
                }
            }

            result = res;

            return true;
        }

        #endregion

        #region Internal Methods

        internal bool EqualsWithoutValue(Cookie cookie)
        {
            var caseSensitive = StringComparison.InvariantCulture;
            var caseInsensitive = StringComparison.InvariantCultureIgnoreCase;

            return this._name.Equals(cookie._name, caseInsensitive)
                   && this._path.Equals(cookie._path, caseSensitive)
                   && this._domain.Equals(cookie._domain, caseInsensitive)
                   && this._version == cookie._version;
        }

        internal bool EqualsWithoutValueAndVersion(Cookie cookie)
        {
            var caseSensitive = StringComparison.InvariantCulture;
            var caseInsensitive = StringComparison.InvariantCultureIgnoreCase;

            return this._name.Equals(cookie._name, caseInsensitive)
                   && this._path.Equals(cookie._path, caseSensitive)
                   && this._domain.Equals(cookie._domain, caseInsensitive);
        }

        internal string ToRequestString(Uri uri)
        {
            if (this._name.Length == 0) {
                return string.Empty;
            }

            if (this._version == 0) {
                return string.Format("{0}={1}", this._name, this._value);
            }

            var buff = new StringBuilder(64);

            _ = buff.AppendFormat("$Version={0}; {1}={2}", this._version, this._name, this._value);

            _ = !this._path.IsNullOrEmpty()
                ? buff.AppendFormat("; $Path={0}", this._path)
                : uri != null ? buff.AppendFormat("; $Path={0}", uri.GetAbsolutePath()) : buff.Append("; $Path=/");

            if (!this._domain.IsNullOrEmpty()) {
                if (uri == null || uri.Host != this._domain) {
                    _ = buff.AppendFormat("; $Domain={0}", this._domain);
                }
            }

            if (this._port != null) {
                _ = this._port != "\"\"" ? buff.AppendFormat("; $Port={0}", this._port) : buff.Append("; $Port");
            }

            return buff.ToString();
        }

        internal string ToResponseString()
        {
            return this._name.Length == 0 ? string.Empty : this._version == 0 ? this.toResponseStringVersion0() : this.toResponseStringVersion1();
        }

        internal static bool TryCreate(
          string name, string value, out Cookie result
        )
        {
            result = null;

            try {
                result = new Cookie(name, value);
            }
            catch {
                return false;
            }

            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Determines whether the current cookie instance is equal to
        /// the specified <see cref="object"/> instance.
        /// </summary>
        /// <param name="comparand">
        ///   <para>
        ///   An <see cref="object"/> instance to compare with
        ///   the current cookie instance.
        ///   </para>
        ///   <para>
        ///   An reference to a <see cref="Cookie"/> instance.
        ///   </para>
        /// </param>
        /// <returns>
        /// <c>true</c> if the current cookie instance is equal to
        /// <paramref name="comparand"/>; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object comparand)
        {
            if (comparand is not Cookie cookie) {
                return false;
            }

            var caseSensitive = StringComparison.InvariantCulture;
            var caseInsensitive = StringComparison.InvariantCultureIgnoreCase;

            return this._name.Equals(cookie._name, caseInsensitive)
                   && this._value.Equals(cookie._value, caseSensitive)
                   && this._path.Equals(cookie._path, caseSensitive)
                   && this._domain.Equals(cookie._domain, caseInsensitive)
                   && this._version == cookie._version;
        }

        /// <summary>
        /// Gets a hash code for the current cookie instance.
        /// </summary>
        /// <returns>
        /// An <see cref="int"/> that represents the hash code.
        /// </returns>
        public override int GetHashCode()
        {
            var i = StringComparer.InvariantCultureIgnoreCase.GetHashCode(this._name);
            var j = this._value.GetHashCode();
            var k = this._path.GetHashCode();
            var l = StringComparer.InvariantCultureIgnoreCase.GetHashCode(this._domain);
            var m = this._version;

            return hash(i, j, k, l, m);
        }

        /// <summary>
        /// Returns a string that represents the current cookie instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that is suitable for the Cookie request header.
        /// </returns>
        public override string ToString()
        {
            return this.ToRequestString(null);
        }

        #endregion
    }
}
