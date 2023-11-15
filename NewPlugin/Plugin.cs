using System.IO;
using BepInEx;
using BepInEx.Logging;
using NewPlugin.WorldgenAPI;

namespace NewPlugin
{
    [BepInPlugin("com.eterna.fringingreefeditor", "FringingReefEditor", "0.1")]
    public class Plugin : BaseUnityPlugin
    {
        /*
            TODO: add a thign to read existing builds into a TreeEverythingBuild
            this is actually huge if true
        */

        public static ManualLogSource logSource;

        // thanks to: http://stackoverflow.com/questions/52797/ddg#283917
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                var uri = new System.UriBuilder(codeBase);
                string path = System.Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static string GetPluginPath(string localPath)
        {
            return Path.Combine(AssemblyDirectory, localPath);
        }
        
        private void Awake()
        {
            // Plugin startup logic
            logSource = Logger;
        }

        public static void BuildWorld(string path, BuildDimensions dimensions, IPopulator populator, UnityEngine.Vector3 voxelZeroGlobalPos) {

                logSource.LogInfo($"Plugin FringingReefEditor : Building at path {path}!");

                // var dimensions = new BuildDimensions(
                //     octreeSizeVoxels: 32,
                //     octreeCounts: new Int3(16, 16, 16),
                //     treesPerBatch: Int3.one * 5,
                //     cellsPerBatchLevels: new Int3[] {
                //         Int3.one * 10,
                //         Int3.one * 5,
                //         Int3.one * 5,
                //         Int3.one * 5
                //     },
                //     1
                // );

                var blueprint = new BuildBlueprint_TreeEverything(dimensions);

                // create log file
                // var logfile_path = Path.Combine(AssemblyDirectory, "world_rebuild_log.txt");
                // var logfile_stream = File.Open(logfile_path, FileMode.Create);
                // var logfile_writer = new StreamWriter(logfile_stream);
                var sw = new System.Diagnostics.Stopwatch();
                sw.Start();

                // do stuff
                populator.Sprinkle(blueprint);

                sw.Stop();
                logSource.LogInfo($"Plugin FringingReefEditor : Populated voxels! ({sw.Elapsed.TotalSeconds})");
                sw.Restart();

                var bake = blueprint.Bake(voxelZeroGlobalPos);
                sw.Stop();
                logSource.LogInfo($"Plugin FringingReefEditor : Baked build! ({sw.Elapsed.TotalSeconds})");

                BuildSerializer.SerializeBakedBuild(path, bake);
        }
    }
}
