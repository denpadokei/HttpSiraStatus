using HttpSiraStatus.Enums;
using HttpSiraStatus.Models;
using HttpSiraStatus.Util;
using System.Collections.Generic;

namespace HttpSiraStatus.Interfaces
{
    public interface IStatusManager
    {
        JSONObject StatusJSON { get; }
        Queue<(CutScoreInfoEntity entity, BeatSaberEvent e)> CutScoreInfoQueue { get; }
        JSONObject OtherJSON { get; }
        event SendEventHandler SendEvent;
        void EmitStatusUpdate(ChangedProperty changedProps, BeatSaberEvent e);
        bool EnqueueBeatmapEvent(BeatmapEventPayload payload);
    }
}
