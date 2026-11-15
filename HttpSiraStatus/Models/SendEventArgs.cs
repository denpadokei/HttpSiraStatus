namespace HttpSiraStatus.Models
{
    public class SendEventArgs
    {
        /// <summary>
        /// Serialized message. It is produced once by the send thread and
        /// shared by every WebSocket session.
        /// </summary>
        public string Json { get; }

        public SendEventArgs(string json)
        {
            this.Json = json;
        }
    }

    public delegate void SendEventHandler(object sender, SendEventArgs e);
}
