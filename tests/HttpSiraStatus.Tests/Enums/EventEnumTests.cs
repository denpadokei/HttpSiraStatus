using System;
using System.Linq;
using HttpSiraStatus.Enums;
using Xunit;

namespace HttpSiraStatus.Tests.Enums
{
    public class EventEnumTests
    {
        [Fact]
        public void GetDescription_ReturnsDescriptionAttribute()
        {
            Assert.Equal("songStart", BeatSaberEvent.SongStart.GetDescription());
            Assert.Equal("noteCut", BeatSaberEvent.NoteCut.GetDescription());
            Assert.Equal("other", BeatSaberEvent.Other.GetDescription());
        }

        [Fact]
        public void BeatSaberEvent_DescriptionsAreNotEmptyAndUnique()
        {
            var descriptions = Enum.GetValues(typeof(BeatSaberEvent))
                .Cast<BeatSaberEvent>()
                .Select(value => value.GetDescription())
                .ToArray();

            Assert.All(descriptions, description => Assert.False(string.IsNullOrWhiteSpace(description)));
            Assert.Equal(descriptions.Length, descriptions.Distinct().Count());
        }

        [Fact]
        public void ChangedProperty_FlagsCompose()
        {
            Assert.True(ChangedProperty.PerformanceAndNoteCut.HasFlag(ChangedProperty.Performance));
            Assert.True(ChangedProperty.PerformanceAndNoteCut.HasFlag(ChangedProperty.NoteCut));
            Assert.False(ChangedProperty.AllButNoteCut.HasFlag(ChangedProperty.NoteCut));
            Assert.Equal(
                ChangedProperty.Game | ChangedProperty.Beatmap | ChangedProperty.Performance | ChangedProperty.Mod,
                ChangedProperty.AllButNoteCut);
        }
    }
}
