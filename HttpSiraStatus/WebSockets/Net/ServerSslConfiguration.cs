#region License
/*
 * ServerSslConfiguration.cs
 *
 * The MIT License
 *
 * Copyright (c) 2014 liryna
 * Copyright (c) 2014-2023 sta.blockhead
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
 * - Liryna <liryna.stark@gmail.com>
 */
#endregion

using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace HttpSiraStatus.WebSockets.Net
{
    /// <summary>
    /// Stores the parameters for the <see cref="SslStream"/> used by servers.
    /// </summary>
    public class ServerSslConfiguration
    {
        #region Private Fields

        private RemoteCertificateValidationCallback _clientCertValidationCallback;

        #endregion

        #region Public Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerSslConfiguration"/>
        /// class.
        /// </summary>
        public ServerSslConfiguration()
        {
            this.EnabledSslProtocols = SslProtocols.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerSslConfiguration"/>
        /// class copying from the specified configuration.
        /// </summary>
        /// <param name="configuration">
        /// A <see cref="ServerSslConfiguration"/> from which to copy.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="configuration"/> is <see langword="null"/>.
        /// </exception>
        public ServerSslConfiguration(ServerSslConfiguration configuration)
        {
            if (configuration == null) {
                throw new ArgumentNullException("configuration");
            }

            this.CheckCertificateRevocation = configuration.CheckCertificateRevocation;
            this.ClientCertificateRequired = configuration.ClientCertificateRequired;
            this._clientCertValidationCallback = configuration._clientCertValidationCallback;
            this.EnabledSslProtocols = configuration.EnabledSslProtocols;
            this.ServerCertificate = configuration.ServerCertificate;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether the certificate revocation
        /// list is checked during authentication.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the certificate revocation list is checked during
        ///   authentication; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool CheckCertificateRevocation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the client is asked for
        /// a certificate for authentication.
        /// </summary>
        /// <value>
        ///   <para>
        ///   <c>true</c> if the client is asked for a certificate for
        ///   authentication; otherwise, <c>false</c>.
        ///   </para>
        ///   <para>
        ///   The default value is <c>false</c>.
        ///   </para>
        /// </value>
        public bool ClientCertificateRequired { get; set; }

        /// <summary>
        /// Gets or sets the callback used to validate the certificate supplied by
        /// the client.
        /// </summary>
        /// <remarks>
        /// The certificate is valid if the callback returns <c>true</c>.
        /// </remarks>
        /// <value>
        ///   <para>
        ///   A <see cref="RemoteCertificateValidationCallback"/> delegate.
        ///   </para>
        ///   <para>
        ///   The delegate invokes the method called when the server validates
        ///   the certificate.
        ///   </para>
        ///   <para>
        ///   The default value is a delegate that invokes a method that only
        ///   returns <c>true</c>.
        ///   </para>
        /// </value>
        public RemoteCertificateValidationCallback ClientCertificateValidationCallback
        {
            get
            {
                this._clientCertValidationCallback ??= defaultValidateClientCertificate;

                return this._clientCertValidationCallback;
            }

            set => this._clientCertValidationCallback = value;
        }

        /// <summary>
        /// Gets or sets the enabled versions of the SSL/TLS protocols.
        /// </summary>
        /// <value>
        ///   <para>
        ///   Any of the <see cref="SslProtocols"/> enum values.
        ///   </para>
        ///   <para>
        ///   It represents the enabled versions of the SSL/TLS protocols.
        ///   </para>
        ///   <para>
        ///   The default value is <see cref="SslProtocols.None"/>.
        ///   </para>
        /// </value>
        public SslProtocols EnabledSslProtocols { get; set; }

        /// <summary>
        /// Gets or sets the certificate used to authenticate the server.
        /// </summary>
        /// <value>
        ///   <para>
        ///   A <see cref="X509Certificate2"/> that represents an X.509 certificate.
        ///   </para>
        ///   <para>
        ///   <see langword="null"/> if not present.
        ///   </para>
        ///   <para>
        ///   The default value is <see langword="null"/>.
        ///   </para>
        /// </value>
        public X509Certificate2 ServerCertificate { get; set; }

        #endregion

        #region Private Methods

        private static bool defaultValidateClientCertificate(
          object sender,
          X509Certificate certificate,
          X509Chain chain,
          SslPolicyErrors sslPolicyErrors
        )
        {
            return true;
        }

        #endregion
    }
}
