#region License
/*
 * LogData.cs
 *
 * The MIT License
 *
 * Copyright (c) 2013-2022 sta.blockhead
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
using System.Diagnostics;
using System.Text;

namespace HttpSiraStatus.WebSockets
{
    /// <summary>
    /// Represents a log data used by the <see cref="Logger"/> class.
    /// </summary>
    public class LogData
    {
        #region Private Fields

        #endregion

        #region Internal Constructors

        internal LogData(LogLevel level, StackFrame caller, string message)
        {
            this.Level = level;
            this.Caller = caller;
            this.Message = message ?? string.Empty;

            this.Date = DateTime.Now;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the information of the logging method caller.
        /// </summary>
        /// <value>
        /// A <see cref="StackFrame"/> that provides the information of
        /// the logging method caller.
        /// </value>
        public StackFrame Caller { get; }

        /// <summary>
        /// Gets the date and time when the log data was created.
        /// </summary>
        /// <value>
        /// A <see cref="DateTime"/> that represents the date and time when
        /// the log data was created.
        /// </value>
        public DateTime Date { get; }

        /// <summary>
        /// Gets the logging level of the log data.
        /// </summary>
        /// <value>
        /// One of the <see cref="LogLevel"/> enum values that represents
        /// the logging level of the log data.
        /// </value>
        public LogLevel Level { get; }

        /// <summary>
        /// Gets the message of the log data.
        /// </summary>
        /// <value>
        /// A <see cref="string"/> that represents the message of the log data.
        /// </value>
        public string Message { get; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns a string that represents the current instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the current instance.
        /// </returns>
        public override string ToString()
        {
            var date = string.Format("[{0}]", this.Date);
            var level = string.Format("{0,-5}", this.Level.ToString().ToUpper());

            var method = this.Caller.GetMethod();
            var type = method.DeclaringType;
#if DEBUG
      var num = _caller.GetFileLineNumber ();
      var caller = String.Format ("{0}.{1}:{2}", type.Name, method.Name, num);
#else
            var caller = string.Format("{0}.{1}", type.Name, method.Name);
#endif
            var msgs = this.Message.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

            if (msgs.Length <= 1) {
                return string.Format("{0} {1} {2} {3}", date, level, caller, this.Message);
            }

            var buff = new StringBuilder(64);

            buff.AppendFormat("{0} {1} {2}\n\n", date, level, caller);

            for (var i = 0; i < msgs.Length; i++) {
                buff.AppendFormat("  {0}\n", msgs[i]);
            }

            return buff.ToString();
        }

        #endregion
    }
}
