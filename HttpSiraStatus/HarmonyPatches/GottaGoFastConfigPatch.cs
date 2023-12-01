using HarmonyLib;
using IPA.Loader;
using System;
using System.IO;
using System.Reflection;

namespace HttpSiraStatus.HarmonyPatches
{
    [HarmonyPatch]
    internal class GottaGoFastConfigPatch
    {

        public const bool s_enableOptimizations = false;

        public const string s_targetMethodName = "__IsEnabled";

        /// <summary>
        /// パッチを当てるかどうか
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        [HarmonyPrepare]
        public static bool SetMultipliersPrefixPrepare(MethodBase original)
        {
            return SetMultipliersPrefixMethod(original) != null;
        }

        /// <summary>
        /// Gotta Go Fastから対象のメソッド情報を取得します。
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        [HarmonyTargetMethod]
        public static MethodBase SetMultipliersPrefixMethod(MethodBase original)
        {
            if (original != null) {
                return original;
            }
            var gottaGoFastInfo = PluginManager.GetPlugin("Gotta Go Fast");
            var customPlatformsInfo = PluginManager.GetPlugin("Custom Platforms");
            if (gottaGoFastInfo == null || customPlatformsInfo == null) {
                Plugin.Logger.Info("Gotta Go Fast not loaded.");
                return null;
            }
            var gottaGoFastPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GottaGoFast.dll");
            Assembly gottaGoFastAssembly;
            try {
                gottaGoFastAssembly = Assembly.LoadFrom(gottaGoFastPath);
            }
            catch (FileNotFoundException) {
                Plugin.Logger.Info("GottaGoFast failed load");
                return null;
            }
            catch (Exception e) {
                Plugin.Logger.Error(e);
                return null;
            }
            var patchGameScenesManagerConfig = gottaGoFastAssembly.GetType("GottaGoFast.HarmonyPatches.PatchGameScenesManager");

            if (patchGameScenesManagerConfig != null) {
                var method = patchGameScenesManagerConfig.GetMethod(s_targetMethodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                Plugin.Logger.Info($"{patchGameScenesManagerConfig}");
                Plugin.Logger.Info($"{method?.Name}");
                return method;
            }
            else {
                Plugin.Logger.Info("Not found target method.");
                return null;
            }
        }

        /// <summary>
        /// 独自処理を無効化する
        /// </summary>
        /// <param name="__result"></param>
        [HarmonyPostfix]
        public static void HarmonyPostfix(ref bool __result)
        {
            __result = s_enableOptimizations;
        }
    }
}
