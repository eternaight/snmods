using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using WorldStreaming;

namespace WarperTravel
{
    public static class WarperTravelPatches
    {
        [HarmonyPatch(typeof(WarpBall))]
        public static class WarpBall_Patch
        {
            [HarmonyPatch(nameof(WarpBall.WarpOut))]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> WarpOut_Transpiler(IEnumerable<CodeInstruction> instructions) 
            {   
                var found_target = false;
                var target_method = typeof(WarpBall).GetMethod(nameof(WarpBall.GetPositionBeforeWarper), AccessTools.all);
                // var target_method = AccessTools.Method(typeof(WarpBall), nameof(WarpBall.GetPositionBeforeWarper));
                // var replacement_method = typeof(WarpBall_Patch).GetMethod(nameof(GetWarpOutPosition));
                var replacement_method = AccessTools.Method(typeof(WarpBall_Patch), nameof(GetWarpOutPosition));
                foreach (var instruction in instructions)
                {
                    if (!instruction.Calls(target_method)) 
                    {
                        yield return instruction;
                    }
                    else
                    {
                        found_target = true;
                        // consumes WarpBall instance
                        yield return new CodeInstruction(OpCodes.Call, replacement_method);
                    }
                }

                if (!found_target)
                {
                    Plugin.logSource.LogError($"WarpOut_Transpiler: failed to find target code instruction \"{target_method}\"");
                }
            }

            public static Vector3 GetWarpOutPosition(WarpBall instance)
            {
                Vector3 pos;
                if (instance.GetComponent<WarpBallSubspaceTag>())
                {
                    pos = instance.GetComponent<WarpBallSubspaceTag>().destination;
                }
                else
                {
                    pos = WarperSubspace.GetINLocation();
                }
                Plugin.logSource.LogInfo($"{instance.name}: Warping to {pos}");
                return pos;
            }
        }

        [HarmonyPatch(typeof(Creature))]
        public static class Creature_Patch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return typeof(Creature).GetMethod("Start", AccessTools.all);
            }

            [HarmonyPrefix]
            public static void Start_Prefix(Creature __instance)
            {
                if (__instance is GhostLeviathan || __instance is GhostLeviatanVoid)
                {
                    // Plugin.logSource.LogInfo("Ghost Leviathan start!");
                    // __instance._friendlyToPlayer = true;

                    // foreach (var action in __instance.GetComponents<CreatureAction>())
                    // {
                    //     Plugin.logSource.LogInfo("deleting " + action);
                    //     Component.Destroy(action);
                    // }

                    __instance.gameObject.AddComponent<GhostLeviCircleAroundTree>();
                }
            }
        } 

        [HarmonyPatch(typeof(uGUI_DepthCompass))]
        public static class DepthCompass_Patch
        {
            private static readonly string[] GLITCHY_DEPTH_STRINGS = new string[] 
            { 
                "▜▖▞", 
                "▛▖▙", 
                "▉▗▙", 
                "▖▘▋", 
                "▚▜▗", 
                "▜▙▉", 
                "▖▘▝"
            };
            private static readonly System.Random prng = new();

            [HarmonyPatch(nameof(uGUI_DepthCompass.UpdateDepth))]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> UpdateDepth_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                // just call replacement, replace IntStringCache.GetStringForInt calls with checks for warper biome
                var target = typeof(IntStringCache).GetMethod(nameof(IntStringCache.GetStringForInt), AccessTools.all);
                var substitute = typeof(DepthCompass_Patch).GetMethod(nameof(GetStringForDepthSub), AccessTools.all);

                foreach (var instruction in instructions)
                {
                    if (instruction.Calls(target)) yield return new CodeInstruction(OpCodes.Call, substitute);
                    else yield return instruction;
                }
            }

            public static string GetStringForDepthSub(int depth)
            {
                if (Player.main.biomeString == Plugin.config.warperBiome) return GLITCHY_DEPTH_STRINGS[prng.Next() % 7];
                return IntStringCache.GetStringForInt(depth);
            }
        }
        
        [HarmonyPatch(typeof(VoidGhostLeviathansSpawner))]
        public static class VoidGhostLeviathansSpawner_Patch
        {
            [HarmonyPatch(nameof(VoidGhostLeviathansSpawner.IsVoidBiome))]
            [HarmonyPostfix]
            public static void IsVoidBiome_Postfix(ref bool __result, string biomeName)
            {
                if (biomeName == Plugin.config.warperBiome)
                {
                    __result = true;
                }
            }
        }

        [HarmonyPatch(typeof(WaterBiomeManager))]
        public static class WaterBiomeManager_Patch
        {
            [HarmonyPatch(nameof(WaterBiomeManager.Start))]
            [HarmonyPrefix]
            public static void Prefix(WaterBiomeManager __instance)
            {
                var warperSettings = new WaterBiomeManager.BiomeSettings();
                warperSettings.name = Plugin.config.warperBiome;

                var res = new TaskResult<GameObject>();
                var coroutine = Plugin.bundle.LoadAssetAsync("WarperSubspaceSky", res);
                
                while(coroutine.MoveNext());

                warperSettings.skyPrefab = res.value;

                var watersacpesetttings = new WaterscapeVolume.Settings
                {
                    absorption = new Vector3(100f, 18.29155f, 3.531373f),
                    scattering = 1.5f,
                    scatteringColor = new Color(249 / 255f, 158 / 255f, 227 / 255f, 1f),
                    murkiness = 0.025f,
                    emissive = new Color(191 / 255f, 130 / 255f, 219 / 255f, 1f),
                    emissiveScale =  0.2f,
                    startDistance = 1f,
                    sunlightScale = 0,
                    ambientScale = 2f,
                    temperature = 10f
                };
                warperSettings.settings = watersacpesetttings;

                __instance.biomeSettings.Add(warperSettings);
            }
        }
    }
}