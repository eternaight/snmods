using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace BuildCustomer
{
    [BepInPlugin("com.eterna.buildlauncher", "BuildLauncher", "0.1")]
    public class Plugin : BaseUnityPlugin
    {
        internal static BepInEx.Logging.ManualLogSource logSource;

        internal static Vector3 voxelandLocalPosition;
        internal static Vector3 lifepodSpawn;
        internal static string buildDirectory;
        internal static System.Collections.Generic.List<string> buildLocations = new();
        private static Harmony harmony;

        private void Awake()
        {
            // Plugin startup logic
            logSource = Logger;
            harmony = new Harmony("com.eterna.buildlauncher");
            PatchAppendages();
            Logger.LogInfo($"Plugin BuildLauncher is loaded!");
        }

        public static void SetParameters(Vector3 voxelandLocalPosition, Vector3 lifepodSpawn, string buildDirectory)
        {
            Plugin.voxelandLocalPosition = voxelandLocalPosition;
            Plugin.lifepodSpawn = lifepodSpawn;
            Plugin.buildDirectory = buildDirectory;

            PatchOverrides();
        }

        private static void PatchOverrides() {
            foreach (var overridepatchtype in typeof(OverrideBuildPatches).GetNestedTypes())
                harmony.PatchAll(overridepatchtype);
        }

        private static void PatchAppendages() {
            foreach (var appendpatchtype in typeof(AppendBuildDirectoriesPatches).GetNestedTypes())
                harmony.PatchAll(appendpatchtype);
        }

        public static void AddBuildLocation(string pluginDirectory) { buildLocations.Add(pluginDirectory); }

        internal static string FirstFolderWithFile(string postfix) => buildLocations.Select(dir => Path.Combine(dir, postfix)).FirstOrDefault(path => File.Exists(path));
    }
}
