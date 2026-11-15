using HttpSiraStatus.Util;

namespace HttpSiraStatus.Models
{
    public enum SendKind
    {
        Message,
        BeatmapEvent,
    }

    /// <summary>
    /// One item in the send queue. Keeping both payload kinds in one struct
    /// preserves the event order without boxing or an extra allocation.
    /// </summary>
    public struct SendItem
    {
        public SendKind Kind;
        public JSONObject Message;
        public BeatmapEventPayload BeatmapEvent;

        public static SendItem ForMessage(JSONObject message)
        {
            return new SendItem
            {
                Kind = SendKind.Message,
                Message = message,
            };
        }

        public static SendItem ForBeatmapEvent(in BeatmapEventPayload payload)
        {
            return new SendItem
            {
                Kind = SendKind.BeatmapEvent,
                BeatmapEvent = payload,
            };
        }
    }
}
