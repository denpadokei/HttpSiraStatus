using HttpSiraStatus.Enums;
using HttpSiraStatus.Models;
using HttpSiraStatus.Util;
using System.Collections.Concurrent;

namespace HttpSiraStatus.Interfaces
{
    public interface IStatusManager
    {
        JSONObject StatusJSON { get; }
        ConcurrentQueue<(CutScoreInfoEntity entity, BeatSaberEvent e)> CutScoreInfoQueue { get; }
        ConcurrentQueue<IBeatmapEventInformation> BeatmapEventJSON { get; }
        JSONObject OtherJSON { get; }
        ConcurrentQueue<JSONObject> JsonQueue { get; }
        event SendEventHandler SendEvent;
        void EmitStatusUpdate(ChangedProperty changedProps, BeatSaberEvent e);
    }
}
