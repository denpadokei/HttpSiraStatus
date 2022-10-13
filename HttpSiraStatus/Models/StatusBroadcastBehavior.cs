using HttpSiraStatus.Interfaces;
using HttpSiraStatus.Util;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace HttpSiraStatus.Models
{
    public class StatusBroadcastBehavior : WebSocketBehavior
    {
        private IStatusManager _statusManager;
        public void SetStatusManager(IStatusManager statusManager)
        {
            this._statusManager = statusManager;
            this._statusManager.SendEvent += this.OnSendEvent;
        }

        private void OnSendEvent(object sender, SendEventArgs e)
        {
            e.JsonNode["time"].AsLong = Utility.GetCurrentTime();
            this.Send(e.JsonNode.ToString());
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Plugin.Logger.Debug("OnOpen call.");
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
            this._statusManager.SendEvent -= this.OnSendEvent;
            base.OnClose(e);
        }
    }
}
