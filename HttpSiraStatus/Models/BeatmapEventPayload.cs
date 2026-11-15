using HttpSiraStatus.Enums;

namespace HttpSiraStatus.Models
{
    /// <summary>
    /// Format of the serialized beatmap event. V2 is used for the legacy
    /// <c>BasicBeatmapEventData</c> events, V3 for the newer event types.
    /// </summary>
    public enum BeatmapEventFormat
    {
        V2 = 2,
        V3 = 3,
    }

    /// <summary>
    /// A game-independent snapshot of one beatmap event. The values are copied
    /// from the game object on the Unity main thread so the send thread never
    /// touches a game object.
    /// </summary>
    public struct BeatmapEventFields
    {
        public BeatmapEventFormat Format;
        public V3BeatmapEventType Type;
        public float Time;
        public int ExecutionOrder;

        // V2 / BasicBeatmapEventData
        public int BasicType;
        public int Value;
        public float FloatValue;

        // V3
        public float BPM;
        public bool BoostColorsAreOn;
        public int GroupId;
        public int ElementId;
        public string TransitionType;
        public string ColorType;
        public string EaseType;
        public string Axis;
        public string RotationDirection;
        public bool UsePreviousEventValue;
        public bool UsePreviousValue;
        public float Brightness;
        public int StrobeBeatFrequency;
        public float Rotation;
        public int LoopCount;
        public float Translation;
        public float Distribution;
        public float RelativeNoteJumpSpeed;
    }

    /// <summary>
    /// One event message: the event itself plus its previous/next same-type
    /// events. The flags are required because a default <see cref="BeatmapEventFields"/>
    /// is also a valid zero-valued event.
    /// </summary>
    public struct BeatmapEventPayload
    {
        public BeatmapEventFields Fields;
        public BeatmapEventFields Previous;
        public BeatmapEventFields Next;
        public bool HasPrevious;
        public bool HasNext;
    }
}
