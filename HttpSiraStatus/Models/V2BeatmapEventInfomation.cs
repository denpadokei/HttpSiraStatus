using HttpSiraStatus.Interfaces;
using HttpSiraStatus.Util;
using IPA.Utilities;
using Zenject;

namespace HttpSiraStatus.Models
{
    internal class V2BeatmapEventInfomation : IBeatmapEventInformation
    {
        public string version { get; } = "2.6.0";
        public int beatmapEventType { get; private set; } = 0;
        public int beatmapEventValue { get; private set; } = 0;
        public float beatmapEventFloatValue { get; private set; } = 0;
        public float time { get; private set; } = 0;
        public int executionOrder { get; private set; } = 0;
        public IBeatmapEventInformation previousSameTypeEventData { get; private set; } = null;
        public IBeatmapEventInformation nextSameTypeEventData { get; private set; } = null;
        public JSONObject SilializedJson { get; } = new JSONObject();

        public void Init(BeatmapEventData eventData, bool isChild)
        {
            if (eventData is BasicBeatmapEventData basic) {
                this.time = basic.time;
                this.executionOrder = basic.executionOrder;
                this.beatmapEventType = (int)basic.basicBeatmapEventType;
                this.beatmapEventValue = basic.value;
                this.beatmapEventFloatValue = basic.floatValue;
                if (!isChild) {
                    this.previousSameTypeEventData ??= new V2BeatmapEventInfomation();
                    this.previousSameTypeEventData.Init(eventData.previousSameTypeEventData, true);
                    this.nextSameTypeEventData ??= new V2BeatmapEventInfomation();
                    this.nextSameTypeEventData.Init(eventData.nextSameTypeEventData, true);
                }
            }
            this.ToJson(false);
        }

        public void Reset()
        {
            this.time = 0;
            this.executionOrder = 0;
            this.beatmapEventType = 0;
            this.beatmapEventValue = 0;
            this.beatmapEventFloatValue = 0;
            this.previousSameTypeEventData?.Reset();
            this.nextSameTypeEventData?.Reset();
            this.ToJson(false);
        }

        public JSONNode ToJson(bool isChild)
        {
            var result = this.SilializedJson;
            result.Clear();
            result["version"] = this.version;
            result["time"] = this.time;
            result["executionOrder"] = this.executionOrder;
            result["type"] = this.beatmapEventType;
            result["value"] = this.beatmapEventValue;
            result["floatValue"] = this.beatmapEventFloatValue;
            if (!isChild) {
                result["previousSameTypeEventData"] = this.previousSameTypeEventData?.ToJson(true);
                result["nextSameTypeEventData"] = this.nextSameTypeEventData?.ToJson(true);
            }
            return result.Clone();
        }
    }
}
