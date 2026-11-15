using System.Globalization;
using HttpSiraStatus.Enums;
using HttpSiraStatus.Models;
using Xunit;

namespace HttpSiraStatus.Tests.Models
{
    public class BeatmapEventJsonWriterTests
    {
        private const long Time = 1700000000000L;

        private static string Write(BeatmapEventPayload payload)
        {
            return new BeatmapEventJsonWriter().Write(payload, Time);
        }

        private static BeatmapEventPayload Payload(BeatmapEventFields fields)
        {
            return new BeatmapEventPayload { Fields = fields };
        }

        [Fact]
        public void Write_V2Basic_WritesV2Shape()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V2,
                Time = 1.5f,
                ExecutionOrder = 3,
                BasicType = 1,
                Value = 2,
                FloatValue = 0.25f,
            });

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"2.6.0\",\"time\":1.5,\"executionOrder\":3,\"type\":1,\"value\":2,\"floatValue\":0.25,\"previousSameTypeEventData\":null,\"nextSameTypeEventData\":null}}",
                Write(payload));
        }

        [Fact]
        public void Write_Bpm_WritesV3Shape()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.BPM,
                Time = 2f,
                ExecutionOrder = 4,
                BPM = 120.5f,
            });

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"3.0.0\",\"type\":1,\"time\":2,\"executionOrder\":4,\"bpm\":120.5,\"previousSameTypeEventData\":null,\"nextSameTypeEventData\":null}}",
                Write(payload));
        }

        [Fact]
        public void Write_ColorBoost_WritesV3Shape()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.ColorBoost,
                Time = 3.5f,
                ExecutionOrder = 5,
                BoostColorsAreOn = true,
            });

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"3.0.0\",\"type\":2,\"time\":3.5,\"executionOrder\":5,\"boostColorsAreOn\":true,\"previousSameTypeEventData\":null,\"nextSameTypeEventData\":null}}",
                Write(payload));
        }

        [Fact]
        public void Write_FloatFx_WritesV3Shape()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.FloatFx,
                Time = 4f,
                ExecutionOrder = 6,
                FloatValue = 0.75f,
                EaseType = "InQuad",
            });

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"3.0.0\",\"type\":3,\"time\":4,\"executionOrder\":6,\"value\":0.75,\"easeType\":\"InQuad\",\"previousSameTypeEventData\":null,\"nextSameTypeEventData\":null}}",
                Write(payload));
        }

        [Fact]
        public void Write_LightColor_WritesV3Shape()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.LightColor,
                Time = 5f,
                ExecutionOrder = 7,
                GroupId = 1,
                ElementId = 2,
                TransitionType = "Fade",
                ColorType = "Color0",
                Brightness = 0.5f,
                StrobeBeatFrequency = 3,
            });

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"3.0.0\",\"type\":4,\"time\":5,\"executionOrder\":7,\"groupId\":1,\"elementId\":2,\"transitionType\":\"Fade\",\"colorType\":\"Color0\",\"brightness\":0.5,\"strobeBeatFrequency\":3,\"previousSameTypeEventData\":null,\"nextSameTypeEventData\":null}}",
                Write(payload));
        }

        [Fact]
        public void Write_LightRotation_WritesV3Shape()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.LightRotation,
                Time = 6f,
                ExecutionOrder = 8,
                GroupId = 1,
                ElementId = 2,
                UsePreviousEventValue = true,
                EaseType = "InQuad",
                Axis = "X",
                LoopCount = 2,
                RotationDirection = "Clockwise",
                Rotation = 90f,
            });

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"3.0.0\",\"type\":5,\"time\":6,\"executionOrder\":8,\"groupId\":1,\"elementId\":2,\"usePreviousEventValue\":true,\"easeType\":\"InQuad\",\"axis\":\"X\",\"loopCount\":2,\"rotationDirection\":\"Clockwise\",\"rotation\":90,\"previousSameTypeEventData\":null,\"nextSameTypeEventData\":null}}",
                Write(payload));
        }

        [Fact]
        public void Write_LightTranslation_WritesNumbersForTranslationAndDistribution()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.LightTranslation,
                Time = 7f,
                ExecutionOrder = 9,
                GroupId = 1,
                ElementId = 2,
                EaseType = "OutQuad",
                Axis = "Y",
                Translation = 1.5f,
                Distribution = 0.25f,
            });

            var json = Write(payload);

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"3.0.0\",\"type\":6,\"time\":7,\"executionOrder\":9,\"groupId\":1,\"elementId\":2,\"usePreviousEventValue\":false,\"easeType\":\"OutQuad\",\"axis\":\"Y\",\"translation\":1.5,\"distribution\":0.25,\"previousSameTypeEventData\":null,\"nextSameTypeEventData\":null}}",
                json);
            Assert.DoesNotContain("\"translation\":\"", json);
            Assert.DoesNotContain("\"distribution\":\"", json);
        }

        [Fact]
        public void Write_Njs_WritesTypeAndFields()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.NJS,
                Time = 8f,
                ExecutionOrder = 10,
                EaseType = "None",
                UsePreviousValue = true,
                RelativeNoteJumpSpeed = 1.25f,
            });

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"3.0.0\",\"type\":8,\"time\":8,\"executionOrder\":10,\"easeType\":\"None\",\"usePreviousValue\":true,\"relativeNoteJumpSpeed\":1.25,\"previousSameTypeEventData\":null,\"nextSameTypeEventData\":null}}",
                Write(payload));
        }

        [Fact]
        public void Write_Unknown_WritesTypeOnly()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.Unknown,
                Time = 9f,
                ExecutionOrder = 11,
            });

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"3.0.0\",\"type\":0,\"time\":9,\"executionOrder\":11,\"previousSameTypeEventData\":null,\"nextSameTypeEventData\":null}}",
                Write(payload));
        }

        [Fact]
        public void Write_PreviousAndNext_WritesNestedEvents()
        {
            var payload = new BeatmapEventPayload
            {
                Fields = new BeatmapEventFields
                {
                    Format = BeatmapEventFormat.V3,
                    Type = V3BeatmapEventType.LightColor,
                    Time = 10f,
                    ExecutionOrder = 12,
                    GroupId = 1,
                    ElementId = 2,
                    TransitionType = "Fade",
                    ColorType = "Color0",
                    Brightness = 0.5f,
                },
                HasPrevious = true,
                Previous = new BeatmapEventFields
                {
                    Format = BeatmapEventFormat.V3,
                    Type = V3BeatmapEventType.LightColor,
                    Time = 9f,
                    ExecutionOrder = 11,
                    GroupId = 9,
                    ElementId = 8,
                    TransitionType = "Instant",
                    ColorType = "Color1",
                    Brightness = 1f,
                },
            };

            Assert.Equal(
                "{\"event\":\"beatmapEvent\",\"time\":1700000000000,\"beatmapEvent\":{\"version\":\"3.0.0\",\"type\":4,\"time\":10,\"executionOrder\":12,\"groupId\":1,\"elementId\":2,\"transitionType\":\"Fade\",\"colorType\":\"Color0\",\"brightness\":0.5,\"strobeBeatFrequency\":0,\"previousSameTypeEventData\":{\"version\":\"3.0.0\",\"type\":4,\"time\":9,\"executionOrder\":11,\"groupId\":9,\"elementId\":8,\"transitionType\":\"Instant\",\"colorType\":\"Color1\",\"brightness\":1,\"strobeBeatFrequency\":0},\"nextSameTypeEventData\":null}}",
                Write(payload));
        }

        [Fact]
        public void Write_EscapesStrings()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.FloatFx,
                EaseType = "a\"b\\c",
            });

            Assert.Contains("\"easeType\":\"a\\\"b\\\\c\"", Write(payload));
        }

        [Fact]
        public void Write_NullString_WritesNull()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.FloatFx,
                EaseType = null,
            });

            Assert.Contains("\"easeType\":null", Write(payload));
        }

        [Fact]
        public void Write_NonFiniteFloat_WritesZero()
        {
            var payload = Payload(new BeatmapEventFields
            {
                Format = BeatmapEventFormat.V3,
                Type = V3BeatmapEventType.FloatFx,
                FloatValue = float.NaN,
                EaseType = "None",
            });

            Assert.Contains("\"value\":0,", Write(payload));
        }

        [Fact]
        public void Write_UsesInvariantCulture()
        {
            var old = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");
                var payload = Payload(new BeatmapEventFields
                {
                    Format = BeatmapEventFormat.V3,
                    Type = V3BeatmapEventType.BPM,
                    BPM = 1.5f,
                });

                var json = Write(payload);

                Assert.Contains("\"bpm\":1.5,", json);
                Assert.DoesNotContain("1,5", json);
            }
            finally
            {
                CultureInfo.CurrentCulture = old;
            }
        }
    }
}
