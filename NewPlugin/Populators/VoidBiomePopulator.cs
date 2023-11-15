using System.Collections.Generic;
using System.Linq;
using NewPlugin.ProcGen;
using NewPlugin.WorldgenAPI;
using NewPlugin.WorldgenEntities;
using UnityEngine;

namespace NewPlugin
{
    public class VoidBiomePopulator : IPopulator
    {
        private readonly Array3<TreeFillingFunction> octreeFillers;
        private readonly System.Random prng;

        public VoidBiomePopulator(int seed, BuildDimensions dimensions)
        {
            prng = new System.Random(seed);
            octreeFillers = new Array3<TreeFillingFunction>(dimensions.octreeCounts.x, dimensions.octreeCounts.y, dimensions.octreeCounts.z);

            var voidfloor = new VoidBiomeHeightmapFiller(prng, dimensions, 64, 5);
            voidfloor.SubscribeToOctrees(octreeFillers);
        }

        public VoxelPayload Populate(Int3 voxel)
        {
            var payload = new VoxelPayload(
                signedDistance: 1,
                solidType: 2
            );

            return payload;
        }

        public void Sprinkle(BuildBlueprint_TreeEverything build)
        {
            foreach (var octree in build.trees)
            {
                var treeFillFunction = octreeFillers.Get(octree.globalTreeIndex);
                if (treeFillFunction != null) {
                    octree.Populate(treeFillFunction);
                }
            }

            var ghostvoxel = new Int3(256, 200, 256);
            build.GetVoxelPayload(ghostvoxel).entityData.Add(new BasicPrefabEntity(
                "54701bfc-bb1a-4a84-8f79-ba4f76691bef", // shocka
                ghostvoxel.ToVector3(),
                false
            ) 
            {
                cellLevel = 0
            });
        }

        // private T SelectRandomFromArray<T> (T[] array) => array[prng.Next(array.Length)];

        private class VoidBiomeHeightmapFiller
        {
            private readonly float[][] heightmap;
            private HashSet<Int3> octrees_of_interest = new();
            private HashSet<Int2> geyser_voxels = new();
            private Int2[] outpost_locations = new Int2[5];
            private readonly System.Random prng;

            public VoidBiomeHeightmapFiller(System.Random prng, BuildDimensions dims, float ridge_width, float ridge_bed_height)
            {
                var size_x = dims.VoxelSize.x;
                var size_z = dims.VoxelSize.z;

                var noiseFreq = Vector3.one / size_x * 5f;
                var noise = new FractalNoise(
                    prng.Next(),
                    5,
                    noiseFreq,
                    30,
                    100,
                    .4f,
                    Vector3.one * 2.5f
                );
                var w = ridge_width * 0.5f * Mathf.Sqrt(size_x * size_x + size_z * size_z);

                const int vent_number = 25;
                const float vent_depth = 15;
                const float vent_width = 10;
                geyser_voxels.AddRange(RandomUtils.ScatterWithMinSeparation(prng, Int2.zero, dims.VoxelSize.xz, vent_number, vent_width * 2));

                const int vent_number_ridge = 15;
                for (int i = 0; i < vent_number_ridge; i++)
                {
                    var t = Random.value;
                    geyser_voxels.Add(Int2.Floor(new Vector2(size_x * t, size_z * (1 - t))));
                }

                outpost_locations = RandomUtils.ScatterWithMinSeparation(prng, Int2.zero, dims.VoxelSize.xz, 5, 30);

                heightmap = new float[size_z][];
                for (int z = 0; z < size_z; z++) 
                {
                    heightmap[z] = new float[size_x];
                    for (int x = 0; x < size_x; x++)
                    {
                        var noise_height = noise.Evaluate(new Vector3(x, 40, z));

                        var value = x * size_z + z * size_x - size_x * size_z;
                        var t = Mathf.Abs(value / w);
                        var y = Mathf.Lerp(ridge_bed_height, noise_height, t);

                        var voxel_flat = new Int2(x, z);
                        var sqr_dist_to_nearest_vent = geyser_voxels.Select(vt => vt - voxel_flat).Select(vt => vt.x * vt.x + vt.y * vt.y).Min();
                        var vent_factor = -vent_depth * Mathf.Exp(-sqr_dist_to_nearest_vent/(vent_width * vent_width));
                        y = Mathf.Clamp(y + vent_factor, ridge_bed_height, dims.VoxelSize.y - 1);

                        heightmap[z][x] = y;
                        
                        var y_floored = Mathf.FloorToInt(y - 0.5f);
                        var y_ceiled = Mathf.CeilToInt(y + 0.5f);
                        if (y_floored < 0) y_floored = 0;
                        if (y_ceiled > dims.VoxelSize.y - 1) y_ceiled = dims.VoxelSize.y - 1;

                        var octree_index_floor = Int3.FloorDiv(new Int3(x, y_floored, z), dims.octreeSizeVoxels);
                        var octree_index_ceil =  Int3.FloorDiv(new Int3(x, y_ceiled, z), dims.octreeSizeVoxels);

                        octrees_of_interest.Add(octree_index_floor);
                        octrees_of_interest.Add(octree_index_ceil);
                    }
                }

                this.prng = prng;
            }

            private readonly string[] outposts = new string[] { 
                "c5512e00-9959-4f57-98ae-9a9962976eaa",
                "542aaa41-26df-4dba-b2bc-3fa3aa84b777",
                "5bcaefae-2236-4082-9a44-716b0598d6ed",
                "20ad299d-ca52-48ef-ac29-c5ec5479e070",
                "430b36ae-94f3-4289-91ac-25475ad3bf74"
            };
            private readonly string[] geysers = new string[] { "ce0b4131-86e2-444b-a507-45f7b824a286", "63462cb4-d177-4551-822f-1904f809ec1f" };

            public VoxelPayload Fill(Int3 voxel)
            {
                var payload = new VoxelPayload(
                    signedDistance: heightmap[voxel.z][voxel.x] - voxel.y - 0.5f,
                    solidType: 69
                );

                if (payload.signedDistance > 0 && payload.signedDistance <= 1)
                {
                    // near + solid

                    if (geyser_voxels.Contains(voxel.xz))
                    {
                        var geyser = geysers[Random.Range(0, 2)];
                        payload.entityData.Add(new BasicPrefabEntity(geyser, voxel.ToVector3() + Vector3.up * 2, false));
                    } 
                    else
                    {
                        var i = System.Array.FindIndex(outpost_locations, el => el == voxel.xz);
                        if (i != -1)
                        {
                            payload.entityData.Add(new BasicPrefabEntity(outposts[i], voxel.ToVector3() + Vector3.up * 8, false));
                        }
                    }

                    // loot roll
                    var roll = prng.Next(0, 75);
                    if (roll == 0)
                    {
                        var loot_entity = new BasicPrefabEntity("480b5570-8c07-4180-a284-45d2a9a8d152", voxel.ToVector3() + Vector3.up, false)
                        {
                            cellLevel = 3
                        };
                        payload.entityData.Add(loot_entity);
                    }
                }

                return payload;
            }

            public void SubscribeToOctrees(Array3<TreeFillingFunction> treeFillers)
            {
                foreach (Int3 index in octrees_of_interest)
                {
                    treeFillers.Set(index, Fill);
                }
            }
        }
    }
}