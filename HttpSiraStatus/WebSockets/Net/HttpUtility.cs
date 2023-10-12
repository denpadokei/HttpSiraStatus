#region License
/*
 * HttpUtility.cs
 *
 * This code is derived from HttpUtility.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005-2009 Novell, Inc. (http://www.novell.com)
 * Copyright (c) 2012-2019 sta.blockhead
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
 * - Patrik Torstensson <Patrik.Torstensson@labs2.com>
 * - Wictor Wilén (decode/encode functions) <wictor@ibizkit.se>
 * - Tim Coleman <tim@timcoleman.com>
 * - Gonzalo Paniagua Javier <gonzalo@ximian.com>
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Text;

namespace HttpSiraStatus.WebSockets.Net
{
    internal static class HttpUtility
    {
        #region Private Fields

        private static Dictionary<string, char> _entities;
        private static readonly char[] _hexChars;
        private static readonly object _sync;

        #endregion

        #region Static Constructor

        static HttpUtility()
        {
            _hexChars = "0123456789ABCDEF".ToCharArray();
            _sync = new object();
        }

        #endregion

        #region Private Methods

        private static Dictionary<string, char> getEntities()
        {
            lock (_sync) {
                if (_entities == null) {
                    initEntities();
                }

                return _entities;
            }
        }

        private static int getNumber(char c)
        {
            return c >= '0' && c <= '9'
                   ? c - '0'
                   : c >= 'A' && c <= 'F'
                     ? c - 'A' + 10
                     : c >= 'a' && c <= 'f'
                       ? c - 'a' + 10
                       : -1;
        }

        private static int getNumber(byte[] bytes, int offset, int count)
        {
            var ret = 0;

            var end = offset + count - 1;
            for (var i = offset; i <= end; i++) {
                var num = getNumber((char)bytes[i]);
                if (num == -1) {
                    return -1;
                }

                ret = (ret << 4) + num;
            }

            return ret;
        }

        private static int getNumber(string s, int offset, int count)
        {
            var ret = 0;

            var end = offset + count - 1;
            for (var i = offset; i <= end; i++) {
                var num = getNumber(s[i]);
                if (num == -1) {
                    return -1;
                }

                ret = (ret << 4) + num;
            }

            return ret;
        }

        private static string htmlDecode(string s)
        {
            var buff = new StringBuilder();

            // 0: None
            // 1: Right after '&'
            // 2: Between '&' and ';' but no NCR
            // 3: '#' found after '&' and getting numbers
            // 4: 'x' found after '#' and getting numbers
            var state = 0;

            var reference = new StringBuilder();
            var num = 0;

            foreach (var c in s) {
                if (state == 0) {
                    if (c == '&') {
                        _ = reference.Append('&');
                        state = 1;

                        continue;
                    }

                    _ = buff.Append(c);
                    continue;
                }

                if (c == '&') {
                    _ = buff.Append(reference.ToString());

                    reference.Length = 0;
                    _ = reference.Append('&');
                    state = 1;

                    continue;
                }

                _ = reference.Append(c);

                if (state == 1) {
                    if (c == ';') {
                        _ = buff.Append(reference.ToString());

                        reference.Length = 0;
                        state = 0;

                        continue;
                    }

                    num = 0;
                    state = c == '#' ? 3 : 2;

                    continue;
                }

                if (state == 2) {
                    if (c == ';') {
                        var entity = reference.ToString();
                        var name = entity.Substring(1, entity.Length - 2);

                        var entities = getEntities();
                        _ = entities.ContainsKey(name) ? buff.Append(entities[name]) : buff.Append(entity);

                        reference.Length = 0;
                        state = 0;

                        continue;
                    }

                    continue;
                }

                if (state == 3) {
                    if (c == ';') {
                        _ = reference.Length > 3 && num < 65536 ? buff.Append((char)num) : buff.Append(reference.ToString());

                        reference.Length = 0;
                        state = 0;

                        continue;
                    }

                    if (c == 'x') {
                        state = reference.Length == 3 ? 4 : 2;
                        continue;
                    }

                    if (!isNumeric(c)) {
                        state = 2;
                        continue;
                    }

                    num = (num * 10) + (c - '0');
                    continue;
                }

                if (state == 4) {
                    if (c == ';') {
                        _ = reference.Length > 4 && num < 65536 ? buff.Append((char)num) : buff.Append(reference.ToString());

                        reference.Length = 0;
                        state = 0;

                        continue;
                    }

                    var n = getNumber(c);
                    if (n == -1) {
                        state = 2;
                        continue;
                    }

                    num = (num << 4) + n;
                }
            }

            if (reference.Length > 0) {
                _ = buff.Append(reference.ToString());
            }

            return buff.ToString();
        }

        /// <summary>
        /// Converts the specified string to an HTML-encoded string.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///   This method starts encoding with a NCR from the character code 160
        ///   but does not stop at the character code 255.
        ///   </para>
        ///   <para>
        ///   One reason is the unicode characters &#65308; and &#65310; that
        ///   look like &lt; and &gt;.
        ///   </para>
        /// </remarks>
        /// <returns>
        /// A <see cref="string"/> that represents an encoded string.
        /// </returns>
        /// <param name="s">
        /// A <see cref="string"/> to encode.
        /// </param>
        /// <param name="minimal">
        /// A <see cref="bool"/>: <c>true</c> if encodes without a NCR;
        /// otherwise, <c>false</c>.
        /// </param>
        private static string htmlEncode(string s, bool minimal)
        {
            var buff = new StringBuilder();

            foreach (var c in s) {
                _ = buff.Append(
                  c == '"'
                  ? "&quot;"
                  : c == '&'
                    ? "&amp;"
                    : c == '<'
                      ? "&lt;"
                      : c == '>'
                        ? "&gt;"
                        : !minimal && c > 159
                          ? string.Format("&#{0};", (int)c)
                          : c.ToString()
                );
            }

            return buff.ToString();
        }

        /// <summary>
        /// Initializes the _entities field.
        /// </summary>
        /// <remarks>
        /// This method builds a dictionary of HTML character entity references.
        /// This dictionary comes from the HTML 4.01 W3C recommendation.
        /// </remarks>
        private static void initEntities()
        {
            _entities = new Dictionary<string, char>
            {
                { "nbsp", '\u00A0' },
                { "iexcl", '\u00A1' },
                { "cent", '\u00A2' },
                { "pound", '\u00A3' },
                { "curren", '\u00A4' },
                { "yen", '\u00A5' },
                { "brvbar", '\u00A6' },
                { "sect", '\u00A7' },
                { "uml", '\u00A8' },
                { "copy", '\u00A9' },
                { "ordf", '\u00AA' },
                { "laquo", '\u00AB' },
                { "not", '\u00AC' },
                { "shy", '\u00AD' },
                { "reg", '\u00AE' },
                { "macr", '\u00AF' },
                { "deg", '\u00B0' },
                { "plusmn", '\u00B1' },
                { "sup2", '\u00B2' },
                { "sup3", '\u00B3' },
                { "acute", '\u00B4' },
                { "micro", '\u00B5' },
                { "para", '\u00B6' },
                { "middot", '\u00B7' },
                { "cedil", '\u00B8' },
                { "sup1", '\u00B9' },
                { "ordm", '\u00BA' },
                { "raquo", '\u00BB' },
                { "frac14", '\u00BC' },
                { "frac12", '\u00BD' },
                { "frac34", '\u00BE' },
                { "iquest", '\u00BF' },
                { "Agrave", '\u00C0' },
                { "Aacute", '\u00C1' },
                { "Acirc", '\u00C2' },
                { "Atilde", '\u00C3' },
                { "Auml", '\u00C4' },
                { "Aring", '\u00C5' },
                { "AElig", '\u00C6' },
                { "Ccedil", '\u00C7' },
                { "Egrave", '\u00C8' },
                { "Eacute", '\u00C9' },
                { "Ecirc", '\u00CA' },
                { "Euml", '\u00CB' },
                { "Igrave", '\u00CC' },
                { "Iacute", '\u00CD' },
                { "Icirc", '\u00CE' },
                { "Iuml", '\u00CF' },
                { "ETH", '\u00D0' },
                { "Ntilde", '\u00D1' },
                { "Ograve", '\u00D2' },
                { "Oacute", '\u00D3' },
                { "Ocirc", '\u00D4' },
                { "Otilde", '\u00D5' },
                { "Ouml", '\u00D6' },
                { "times", '\u00D7' },
                { "Oslash", '\u00D8' },
                { "Ugrave", '\u00D9' },
                { "Uacute", '\u00DA' },
                { "Ucirc", '\u00DB' },
                { "Uuml", '\u00DC' },
                { "Yacute", '\u00DD' },
                { "THORN", '\u00DE' },
                { "szlig", '\u00DF' },
                { "agrave", '\u00E0' },
                { "aacute", '\u00E1' },
                { "acirc", '\u00E2' },
                { "atilde", '\u00E3' },
                { "auml", '\u00E4' },
                { "aring", '\u00E5' },
                { "aelig", '\u00E6' },
                { "ccedil", '\u00E7' },
                { "egrave", '\u00E8' },
                { "eacute", '\u00E9' },
                { "ecirc", '\u00EA' },
                { "euml", '\u00EB' },
                { "igrave", '\u00EC' },
                { "iacute", '\u00ED' },
                { "icirc", '\u00EE' },
                { "iuml", '\u00EF' },
                { "eth", '\u00F0' },
                { "ntilde", '\u00F1' },
                { "ograve", '\u00F2' },
                { "oacute", '\u00F3' },
                { "ocirc", '\u00F4' },
                { "otilde", '\u00F5' },
                { "ouml", '\u00F6' },
                { "divide", '\u00F7' },
                { "oslash", '\u00F8' },
                { "ugrave", '\u00F9' },
                { "uacute", '\u00FA' },
                { "ucirc", '\u00FB' },
                { "uuml", '\u00FC' },
                { "yacute", '\u00FD' },
                { "thorn", '\u00FE' },
                { "yuml", '\u00FF' },
                { "fnof", '\u0192' },
                { "Alpha", '\u0391' },
                { "Beta", '\u0392' },
                { "Gamma", '\u0393' },
                { "Delta", '\u0394' },
                { "Epsilon", '\u0395' },
                { "Zeta", '\u0396' },
                { "Eta", '\u0397' },
                { "Theta", '\u0398' },
                { "Iota", '\u0399' },
                { "Kappa", '\u039A' },
                { "Lambda", '\u039B' },
                { "Mu", '\u039C' },
                { "Nu", '\u039D' },
                { "Xi", '\u039E' },
                { "Omicron", '\u039F' },
                { "Pi", '\u03A0' },
                { "Rho", '\u03A1' },
                { "Sigma", '\u03A3' },
                { "Tau", '\u03A4' },
                { "Upsilon", '\u03A5' },
                { "Phi", '\u03A6' },
                { "Chi", '\u03A7' },
                { "Psi", '\u03A8' },
                { "Omega", '\u03A9' },
                { "alpha", '\u03B1' },
                { "beta", '\u03B2' },
                { "gamma", '\u03B3' },
                { "delta", '\u03B4' },
                { "epsilon", '\u03B5' },
                { "zeta", '\u03B6' },
                { "eta", '\u03B7' },
                { "theta", '\u03B8' },
                { "iota", '\u03B9' },
                { "kappa", '\u03BA' },
                { "lambda", '\u03BB' },
                { "mu", '\u03BC' },
                { "nu", '\u03BD' },
                { "xi", '\u03BE' },
                { "omicron", '\u03BF' },
                { "pi", '\u03C0' },
                { "rho", '\u03C1' },
                { "sigmaf", '\u03C2' },
                { "sigma", '\u03C3' },
                { "tau", '\u03C4' },
                { "upsilon", '\u03C5' },
                { "phi", '\u03C6' },
                { "chi", '\u03C7' },
                { "psi", '\u03C8' },
                { "omega", '\u03C9' },
                { "thetasym", '\u03D1' },
                { "upsih", '\u03D2' },
                { "piv", '\u03D6' },
                { "bull", '\u2022' },
                { "hellip", '\u2026' },
                { "prime", '\u2032' },
                { "Prime", '\u2033' },
                { "oline", '\u203E' },
                { "frasl", '\u2044' },
                { "weierp", '\u2118' },
                { "image", '\u2111' },
                { "real", '\u211C' },
                { "trade", '\u2122' },
                { "alefsym", '\u2135' },
                { "larr", '\u2190' },
                { "uarr", '\u2191' },
                { "rarr", '\u2192' },
                { "darr", '\u2193' },
                { "harr", '\u2194' },
                { "crarr", '\u21B5' },
                { "lArr", '\u21D0' },
                { "uArr", '\u21D1' },
                { "rArr", '\u21D2' },
                { "dArr", '\u21D3' },
                { "hArr", '\u21D4' },
                { "forall", '\u2200' },
                { "part", '\u2202' },
                { "exist", '\u2203' },
                { "empty", '\u2205' },
                { "nabla", '\u2207' },
                { "isin", '\u2208' },
                { "notin", '\u2209' },
                { "ni", '\u220B' },
                { "prod", '\u220F' },
                { "sum", '\u2211' },
                { "minus", '\u2212' },
                { "lowast", '\u2217' },
                { "radic", '\u221A' },
                { "prop", '\u221D' },
                { "infin", '\u221E' },
                { "ang", '\u2220' },
                { "and", '\u2227' },
                { "or", '\u2228' },
                { "cap", '\u2229' },
                { "cup", '\u222A' },
                { "int", '\u222B' },
                { "there4", '\u2234' },
                { "sim", '\u223C' },
                { "cong", '\u2245' },
                { "asymp", '\u2248' },
                { "ne", '\u2260' },
                { "equiv", '\u2261' },
                { "le", '\u2264' },
                { "ge", '\u2265' },
                { "sub", '\u2282' },
                { "sup", '\u2283' },
                { "nsub", '\u2284' },
                { "sube", '\u2286' },
                { "supe", '\u2287' },
                { "oplus", '\u2295' },
                { "otimes", '\u2297' },
                { "perp", '\u22A5' },
                { "sdot", '\u22C5' },
                { "lceil", '\u2308' },
                { "rceil", '\u2309' },
                { "lfloor", '\u230A' },
                { "rfloor", '\u230B' },
                { "lang", '\u2329' },
                { "rang", '\u232A' },
                { "loz", '\u25CA' },
                { "spades", '\u2660' },
                { "clubs", '\u2663' },
                { "hearts", '\u2665' },
                { "diams", '\u2666' },
                { "quot", '\u0022' },
                { "amp", '\u0026' },
                { "lt", '\u003C' },
                { "gt", '\u003E' },
                { "OElig", '\u0152' },
                { "oelig", '\u0153' },
                { "Scaron", '\u0160' },
                { "scaron", '\u0161' },
                { "Yuml", '\u0178' },
                { "circ", '\u02C6' },
                { "tilde", '\u02DC' },
                { "ensp", '\u2002' },
                { "emsp", '\u2003' },
                { "thinsp", '\u2009' },
                { "zwnj", '\u200C' },
                { "zwj", '\u200D' },
                { "lrm", '\u200E' },
                { "rlm", '\u200F' },
                { "ndash", '\u2013' },
                { "mdash", '\u2014' },
                { "lsquo", '\u2018' },
                { "rsquo", '\u2019' },
                { "sbquo", '\u201A' },
                { "ldquo", '\u201C' },
                { "rdquo", '\u201D' },
                { "bdquo", '\u201E' },
                { "dagger", '\u2020' },
                { "Dagger", '\u2021' },
                { "permil", '\u2030' },
                { "lsaquo", '\u2039' },
                { "rsaquo", '\u203A' },
                { "euro", '\u20AC' }
            };
        }

        private static bool isAlphabet(char c)
        {
            return (c >= 'A' && c <= 'Z')
                   || (c >= 'a' && c <= 'z');
        }

        private static bool isNumeric(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static bool isUnreserved(char c)
        {
            return c == '*'
                   || c == '-'
                   || c == '.'
                   || c == '_';
        }

        private static bool isUnreservedInRfc2396(char c)
        {
            return c == '!'
                   || c == '\''
                   || c == '('
                   || c == ')'
                   || c == '*'
                   || c == '-'
                   || c == '.'
                   || c == '_'
                   || c == '~';
        }

        private static bool isUnreservedInRfc3986(char c)
        {
            return c == '-'
                   || c == '.'
                   || c == '_'
                   || c == '~';
        }

        private static byte[] urlDecodeToBytes(byte[] bytes, int offset, int count)
        {
            using (var buff = new MemoryStream()) {
                var end = offset + count - 1;
                for (var i = offset; i <= end; i++) {
                    var b = bytes[i];

                    var c = (char)b;
                    if (c == '%') {
                        if (i > end - 2) {
                            break;
                        }

                        var num = getNumber(bytes, i + 1, 2);
                        if (num == -1) {
                            break;
                        }

                        buff.WriteByte((byte)num);
                        i += 2;

                        continue;
                    }

                    if (c == '+') {
                        buff.WriteByte((byte)' ');
                        continue;
                    }

                    buff.WriteByte(b);
                }

                buff.Close();
                return buff.ToArray();
            }
        }

        private static void urlEncode(byte b, Stream output)
        {
            if (b > 31 && b < 127) {
                var c = (char)b;
                if (c == ' ') {
                    output.WriteByte((byte)'+');
                    return;
                }

                if (isNumeric(c)) {
                    output.WriteByte(b);
                    return;
                }

                if (isAlphabet(c)) {
                    output.WriteByte(b);
                    return;
                }

                if (isUnreserved(c)) {
                    output.WriteByte(b);
                    return;
                }
            }

            var i = (int)b;
            var bytes = new byte[] {
                    (byte) '%',
                    (byte) _hexChars[i >> 4],
                    (byte) _hexChars[i & 0x0F]
                  };

            output.Write(bytes, 0, 3);
        }

        private static byte[] urlEncodeToBytes(byte[] bytes, int offset, int count)
        {
            using (var buff = new MemoryStream()) {
                var end = offset + count - 1;
                for (var i = offset; i <= end; i++) {
                    urlEncode(bytes[i], buff);
                }

                buff.Close();
                return buff.ToArray();
            }
        }

        #endregion

        #region Internal Methods

        internal static Uri CreateRequestUrl(
          string requestUri, string host, bool websocketRequest, bool secure
        )
        {
            if (requestUri == null || requestUri.Length == 0) {
                return null;
            }

            if (host == null || host.Length == 0) {
                return null;
            }

            string schm = null;
            string path = null;

            if (requestUri.IndexOf('/') == 0) {
                path = requestUri;
            }
            else if (requestUri.MaybeUri()) {

                if (!Uri.TryCreate(requestUri, UriKind.Absolute, out var uri)) {
                    return null;
                }

                schm = uri.Scheme;
                var valid = websocketRequest
                            ? schm == "ws" || schm == "wss"
                            : schm == "http" || schm == "https";

                if (!valid) {
                    return null;
                }

                host = uri.Authority;
                path = uri.PathAndQuery;
            }
            else if (requestUri == "*") {
            }
            else {
                // As the authority form.

                host = requestUri;
            }

            schm ??= websocketRequest
                       ? secure ? "wss" : "ws"
                       : secure ? "https" : "http";

            if (host.IndexOf(':') == -1) {
                host = string.Format("{0}:{1}", host, secure ? 443 : 80);
            }

            var url = string.Format("{0}://{1}{2}", schm, host, path);

            return Uri.TryCreate(url, UriKind.Absolute, out var ret) ? ret : null;
        }

        internal static IPrincipal CreateUser(
          string response,
          AuthenticationSchemes scheme,
          string realm,
          string method,
          Func<IIdentity, NetworkCredential> credentialsFinder
        )
        {
            if (response == null || response.Length == 0) {
                return null;
            }

            if (scheme == AuthenticationSchemes.Digest) {
                if (realm == null || realm.Length == 0) {
                    return null;
                }

                if (method == null || method.Length == 0) {
                    return null;
                }
            }
            else {
                if (scheme != AuthenticationSchemes.Basic) {
                    return null;
                }
            }

            if (credentialsFinder == null) {
                return null;
            }

            var compType = StringComparison.OrdinalIgnoreCase;
            if (response.IndexOf(scheme.ToString(), compType) != 0) {
                return null;
            }

            var res = AuthenticationResponse.Parse(response);
            if (res == null) {
                return null;
            }

            var id = res.ToIdentity();
            if (id == null) {
                return null;
            }

            NetworkCredential cred = null;
            try {
                cred = credentialsFinder(id);
            }
            catch {
            }

            if (cred == null) {
                return null;
            }

            if (scheme == AuthenticationSchemes.Basic) {
                var basicId = (HttpBasicIdentity)id;
                return basicId.Password == cred.Password
                       ? new GenericPrincipal(id, cred.Roles)
                       : null;
            }

            var digestId = (HttpDigestIdentity)id;
            return digestId.IsValid(cred.Password, realm, method, null)
                   ? new GenericPrincipal(id, cred.Roles)
                   : null;
        }

        internal static Encoding GetEncoding(string contentType)
        {
            var name = "charset=";
            var compType = StringComparison.OrdinalIgnoreCase;

            foreach (var elm in contentType.SplitHeaderValue(';')) {
                var part = elm.Trim();

                if (!part.StartsWith(name, compType)) {
                    continue;
                }

                var val = part.GetValue('=', true);

                return val == null || val.Length == 0 ? null : Encoding.GetEncoding(val);
            }

            return null;
        }

        internal static bool TryGetEncoding(
          string contentType, out Encoding result
        )
        {
            result = null;

            try {
                result = GetEncoding(contentType);
            }
            catch {
                return false;
            }

            return result != null;
        }

        #endregion

        #region Public Methods

        public static string HtmlAttributeEncode(string s)
        {
            return s == null ? throw new ArgumentNullException("s") : s.Length > 0 ? htmlEncode(s, true) : s;
        }

        public static void HtmlAttributeEncode(string s, TextWriter output)
        {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            if (output == null) {
                throw new ArgumentNullException("output");
            }

            if (s.Length == 0) {
                return;
            }

            output.Write(htmlEncode(s, true));
        }

        public static string HtmlDecode(string s)
        {
            return s == null ? throw new ArgumentNullException("s") : s.Length > 0 ? htmlDecode(s) : s;
        }

        public static void HtmlDecode(string s, TextWriter output)
        {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            if (output == null) {
                throw new ArgumentNullException("output");
            }

            if (s.Length == 0) {
                return;
            }

            output.Write(htmlDecode(s));
        }

        public static string HtmlEncode(string s)
        {
            return s == null ? throw new ArgumentNullException("s") : s.Length > 0 ? htmlEncode(s, false) : s;
        }

        public static void HtmlEncode(string s, TextWriter output)
        {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            if (output == null) {
                throw new ArgumentNullException("output");
            }

            if (s.Length == 0) {
                return;
            }

            output.Write(htmlEncode(s, false));
        }

        public static string UrlDecode(string s)
        {
            return UrlDecode(s, Encoding.UTF8);
        }

        public static string UrlDecode(byte[] bytes, Encoding encoding)
        {
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }

            var len = bytes.Length;
            return len > 0
                   ? (encoding ?? Encoding.UTF8).GetString(
                       urlDecodeToBytes(bytes, 0, len)
                     )
                   : string.Empty;
        }

        public static string UrlDecode(string s, Encoding encoding)
        {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            if (s.Length == 0) {
                return s;
            }

            var bytes = Encoding.ASCII.GetBytes(s);
            return (encoding ?? Encoding.UTF8).GetString(
                     urlDecodeToBytes(bytes, 0, bytes.Length)
                   );
        }

        public static string UrlDecode(
          byte[] bytes, int offset, int count, Encoding encoding
        )
        {
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }

            var len = bytes.Length;
            return len == 0
                ? offset != 0
                    ? throw new ArgumentOutOfRangeException("offset")
                    : count != 0 ? throw new ArgumentOutOfRangeException("count") : string.Empty
                : offset < 0 || offset >= len
                ? throw new ArgumentOutOfRangeException("offset")
                : count < 0 || count > len - offset
                ? throw new ArgumentOutOfRangeException("count")
                : count > 0
                   ? (encoding ?? Encoding.UTF8).GetString(
                       urlDecodeToBytes(bytes, offset, count)
                     )
                   : string.Empty;
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes)
        {
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }

            var len = bytes.Length;
            return len > 0
                   ? urlDecodeToBytes(bytes, 0, len)
                   : bytes;
        }

        public static byte[] UrlDecodeToBytes(string s)
        {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            if (s.Length == 0) {
                return new byte[0];
            }

            var bytes = Encoding.ASCII.GetBytes(s);
            return urlDecodeToBytes(bytes, 0, bytes.Length);
        }

        public static byte[] UrlDecodeToBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }

            var len = bytes.Length;
            return len == 0
                ? offset != 0
                    ? throw new ArgumentOutOfRangeException("offset")
                    : count != 0 ? throw new ArgumentOutOfRangeException("count") : bytes
                : offset < 0 || offset >= len
                ? throw new ArgumentOutOfRangeException("offset")
                : count < 0 || count > len - offset
                ? throw new ArgumentOutOfRangeException("count")
                : count > 0
                   ? urlDecodeToBytes(bytes, offset, count)
                   : new byte[0];
        }

        public static string UrlEncode(byte[] bytes)
        {
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }

            var len = bytes.Length;
            return len > 0
                   ? Encoding.ASCII.GetString(urlEncodeToBytes(bytes, 0, len))
                   : string.Empty;
        }

        public static string UrlEncode(string s)
        {
            return UrlEncode(s, Encoding.UTF8);
        }

        public static string UrlEncode(string s, Encoding encoding)
        {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            var len = s.Length;
            if (len == 0) {
                return s;
            }

            encoding ??= Encoding.UTF8;

            var bytes = new byte[encoding.GetMaxByteCount(len)];
            var realLen = encoding.GetBytes(s, 0, len, bytes, 0);

            return Encoding.ASCII.GetString(urlEncodeToBytes(bytes, 0, realLen));
        }

        public static string UrlEncode(byte[] bytes, int offset, int count)
        {
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }

            var len = bytes.Length;
            return len == 0
                ? offset != 0
                    ? throw new ArgumentOutOfRangeException("offset")
                    : count != 0 ? throw new ArgumentOutOfRangeException("count") : string.Empty
                : offset < 0 || offset >= len
                ? throw new ArgumentOutOfRangeException("offset")
                : count < 0 || count > len - offset
                ? throw new ArgumentOutOfRangeException("count")
                : count > 0
                   ? Encoding.ASCII.GetString(
                       urlEncodeToBytes(bytes, offset, count)
                     )
                   : string.Empty;
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes)
        {
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }

            var len = bytes.Length;
            return len > 0 ? urlEncodeToBytes(bytes, 0, len) : bytes;
        }

        public static byte[] UrlEncodeToBytes(string s)
        {
            return UrlEncodeToBytes(s, Encoding.UTF8);
        }

        public static byte[] UrlEncodeToBytes(string s, Encoding encoding)
        {
            if (s == null) {
                throw new ArgumentNullException("s");
            }

            if (s.Length == 0) {
                return new byte[0];
            }

            var bytes = (encoding ?? Encoding.UTF8).GetBytes(s);
            return urlEncodeToBytes(bytes, 0, bytes.Length);
        }

        public static byte[] UrlEncodeToBytes(byte[] bytes, int offset, int count)
        {
            if (bytes == null) {
                throw new ArgumentNullException("bytes");
            }

            var len = bytes.Length;
            return len == 0
                ? offset != 0
                    ? throw new ArgumentOutOfRangeException("offset")
                    : count != 0 ? throw new ArgumentOutOfRangeException("count") : bytes
                : offset < 0 || offset >= len
                ? throw new ArgumentOutOfRangeException("offset")
                : count < 0 || count > len - offset
                ? throw new ArgumentOutOfRangeException("count")
                : count > 0 ? urlEncodeToBytes(bytes, offset, count) : new byte[0];
        }

        #endregion
    }
}
