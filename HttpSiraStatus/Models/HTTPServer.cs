using HttpSiraStatus.Configuration;
using HttpSiraStatus.Interfaces;
using System;
using System.Text;
using WebSocketSharp.Server;
using Zenject;

namespace HttpSiraStatus.Models
{
    public class HTTPServer : IInitializable, IDisposable
    {
        private HttpServer _server;
        private readonly IStatusManager _statusManager;
        private bool _disposedValue;
        private readonly PluginConfig _config;

        [Inject]
        public HTTPServer(IStatusManager statusManager, PluginConfig config)
        {
            this._statusManager = statusManager;
            this._config = config;
        }

        public void OnHTTPGet(HttpRequestEventArgs e)
        {
            var req = e.Request;
            var res = e.Response;

            if (req.RawUrl == "/status.json") {
                res.StatusCode = 200;
                res.ContentType = "application/json";
                res.ContentEncoding = Encoding.UTF8;

                var stringifiedStatus = Encoding.UTF8.GetBytes(this._statusManager.StatusJSON.ToString());

                res.ContentLength64 = stringifiedStatus.Length;
                res.Close(stringifiedStatus, false);

                return;
            }

            res.StatusCode = 404;
            res.Close();
        }
        public void Initialize()
        {
            this._server = new HttpServer(this._config.Port);

            this._server.OnGet += (sender, e) =>
            {
                this.OnHTTPGet(e);
            };

            this._server.AddWebSocketService<StatusBroadcastBehavior>("/socket", initializer => initializer.SetStatusManager(this._statusManager));

            Plugin.Logger.Info($"Starting HTTP server on port {this._config.Port}");
            this._server.Start();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this._disposedValue) {
                if (disposing) {
                    Plugin.Logger.Info("Stopping HTTP server");
                    this._server.Stop();
                }
                this._disposedValue = true;
            }
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
