using HttpSiraStatus.Util;

#pragma warning disable IDE1006 // 命名スタイル
namespace HttpSiraStatus.Interfaces
{
    public interface IBeatmapEventInformation
    { 
        public string version { get; }
        void Init(BeatmapEventData eventData);
        void Reset();
        JSONObject ToJson();
    }
}
