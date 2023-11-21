using HarmonyLib;
using IPA.Loader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HttpSiraStatus.HarmonyPatches
{
    [HarmonyPatch]
    internal class GottaGoFastConfigPatch
    {
        public const float s_default_Transition = 0.7f;

        public const string s_targetMethodName = "get_SongStartTransition";

        /// <summary>
        /// パッチを当てるかどうか
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        [HarmonyPrepare]
        public static bool SetMultipliersPrefixPrepare(IEnumerable<MethodBase> original)
        {
            return SetMultipliersPrefixMethod(original).Any();
        }

        /// <summary>
        /// AccCampaignScoreSubmission.dllから対象のメソッド情報を取得します。
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> SetMultipliersPrefixMethod(IEnumerable<MethodBase> original)
        {
            if (original != null && original.OfType<MethodBase>().Any()) {
                foreach (var method in original.OfType<MethodBase>()) {
                    yield return method;
                }
                yield break;
            }
            var gottaGoFastInfo = PluginManager.GetPlugin("Gotta Go Fast");
            var customPlatformsInfo = PluginManager.GetPlugin("Custom Platforms");
            if (gottaGoFastInfo == null || customPlatformsInfo == null) {
                Plugin.Logger.Info("Gotta Go Fast not loaded.");
                yield break;
            }
            var gottaGoFastPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "GottaGoFast.dll");
            Assembly gottaGoFastAssembly;
            try {
                gottaGoFastAssembly = Assembly.LoadFrom(gottaGoFastPath);
            }
            catch (FileNotFoundException) {
                Plugin.Logger.Info("GottaGoFast failed load");
                yield break;
            }
            catch (Exception e) {
                Plugin.Logger.Error(e);
                yield break;
            }
            var pluginConfig = gottaGoFastAssembly.GetType("GottaGoFast.Configuration.PluginConfig");
            
            if (pluginConfig != null) {
                var props = pluginConfig.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var property in props) {
                    var getter = property.GetGetMethod(true);
                    if (IsTarget(getter)) {
                        Plugin.Logger.Info($"{pluginConfig}");
                        Plugin.Logger.Info($"{getter}");
                        yield return getter;
                    }
                }
                yield break;
            }
            else {
                Plugin.Logger.Info("Not found target method.");
                yield break;
            }
        }

        private static bool IsTarget(MethodInfo getter)
        {
            return getter != null && getter.Name == s_targetMethodName;
        }

        [HarmonyPostfix]
        public static void SetMultipliersPrefix(ref float __result)
        {
            if (__result < s_default_Transition) {
                __result = s_default_Transition;
            }
        }
    }
}
