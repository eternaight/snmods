using System.Collections;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Nautilus;
using Nautilus.Assets;
using UnityEngine;

namespace WarperTravel
{
    
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("com.eterna.buildlauncher", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.eterna.fringingreefeditor", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        /*
        Plugin draft:

        - Add a method so WarperBalls teleport player to a "subspace" area (DONE)
        - Extend world loading methods so they allow loading of subspace area (DONE)

        - Implement teleports to other... warper locations i guess? (DONE)
        - - Add Nautilus
        - - Add a thing that creates and registers the portal prefab
        - - Get its class id (if unstatic) and generate the subspace voxels

        - Add the new "warpersubspace" biome (DONE)
        - - Create the warper subspace atmo volume prefab
        - - Register
        - - Generate voxels with it
        - - While in the biome, Ghosties are passive & circle around the biome's center (probably hardcoded? if available, use atmo volume's position or maybe batch's cooridnates)

        - Create a NewPlugin dependancy and improve newplugin for it, to make it BETTER idk 
        */

        public static ManualLogSource logSource;
        public static WarperTravelConfig config = new();
        public static BundleMaybeLoaded bundle;

        // thanks to: http://stackoverflow.com/questions/52797/ddg#283917
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var uri = new System.UriBuilder(codeBase);
                string path = System.Uri.UnescapeDataString(uri.Path);
                return System.IO.Path.GetDirectoryName(path);
            }
        }

        public static string GetPluginFile(string localPath)
        {
            return System.IO.Path.Combine(AssemblyDirectory, localPath);
        }

        private void Awake()
        {
            logSource = Logger;

            var harmony = new Harmony(PluginInfo.PLUGIN_GUID);
            harmony.PatchAll();

            LoadAssetBundle();

            var dimensions = new NewPlugin.WorldgenAPI.BuildDimensions(
                octreeSizeVoxels: 32,
                octreeCounts: new Int3(6, 6, 6),
                treesPerBatch: new Int3(5, 5, 5),
                cellsPerBatchLevels: new Int3[] {
                    Int3.one * 10,
                    Int3.one * 5,
                    Int3.one * 5,
                    Int3.one * 5
                },
                1
            );

            var populator = new WarperSubspacePopulator(System.DateTime.Now.GetHashCode(), dimensions);
            
            // NewPlugin.Plugin.BuildWorld(GetPluginFile("Build"), dimensions, populator, config.voxelZero);

            BuildCustomer.Plugin.AddBuildLocation(AssemblyDirectory);

            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void LoadAssetBundle()
        {
            bundle = new BundleMaybeLoaded(GetPluginFile("eternawarpertravel.bundle"));

            var atmoVolumePrefabInfo = new PrefabInfo(config.subspaceVolumeClassID, config.subspaceVolumeClassID + "Prefab", TechType.None);
            var atmoCustomPrefab = new CustomPrefab(atmoVolumePrefabInfo);
            atmoCustomPrefab.SetGameObject(AtmoVolumeFactory);
            atmoCustomPrefab.Register();

            var gatePrefabInfo = new PrefabInfo(config.subspaceGateClassID, config.subspaceGateClassID + "Prefab", TechType.None);
            var gatePrefab = new CustomPrefab(gatePrefabInfo);
            gatePrefab.SetGameObject(GateFactory);
            gatePrefab.Register();
        }

        private IEnumerator AtmoVolumeFactory(IOut<GameObject> gameObject)
        {
            yield return bundle.LoadAssetAsync(config.atmoVolumeAssetName, gameObject);
        }
        private IEnumerator GateFactory(IOut<GameObject> gameObject)
        {
            yield return bundle.LoadAssetAsync(config.gateAssetName, gameObject);
        }

        public class WarperTravelConfig {
            public string warperBiome = "WarperSubspace";
            public readonly Int3 subspaceBatch = new (0,0,0);
            public readonly float subspacePerformanceRadius = 75;
            public Vector3 voxelZero = new (-2048, -3040, -2048);
            public Vector3 subspaceCenter = new (-2048 + 80, -3200 + 160 + 120, -2048 + 80);
            public string atmoVolumeAssetName = "AtmosphereVolume_WarperSubspace";
            public string gateAssetName = "WarperGate";
            public string subspaceVolumeClassID = "com.eterna.warpertravel.subspaceatmo";
            public string subspaceGateClassID = "com.eterna.warpertravel.subspacegate";
        }

        public class BundleMaybeLoaded
        {
            private AssetBundleCreateRequest request;
            public BundleMaybeLoaded(string bundlePath)
            {
                request = AssetBundle.LoadFromFileAsync(bundlePath);
            }
            public IEnumerator LoadAssetAsync(string name, IOut<GameObject> gameObject)
            {
                if (!request.isDone) yield return request;

                var assetRequest = request.assetBundle.LoadAssetAsync(name);
                yield return assetRequest;
                gameObject.Set((GameObject)assetRequest.asset);
            }
        }
    }
}
