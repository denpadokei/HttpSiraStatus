using System;
using System.Linq;
using UnityEngine;

namespace HttpSiraStatus.Util
{
    public static class Utility
    {
        public static long GetCurrentTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
