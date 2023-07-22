#region License
/*
 * HttpListenerAsyncResult.cs
 *
 * This code is derived from ListenerAsyncResult.cs (System.Net) of Mono
 * (http://www.mono-project.com).
 *
 * The MIT License
 *
 * Copyright (c) 2005 Ximian, Inc. (http://www.ximian.com)
 * Copyright (c) 2012-2021 sta.blockhead
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
 * - Gonzalo Paniagua Javier <gonzalo@ximian.com>
 */
#endregion

#region Contributors
/*
 * Contributors:
 * - Nicholas Devenish
 */
#endregion

using System;
using System.Threading;

namespace HttpSiraStatus.WebSockets.Net
{
    internal class HttpListenerAsyncResult : IAsyncResult
    {
        #region Private Fields

        private readonly AsyncCallback _callback;
        private bool _completed;
        private HttpListenerContext _context;
        private Exception _exception;
        private ManualResetEvent _waitHandle;

        #endregion

        #region Internal Constructors

        internal HttpListenerAsyncResult(AsyncCallback callback, object state)
        {
            this._callback = callback;
            this.AsyncState = state;

            this.SyncRoot = new object();
        }

        #endregion

        #region Internal Properties

        internal HttpListenerContext Context => this._exception != null ? throw this._exception : this._context;

        internal bool EndCalled { get; set; }

        internal object SyncRoot { get; }

        #endregion

        #region Public Properties

        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle
        {
            get
            {
                lock (this.SyncRoot) {
                    this._waitHandle ??= new ManualResetEvent(this._completed);

                    return this._waitHandle;
                }
            }
        }

        public bool CompletedSynchronously { get; private set; }

        public bool IsCompleted
        {
            get
            {
                lock (this.SyncRoot) {
                    return this._completed;
                }
            }
        }

        #endregion

        #region Private Methods

        private void complete()
        {
            lock (this.SyncRoot) {
                this._completed = true;

                _ = (this._waitHandle?.Set());
            }

            if (this._callback == null) {
                return;
            }

            _ = ThreadPool.QueueUserWorkItem(
              state =>
              {
                  try {
                      this._callback(this);
                  }
                  catch {
                  }
              },
              null
            );
        }

        #endregion

        #region Internal Methods

        internal void Complete(Exception exception)
        {
            this._exception = exception;

            this.complete();
        }

        internal void Complete(
          HttpListenerContext context, bool completedSynchronously
        )
        {
            this._context = context;
            this.CompletedSynchronously = completedSynchronously;

            this.complete();
        }

        #endregion
    }
}
