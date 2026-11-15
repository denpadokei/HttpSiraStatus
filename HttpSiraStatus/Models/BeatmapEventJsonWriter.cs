using HttpSiraStatus.Enums;
using System.Globalization;
using System.Text;

namespace HttpSiraStatus.Models
{
    /// <summary>
    /// Serializes a <see cref="BeatmapEventPayload"/> to the protocol's JSON
    /// shape without building a SimpleJSON object graph. The instance is
    /// intended to be used by the single send thread only.
    /// </summary>
    public sealed class BeatmapEventJsonWriter
    {
        private const string V2Version = "2.6.0";
        private const string V3Version = "3.0.0";

        private readonly StringBuilder _builder;

        public BeatmapEventJsonWriter(int capacity = 512)
        {
            this._builder = new StringBuilder(capacity);
        }

        /// <summary>
        /// Writes the complete WebSocket message, including the envelope's
        /// UNIX time, and returns the JSON text.
        /// </summary>
        public string Write(in BeatmapEventPayload payload, long unixTimeMilliseconds)
        {
            var sb = this._builder;
            _ = sb.Clear();
            _ = sb.Append("{\"event\":\"beatmapEvent\",\"time\":");
            AppendLong(sb, unixTimeMilliseconds);
            _ = sb.Append(",\"beatmapEvent\":");
            WriteFields(sb, in payload.Fields);

            _ = sb.Append(",\"previousSameTypeEventData\":");
            if (payload.HasPrevious) {
                WriteFields(sb, in payload.Previous);
                _ = sb.Append('}');
            }
            else {
                _ = sb.Append("null");
            }

            _ = sb.Append(",\"nextSameTypeEventData\":");
            if (payload.HasNext) {
                WriteFields(sb, in payload.Next);
                _ = sb.Append('}');
            }
            else {
                _ = sb.Append("null");
            }

            _ = sb.Append('}'); // beatmapEvent
            _ = sb.Append('}'); // envelope
            return sb.ToString();
        }

        private static void WriteFields(StringBuilder sb, in BeatmapEventFields fields)
        {
            _ = sb.Append('{');

            if (fields.Format == BeatmapEventFormat.V2) {
                _ = sb.Append("\"version\":\"")
                      .Append(V2Version)
                      .Append("\",\"time\":");
                AppendFloat(sb, fields.Time);
                _ = sb.Append(",\"executionOrder\":");
                AppendInt(sb, fields.ExecutionOrder);
                _ = sb.Append(",\"type\":");
                AppendInt(sb, fields.BasicType);
                _ = sb.Append(",\"value\":");
                AppendInt(sb, fields.Value);
                _ = sb.Append(",\"floatValue\":");
                AppendFloat(sb, fields.FloatValue);

                return;
            }

            _ = sb.Append("\"version\":\"")
                  .Append(V3Version)
                  .Append("\",\"type\":");
            AppendInt(sb, (int)fields.Type);
            _ = sb.Append(",\"time\":");
            AppendFloat(sb, fields.Time);
            _ = sb.Append(",\"executionOrder\":");
            AppendInt(sb, fields.ExecutionOrder);

            switch (fields.Type) {
                case V3BeatmapEventType.BPM:
                    _ = sb.Append(",\"bpm\":");
                    AppendFloat(sb, fields.BPM);
                    break;

                case V3BeatmapEventType.ColorBoost:
                    _ = sb.Append(",\"boostColorsAreOn\":");
                    AppendBool(sb, fields.BoostColorsAreOn);
                    break;

                case V3BeatmapEventType.FloatFx:
                    _ = sb.Append(",\"value\":");
                    AppendFloat(sb, fields.FloatValue);
                    _ = sb.Append(",\"easeType\":");
                    AppendString(sb, fields.EaseType);
                    break;

                case V3BeatmapEventType.LightColor:
                    _ = sb.Append(",\"groupId\":");
                    AppendInt(sb, fields.GroupId);
                    _ = sb.Append(",\"elementId\":");
                    AppendInt(sb, fields.ElementId);
                    _ = sb.Append(",\"transitionType\":");
                    AppendString(sb, fields.TransitionType);
                    _ = sb.Append(",\"colorType\":");
                    AppendString(sb, fields.ColorType);
                    _ = sb.Append(",\"brightness\":");
                    AppendFloat(sb, fields.Brightness);
                    _ = sb.Append(",\"strobeBeatFrequency\":");
                    AppendInt(sb, fields.StrobeBeatFrequency);
                    break;

                case V3BeatmapEventType.LightRotation:
                    _ = sb.Append(",\"groupId\":");
                    AppendInt(sb, fields.GroupId);
                    _ = sb.Append(",\"elementId\":");
                    AppendInt(sb, fields.ElementId);
                    _ = sb.Append(",\"usePreviousEventValue\":");
                    AppendBool(sb, fields.UsePreviousEventValue);
                    _ = sb.Append(",\"easeType\":");
                    AppendString(sb, fields.EaseType);
                    _ = sb.Append(",\"axis\":");
                    AppendString(sb, fields.Axis);
                    _ = sb.Append(",\"loopCount\":");
                    AppendInt(sb, fields.LoopCount);
                    _ = sb.Append(",\"rotationDirection\":");
                    AppendString(sb, fields.RotationDirection);
                    _ = sb.Append(",\"rotation\":");
                    AppendFloat(sb, fields.Rotation);
                    break;

                case V3BeatmapEventType.LightTranslation:
                    _ = sb.Append(",\"groupId\":");
                    AppendInt(sb, fields.GroupId);
                    _ = sb.Append(",\"elementId\":");
                    AppendInt(sb, fields.ElementId);
                    _ = sb.Append(",\"usePreviousEventValue\":");
                    AppendBool(sb, fields.UsePreviousEventValue);
                    _ = sb.Append(",\"easeType\":");
                    AppendString(sb, fields.EaseType);
                    _ = sb.Append(",\"axis\":");
                    AppendString(sb, fields.Axis);
                    _ = sb.Append(",\"translation\":");
                    AppendFloat(sb, fields.Translation);
                    _ = sb.Append(",\"distribution\":");
                    AppendFloat(sb, fields.Distribution);
                    break;

                case V3BeatmapEventType.NJS:
                    _ = sb.Append(",\"easeType\":");
                    AppendString(sb, fields.EaseType);
                    _ = sb.Append(",\"usePreviousValue\":");
                    AppendBool(sb, fields.UsePreviousValue);
                    _ = sb.Append(",\"relativeNoteJumpSpeed\":");
                    AppendFloat(sb, fields.RelativeNoteJumpSpeed);
                    break;

                default:
                    break;
            }
        }

        private static void AppendInt(StringBuilder sb, int value)
        {
            _ = sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendLong(StringBuilder sb, long value)
        {
            _ = sb.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AppendFloat(StringBuilder sb, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) {
                // JSON has no NaN/Infinity literals; keep the payload valid.
                _ = sb.Append('0');
                return;
            }

            _ = sb.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void AppendBool(StringBuilder sb, bool value)
        {
            _ = sb.Append(value ? "true" : "false");
        }

        private static void AppendString(StringBuilder sb, string value)
        {
            if (value == null) {
                _ = sb.Append("null");
                return;
            }

            _ = sb.Append('"');
            foreach (var c in value) {
                switch (c) {
                    case '"':
                        _ = sb.Append("\\\"");
                        break;
                    case '\\':
                        _ = sb.Append("\\\\");
                        break;
                    case '\b':
                        _ = sb.Append("\\b");
                        break;
                    case '\f':
                        _ = sb.Append("\\f");
                        break;
                    case '\n':
                        _ = sb.Append("\\n");
                        break;
                    case '\r':
                        _ = sb.Append("\\r");
                        break;
                    case '\t':
                        _ = sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20) {
                            _ = sb.Append("\\u");
                            _ = sb.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else {
                            _ = sb.Append(c);
                        }
                        break;
                }
            }
            _ = sb.Append('"');
        }
    }
}
