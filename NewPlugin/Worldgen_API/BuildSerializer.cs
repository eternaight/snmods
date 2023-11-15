using System.IO;

namespace NewPlugin.WorldgenAPI 
{
    public static class BuildSerializer 
    {
        public static void SerializeBakedBuild(string buildDirectoryPath, BuildBaked bake) 
        {
            if (!Directory.Exists(buildDirectoryPath))
            {
                Directory.CreateDirectory(buildDirectoryPath);
            }

            // write index.txt
            using (StreamWriter s = new(File.Create(Path.Combine(buildDirectoryPath, "index.txt")))) 
            {
                s.WriteLine(0); // dud
                s.WriteLine($"{bake.dimensions.VoxelSize.x} {bake.dimensions.VoxelSize.y} {bake.dimensions.VoxelSize.z}");
                s.WriteLine($"{bake.dimensions.octreeCounts.x} {bake.dimensions.octreeCounts.y} {bake.dimensions.octreeCounts.z}");
                s.WriteLine(bake.dimensions.octreeSizeVoxels);
                s.WriteLine($"{bake.dimensions.treesPerBatch.x} {bake.dimensions.treesPerBatch.y} {bake.dimensions.treesPerBatch.z}");
                foreach (var _ in Int3.Range(bake.dimensions.BatchCounts))
                    s.WriteLine(0); // duds
            };

            // meta
            using (StreamWriter s = new(File.Create(Path.Combine(buildDirectoryPath, "meta.txt")))) {
                s.WriteLine("39");
                s.WriteLine("BlockPrefabs");
            }

            // write voxels, batch objects, cells
            SerializeBatchData(bake, buildDirectoryPath);

            // biomes.csv
            using (StreamWriter s = new(File.Create(Path.Combine(buildDirectoryPath, "biomes.csv")))) 
            {
                s.WriteLine("name");
                foreach (var name in bake.biomenames) 
                {
                    s.WriteLine(name);
                }
            }

            // 2D biomemap
            File.WriteAllBytes(Path.Combine(buildDirectoryPath, "biomeMap.bin"), bake.biomemap);
        }

        private static void SerializeBatchData(BuildBaked bake, string buildDirectoryPath) 
        {
            var compiledOctreesDirectory = Path.Combine(buildDirectoryPath, "CompiledOctreesCache");
            Directory.CreateDirectory(compiledOctreesDirectory);
            var cellsCacheDirectory = Path.Combine(buildDirectoryPath, "CellsCache");
            Directory.CreateDirectory(cellsCacheDirectory);
            var batchObjectsDirectory = Path.Combine(buildDirectoryPath, "BatchObjectsCache");
            Directory.CreateDirectory(batchObjectsDirectory);

            foreach(var entry in bake.batches) 
            {
                var batchIndex = entry.Key;
                var batch = entry.Value;                

                if (batch.octNodeDatas != null)
                {
                    Plugin.logSource.LogInfo($"Writing batch {batchIndex} optoctrees ");
                    WriteBatchOptoctrees(batch.octNodeDatas, batchIndex, compiledOctreesDirectory);
                } else {
                    Plugin.logSource.LogInfo($"Received null batch {batchIndex} optoctrees array, skipping ");
                }

                if (batch.batchEntities != null)
                {
                    Plugin.logSource.LogInfo($"Writing batch {batchIndex} objects");

                    using var fs = new FileStream(Path.Combine(batchObjectsDirectory, $"batch-objects-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}.bin"), FileMode.Create);
                    var entityData = batch.batchEntities;
                    var batchOrigin = bake.dimensions.treesPerBatch * bake.dimensions.octreeSizeVoxels;
                    EntitySerializer.WriteBatchEntities(entityData, fs);
                } else {
                    Plugin.logSource.LogInfo($"Received null batch {batchIndex} entities array, skipping ");
                }

                var batchCells = batch.batchCells;
                if (batchCells != null) 
                {
                    Plugin.logSource.LogInfo($"Writing batch {batchIndex} cells");
                    using var fs = new FileStream(Path.Combine(cellsCacheDirectory, $"baked-batch-cells-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}.bin"), FileMode.Create);
                    EntitySerializer.WriteCellsCache(batchCells, fs);
                } else {
                    Plugin.logSource.LogInfo($"Received null batch {batchIndex} cells array, skipping ");
                }
            }
        }

        private static void WriteBatchOptoctrees(OctNodeData[][] octNodeDatas, Int3 batchIndex, string compiledOctreesDirectory) 
        {
            var filename = $"compiled-batch-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}.optoctrees";
            var writeStream = new FileStream(Path.Combine(compiledOctreesDirectory, filename), FileMode.Create);

            var writer = new BinaryWriter(writeStream);
            writer.Write(4);

            // enumerate through octrees within this batch
            foreach (var array in octNodeDatas)
            {
                writer.Write((ushort)array.Length);
                foreach (var data in array)
                {
                    writer.Write(data.type);
                    writer.Write(data.density);
                    writer.Write(data.childPosition);
                }
            }

            Plugin.logSource.LogInfo($"Writing {octNodeDatas.Length} trees to batch {batchIndex}");

            writer.Close();
        }
    }
}