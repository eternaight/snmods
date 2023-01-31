using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using NewPlugin.WorldgenAPI;
using UnityEngine;

namespace NewPlugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource logSource;
        public static WorldLaunchConfig config;
        
        private void Awake()
        {
            // Plugin startup logic
            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            config = new WorldLaunchConfig();
            logSource = Logger;

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} - Rebuilding world!");
            RebuildWorld();
            
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} - Rebuild complete!");
        }

        public static void RebuildWorld() {

                var dimensions = new BuildDimensions(
                    octreeSizeVoxels: 32,
                    octreeCounts: new Int3(16, 5, 16),
                    treesPerBatch: Int3.one * 5,
                    cellsPerBatchLevels: new Int3[] {
                        Int3.one * 10,
                        Int3.one * 5,
                        Int3.one * 5,
                        Int3.one * 5
                    },
                    1
                );

                var blueprint = new BuildBlueprint_TreeEverything(dimensions);

                // do stuff
                var populator = new TestPopulator(dimensions);
                blueprint.trees.ForEach(tree => tree.Populate(populator.PopulateVoxel));
                logSource.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} : RebuildWorld - Populated voxels!");

                var bake = blueprint.Bake();
                logSource.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} : RebuildWorld - Baked build!");

                BuildSerializer.SerializeBakedBuild(config.worldDir, bake);
        }

        public class WorldLaunchConfig {
            public readonly string worldDir = "C:\\Games\\Steam\\steamapps\\common\\Subnautica\\Subnautica_Data\\StreamingAssets\\SNUnmanagedData\\Build19";
            public readonly Vector3 voxelZero = new (-256, -80, -256);
        }
    }
}
