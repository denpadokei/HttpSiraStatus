using HttpSiraStatus.Interfaces;
using HttpSiraStatus.Util;
using HttpSiraStatus.WebSockets;
using HttpSiraStatus.WebSockets.Server;

namespace HttpSiraStatus.Models
{
    public class StatusBroadcastBehavior : WebSocketBehavior
    {
        private IStatusManager _statusManager;

        public void SetStatusManager(IStatusManager statusManager)
        {
            this._statusManager = statusManager;
        }

        private void OnSendEvent(object sender, SendEventArgs e)
        {
            if (this.ReadyState != WebSocketState.Open) {
                return;
            }

            try {
                this.Send(e.Json);
            }
            catch (System.Exception ex) {
                Plugin.Logger.Debug(ex.Message);
            }
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Plugin.Logger.Debug("OnOpen call.");
            this._statusManager.SendEvent += this.OnSendEvent;

            var eventJSON = new JSONObject();

            eventJSON["event"] = "hello";
            eventJSON["time"].AsLong = Utility.GetCurrentTime();
            eventJSON["status"] = this._statusManager.StatusJSON;
            eventJSON["other"] = this._statusManager.OtherJSON;

            this.SendAsync(eventJSON.ToString(), null);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Plugin.Logger.Debug("OnClose call.");
            if (this._statusManager != null) {
                this._statusManager.SendEvent -= this.OnSendEvent;
            }
            base.OnClose(e);
        }
    }
}
