using HttpSiraStatus.Enums;
using System.Collections.Generic;

namespace HttpSiraStatus.Models
{
    /// <summary>
    /// Copies a game <see cref="BeatmapEventData"/> into the game-independent
    /// payload on the Unity main thread. No JSON is built here.
    /// </summary>
    internal static class BeatmapEventCapture
    {
        private static readonly Dictionary<int, string> s_easeNames = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> s_axisNames = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> s_colorTypeNames = new Dictionary<int, string>();
        private static readonly Dictionary<int, string> s_rotationDirectionNames = new Dictionary<int, string>();

        public static BeatmapEventPayload Capture(BeatmapEventData data)
        {
            var payload = new BeatmapEventPayload();
            if (data == null) {
                return payload;
            }

            FillFields(ref payload.Fields, data);

            if (data.previousSameTypeEventData != null) {
                payload.HasPrevious = true;
                FillFields(ref payload.Previous, data.previousSameTypeEventData);
            }

            if (data.nextSameTypeEventData != null) {
                payload.HasNext = true;
                FillFields(ref payload.Next, data.nextSameTypeEventData);
            }

            return payload;
        }

        private static void FillFields(ref BeatmapEventFields fields, BeatmapEventData data)
        {
            fields.Time = data.time;
            fields.ExecutionOrder = data.executionOrder;

            switch (data) {
                case BPMChangeBeatmapEventData bpm:
                    fields.Format = BeatmapEventFormat.V3;
                    fields.Type = V3BeatmapEventType.BPM;
                    fields.BPM = bpm.bpm;
                    break;

                case ColorBoostBeatmapEventData colorBoost:
                    fields.Format = BeatmapEventFormat.V3;
                    fields.Type = V3BeatmapEventType.ColorBoost;
                    fields.BoostColorsAreOn = colorBoost.boostColorsAreOn;
                    break;

                case FloatFxBeatmapEventData floatFx:
                    fields.Format = BeatmapEventFormat.V3;
                    fields.Type = V3BeatmapEventType.FloatFx;
                    fields.FloatValue = floatFx.value;
                    fields.EaseType = GetEaseName(floatFx.easeType);
                    break;

                case LightColorBeatmapEventData lightColor:
                    fields.Format = BeatmapEventFormat.V3;
                    fields.Type = V3BeatmapEventType.LightColor;
                    fields.GroupId = lightColor.groupId;
                    fields.ElementId = lightColor.elementId;
                    // The game exposes no transitionType field on the supported
                    // versions; keep the legacy value the protocol has always
                    // reported.
                    fields.TransitionType = "Instant";
                    fields.ColorType = GetColorTypeName(lightColor.colorType);
                    fields.EaseType = GetEaseName(lightColor.easeType);
                    fields.Brightness = lightColor.brightness;
                    fields.StrobeBeatFrequency = lightColor.strobeBeatFrequency;
                    break;

                case LightRotationBeatmapEventData lightRotation:
                    fields.Format = BeatmapEventFormat.V3;
                    fields.Type = V3BeatmapEventType.LightRotation;
                    fields.GroupId = lightRotation.groupId;
                    fields.ElementId = lightRotation.elementId;
                    fields.UsePreviousEventValue = lightRotation.usePreviousEventValue;
                    fields.EaseType = GetEaseName(lightRotation.easeType);
                    fields.Axis = GetAxisName(lightRotation.axis);
                    fields.LoopCount = lightRotation.loopCount;
                    fields.RotationDirection = GetRotationDirectionName(lightRotation.rotationDirection);
                    fields.Rotation = lightRotation.rotation;
                    break;

                case LightTranslationBeatmapEventData lightTranslation:
                    fields.Format = BeatmapEventFormat.V3;
                    fields.Type = V3BeatmapEventType.LightTranslation;
                    fields.GroupId = lightTranslation.groupId;
                    fields.ElementId = lightTranslation.elementId;
                    fields.UsePreviousEventValue = lightTranslation.usePreviousEventValue;
                    fields.EaseType = GetEaseName(lightTranslation.easeType);
                    fields.Axis = GetAxisName(lightTranslation.axis);
                    fields.Translation = lightTranslation.translation;
                    fields.Distribution = lightTranslation.distribution;
                    break;

                case NoteJumpSpeedEventData noteJumpSpeed:
                    fields.Format = BeatmapEventFormat.V3;
                    fields.Type = V3BeatmapEventType.NJS;
                    fields.EaseType = GetEaseName(noteJumpSpeed.easeType);
                    fields.UsePreviousValue = noteJumpSpeed.usePreviousValue;
                    fields.RelativeNoteJumpSpeed = noteJumpSpeed.relativeNoteJumpSpeed;
                    break;

                case BasicBeatmapEventData basic:
                    fields.Format = BeatmapEventFormat.V2;
                    fields.BasicType = (int)basic.basicBeatmapEventType;
                    fields.Value = basic.value;
                    fields.FloatValue = basic.floatValue;
                    break;

                default:
                    fields.Format = BeatmapEventFormat.V3;
                    fields.Type = V3BeatmapEventType.Unknown;
                    break;
            }
        }

        private static string GetEaseName(EaseType value)
        {
            return GetCachedName(s_easeNames, (int)value, value.ToString());
        }

        private static string GetAxisName(LightAxis value)
        {
            return GetCachedName(s_axisNames, (int)value, value.ToString());
        }

        private static string GetColorTypeName(EnvironmentColorType value)
        {
            return GetCachedName(s_colorTypeNames, (int)value, value.ToString());
        }

        private static string GetRotationDirectionName(LightRotationDirection value)
        {
            return GetCachedName(s_rotationDirectionNames, (int)value, value.ToString());
        }

        private static string GetCachedName(Dictionary<int, string> cache, int key, string value)
        {
            if (!cache.TryGetValue(key, out var cached)) {
                cache[key] = value;
                return value;
            }

            return cached;
        }
    }
}
