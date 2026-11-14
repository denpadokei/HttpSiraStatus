using System;
using HttpSiraStatus.Util;
using Xunit;

namespace HttpSiraStatus.Tests.Util
{
    public class UtilityTests
    {
        [Fact]
        public void GetCurrentTime_ReturnsUtcUnixTimeMilliseconds()
        {
            var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var actual = Utility.GetCurrentTime();

            var after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Assert.InRange(actual, before, after);
        }
    }
}
