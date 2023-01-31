using HarmonyLib;
using WorldStreaming;

namespace NewPlugin
{
    public static class NewPluginPatches
    {
        [HarmonyPatch(typeof(PAXTerrainController))]
        public static class PAXTerrainController_Patch
        {
            [HarmonyPatch(nameof(PAXTerrainController.LoadAsync))]
            [HarmonyPostfix] [HarmonyDebug] 
            public static void LoadAsync_Postfix(PAXTerrainController __instance) 
            {   
                Plugin.logSource.LogInfo("repositioning voxeland");
                __instance.land.transform.localPosition = Plugin.config.voxelZero;
                __instance.GetComponent<WorldStreamer>().chunkRoot.transform.localPosition = Plugin.config.voxelZero;

                __instance.dataDirPath = Plugin.config.worldDir;
                Plugin.logSource.LogInfo("pax terrain controller load async ran");
            }
        }
    }
}