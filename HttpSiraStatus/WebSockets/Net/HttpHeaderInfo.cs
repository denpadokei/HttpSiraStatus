#region License
/*
 * HttpHeaderInfo.cs
 *
 * The MIT License
 *
 * Copyright (c) 2013-2020 sta.blockhead
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

namespace HttpSiraStatus.WebSockets.Net
{
    internal class HttpHeaderInfo
    {
        #region Private Fields

        #endregion

        #region Internal Constructors

        internal HttpHeaderInfo(string headerName, HttpHeaderType headerType)
        {
            this.HeaderName = headerName;
            this.HeaderType = headerType;
        }

        #endregion

        #region Internal Properties

        internal bool IsMultiValueInRequest
        {
            get
            {
                var headerType = this.HeaderType & HttpHeaderType.MultiValueInRequest;

                return headerType == HttpHeaderType.MultiValueInRequest;
            }
        }

        internal bool IsMultiValueInResponse
        {
            get
            {
                var headerType = this.HeaderType & HttpHeaderType.MultiValueInResponse;

                return headerType == HttpHeaderType.MultiValueInResponse;
            }
        }

        #endregion

        #region Public Properties

        public string HeaderName { get; }

        public HttpHeaderType HeaderType { get; }

        public bool IsRequest
        {
            get
            {
                var headerType = this.HeaderType & HttpHeaderType.Request;

                return headerType == HttpHeaderType.Request;
            }
        }

        public bool IsResponse
        {
            get
            {
                var headerType = this.HeaderType & HttpHeaderType.Response;

                return headerType == HttpHeaderType.Response;
            }
        }

        #endregion

        #region Public Methods

        public bool IsMultiValue(bool response)
        {
            var headerType = this.HeaderType & HttpHeaderType.MultiValue;

            return headerType != HttpHeaderType.MultiValue
                ? response ? this.IsMultiValueInResponse : this.IsMultiValueInRequest
                : response ? this.IsResponse : this.IsRequest;
        }

        public bool IsRestricted(bool response)
        {
            var headerType = this.HeaderType & HttpHeaderType.Restricted;

            return headerType == HttpHeaderType.Restricted && (response ? this.IsResponse : this.IsRequest);
        }

        #endregion
    }
}
