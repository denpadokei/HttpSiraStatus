using HarmonyLib;

namespace HttpSiraStatus.HarmonyPatches
{
    /// <summary>
    /// カスタムプラットフォームが死なないようにするやつ
    /// </summary>
    [HarmonyPatch(typeof(TrackLaneRingsManager), MethodType.Constructor)]
    internal class TrackLaneRingsManagerPatch
    {
        public static void Postfix(ref TrackLaneRing[] ____rings)
        {
            if (____rings == null) {
                ____rings = new TrackLaneRing[0];
            }
        }
    }
}
