using HttpSiraStatus.Util;

#pragma warning disable IDE1006 // 命名スタイル
namespace HttpSiraStatus.Interfaces
{
    public interface IBeatmapEventInformation
    {
        public string version { get; }
        public float time { get; }
        public int executionOrder { get; }
        public IBeatmapEventInformation previousSameTypeEventData { get; }
        public IBeatmapEventInformation nextSameTypeEventData { get; }
        void Init(BeatmapEventData eventData, bool isChild = false);
        void Reset();
        JSONObject ToJson(bool isChild = false);
    }
}
