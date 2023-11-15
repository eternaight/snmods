using HarmonyLib;
using WorldStreaming;

namespace BuildCustomer
{
    public static class OverrideBuildPatches
    {
        [HarmonyPatch(typeof(PAXTerrainController))]
        public static class PAXTerrainController_Patch
        {
            [HarmonyPatch(nameof(PAXTerrainController.LoadAsync))]
            [HarmonyPostfix] [HarmonyDebug] 
            public static void LoadAsync_Postfix(PAXTerrainController __instance) 
            {   
                Plugin.logSource.LogInfo("repositioning voxeland");
                __instance.land.transform.localPosition = Plugin.voxelandLocalPosition;
                __instance.GetComponent<WorldStreamer>().chunkRoot.transform.localPosition = Plugin.voxelandLocalPosition;

                __instance.dataDirPath = Plugin.buildDirectory;
                Plugin.logSource.LogInfo("pax terrain controller load async ran");
            }
        }

        [HarmonyPatch(typeof(RandomStart))]
        public static class RandomStart_Patch
        {
            [HarmonyPatch(nameof(RandomStart.GetRandomStartPoint))]
            [HarmonyPostfix] [HarmonyDebug]
            public static void GetRandomStartPoint_Postfix(ref UnityEngine.Vector3 __result)
            {
                __result = Plugin.lifepodSpawn;
            }
        }
    }
}