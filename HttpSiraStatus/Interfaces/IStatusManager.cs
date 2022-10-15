using HttpSiraStatus.Enums;
using HttpSiraStatus.Models;
using HttpSiraStatus.Util;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HttpSiraStatus.Interfaces
{
    public interface IStatusManager
    {
        JSONObject StatusJSON { get; }
        Queue<(CutScoreInfoEntity entity, BeatSaberEvent e)> CutScoreInfoQueue { get; }
        Queue<IBeatmapEventInformation> BeatmapEventJSON { get; }
        JSONObject OtherJSON { get; }
        ConcurrentQueue<JSONObject> JsonQueue { get; }
        event SendEventHandler SendEvent;
        void EmitStatusUpdate(ChangedProperty changedProps, BeatSaberEvent e);
    }
}
