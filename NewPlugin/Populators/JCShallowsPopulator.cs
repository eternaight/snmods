using System;
using System.Collections.Generic;
using System.Linq;
using NewPlugin.ProcGen;
using NewPlugin.WorldgenAPI;
using NewPlugin.WorldgenEntities;
using UnityEngine;

namespace NewPlugin
{
    public class JCShallowsPopulator : IPopulator
    {
        private readonly int seed;
        private readonly BuildDimensions dimensions;
        private readonly float[][] heightmap;
        private readonly float[][] shallowMaterialHeightmap;

        private readonly List<Int3> largeObjectSpawns = new();
        private readonly List<Int3> mediumObjectSpawns = new(); 
        private readonly List<Int3> smallObjectSpawns = new();

        private readonly System.Random prng;

        private readonly string[] bigshrooms = new string[] {
            "d586a247-122a-427d-9032-f42e898df17f",
            "400fa668-152d-4b81-ad8f-a3cef16efed8",
            "8d0b24b7-c71f-42ab-8df9-7bfe05616ab4",
            "a3d11348-e589-4867-ac60-1fa122145615"
        };

        private readonly string[] lootslotIds = new string[] 
        {
            "9aa6ffb9-666b-45c2-b961-68fd16efccca",
            "b3d5e742-554f-4f7f-a36c-57c800c082a9",
            "0aaad212-6566-4a70-9cea-adeafc61f790",
            "7a5a19ed-bbde-4d45-9e44-cbd56784badb",
            "a07fdcaa-ef72-43b8-9c3a-6c687b23b16e"
        };

        private readonly string[] creatureslotIds = new string[] 
        {
            "73b99f65-02a4-4bea-a6f6-3f67e2ccf638",
            "9cc577c4-ea13-4dcc-82e3-70fa5e3f32fc",
            "7f69484a-f949-4303-b6f5-f3aad4934fb9",
            "679ef7e4-a419-4d4d-9860-6e87555d597a",
            "7800c39a-e283-4854-ae83-1b97253ecc0d",
            "6bbc5b08-a0e9-46b8-a332-fe4188f143c0"
        };

        public JCShallowsPopulator(int seed, BuildDimensions dimensions)
        {
            this.seed = seed;
            prng = new System.Random(seed);
            this.dimensions = dimensions;

            var noise = new FractalNoise(
                seed,
                4,
                new Vector3(1f / dimensions.VoxelSize.x, 1f / dimensions.VoxelSize.y, 1f / dimensions.VoxelSize.z) * 10f,
                30,
                60,
                .5f,
                Vector3.one * 2.5f
            );

            var grassyNoise = new FractalNoise(
                seed + "whatever".GetHashCode(),
                1,
                new Vector3(1f / dimensions.VoxelSize.x, 1f / dimensions.VoxelSize.y, 1f / dimensions.VoxelSize.z) * 20f,
                40,
                47,
                1,
                Vector3.one
            );
            
            heightmap = new float[dimensions.VoxelSize.z][];
            shallowMaterialHeightmap = new float[dimensions.VoxelSize.z][];
            for (int z = 0; z < dimensions.VoxelSize.z; z++) 
            {
                heightmap[z] = new float[dimensions.VoxelSize.x];
                shallowMaterialHeightmap[z] = new float[dimensions.VoxelSize.x];
                for (int x = 0; x < dimensions.VoxelSize.x; x++)
                {
                    heightmap[z][x] = noise.Evaluate(new Vector3(x, 40, z));
                    shallowMaterialHeightmap[z][x] =  grassyNoise.Evaluate(new Vector3(x, 0, z));
                }
            }

            GenerateObjectSpawns();
        }

        private void GenerateObjectSpawns()
        {
            var distanceMap = new int[dimensions.VoxelSize.z][];
            var validSpawnBlocks = new List<Int3>();

            // LARGE
            {
                const int LargeObjectCount = 50;

                const int minSeparation = 25;
                var spotsCount = Int3.CeilDiv(dimensions.VoxelSize, minSeparation).XZ();
                var largeObjectSpots = new bool[spotsCount.y][];
                for (int y = 0; y < spotsCount.y; y++)
                {
                    largeObjectSpots[y] = new bool[spotsCount.x];
                }

                for (int i = 0; i < LargeObjectCount && largeObjectSpots.Any(ar => ar.Any(el => !el)); i++) 
                {
                    Int2 pos;
                    Int2 spot;
                    do
                    {
                        pos = RandomUtils.RandomInsideBounds(prng, Int2.zero, dimensions.VoxelSize.XZ() - 1);
                        spot = pos / minSeparation;
                    } while (largeObjectSpots[spot.y][spot.x]);
                    
                    largeObjectSpawns.Add(new Int3(pos.x, Mathf.FloorToInt(heightmap[pos.y][pos.x]), pos.y));

                    for (int v = Math.Max(spot.y - 1, 0); v <= Math.Min(spot.y + 1, spotsCount.y - 1); v++) 
                    {
                        for (int u = Math.Max(spot.x - 1, 0); u <= Math.Min(spot.x + 1, spotsCount.x - 1); u++) 
                        {
                            largeObjectSpots[v][u] = true;
                        }
                    }
                }
                for (int z = 0; z < dimensions.VoxelSize.z; z++) 
                {
                    distanceMap[z] = new int[dimensions.VoxelSize.x];
                    for (int x = 0; x < dimensions.VoxelSize.x; x++)
                    {
                        distanceMap[z][x] = largeObjectSpawns.Select(spawn => SqrDistance2(x, z, spawn.x, spawn.z)).Min();
                    }
                }
            }

            // MEDIUMS
            const int MediumObjectCount = 100;
            const int minDistance = 25;
            const int maxDistance = 400;
            for (int z = 0; z < dimensions.VoxelSize.z; z++) 
            {
                for (int x = 0; x < dimensions.VoxelSize.x; x++)
                {
                    if (distanceMap[z][x] >= minDistance && distanceMap[z][x] <= maxDistance)
                    {
                        var y = Mathf.FloorToInt(heightmap[z][x]);
                        validSpawnBlocks.Add(new Int3(x, y, z));
                    }
                }
            }
            for (int i = 0; i < MediumObjectCount && validSpawnBlocks.Count > 0; i++) 
            {
                var index = prng.Next(validSpawnBlocks.Count);
                var pos = validSpawnBlocks[index];
                validSpawnBlocks.RemoveAt(index);

                mediumObjectSpawns.Add(pos);
            }
            for (int z = 0; z < dimensions.VoxelSize.z; z++) 
            {
                for (int x = 0; x < dimensions.VoxelSize.x; x++)
                {
                    var mindist = mediumObjectSpawns.Select(spawn => SqrDistance2(x, z, spawn.x, spawn.z)).Min();
                    distanceMap[z][x] = Math.Min(mindist, distanceMap[z][x]);
                }
            }
            validSpawnBlocks.Clear();

            // SMALL
            const int SmallObjectCount = 500;
            const int small_minDistance = 1;
            const int small_maxDistance = 100;
            for (int z = 0; z < dimensions.VoxelSize.z; z++) 
            {
                for (int x = 0; x < dimensions.VoxelSize.x; x++)
                {
                    if (distanceMap[z][x] >= small_minDistance && distanceMap[z][x] <= small_maxDistance)
                    {
                        var y = Mathf.FloorToInt(heightmap[z][x]);
                        validSpawnBlocks.Add(new Int3(x, y, z));
                    }
                }
            }
            for (int i = 0; i < SmallObjectCount && validSpawnBlocks.Count > 0; i++) 
            {
                var index = prng.Next(validSpawnBlocks.Count);
                var pos = validSpawnBlocks[index];
                validSpawnBlocks.RemoveAt(index);

                pos.y = (int)heightmap[pos.z][pos.x];
                smallObjectSpawns.Add(pos);
            }
        }

        public VoxelPayload Populate(Int3 voxel)
        {
            var height = heightmap[voxel.z][voxel.x];
            var pos = voxel.ToVector3() + Vector3.one * 0.5f;

            var solidType = height > shallowMaterialHeightmap[voxel.z][voxel.x] ? 75 : 187;

            var payload = new VoxelPayload(
                signedDistance: height - pos.y,
                solidType: solidType
            );

            // entity slot spawns
            if (payload.IsNearSurface())
            {
                // spawn loot
                if (prng.Next(1000) < 5)
                {
                    payload.entityData.Add(new BasicPrefabEntity
                    (
                        classId: SelectRandomFromArray(lootslotIds),
                        pos,
                        false 
                    ));
                }
            } 
            else if (payload.signedDistance < -3)
            {
                if (prng.Next(10000) < 5)
                {
                    payload.entityData.Add(new BasicPrefabEntity
                    (
                        classId: SelectRandomFromArray(creatureslotIds),
                        pos,
                        false 
                    ));
                }
            }
            
            const int size = 160;
            if (voxel % size == Int3.one * (size / 2))
            {
                payload.entityData.Add(new WorldgenEntities.AtmoVolumeEntity(
                    "89422755-ec85-4d68-8005-a0b319346dcd", // WorldEntities/Atmosphere/JellyshroomCaves/Normal.prefab
                    pos,
                    Vector3.one * size
                ));
            }

            return payload;
        }

        public void Sprinkle(BuildBlueprint_TreeEverything build)
        {
            foreach (var spawn in largeObjectSpawns) {

                var payload = build.GetVoxelPayload(spawn);
                payload.entityData.Add(new WorldgenEntities.BasicPrefabEntity(
                    SelectRandomFromArray(bigshrooms),
                    spawn.ToVector3(),
                    Quaternion.identity,
                    Vector3.one * UnityEngine.Random.Range(4, 7),
                    false
                )  { cellLevel = 2 } );
            }

            foreach (var spawn in mediumObjectSpawns) {

                var payload = build.GetVoxelPayload(spawn);
                payload.entityData.Add(new WorldgenEntities.BasicPrefabEntity(
                    SelectRandomFromArray(bigshrooms),
                    spawn.ToVector3(),
                    Quaternion.identity,
                    Vector3.one * UnityEngine.Random.Range(.8f, 3),
                    false
                )  { cellLevel = 1 } );
            }

            foreach (var spawn in smallObjectSpawns) {

                var payload = build.GetVoxelPayload(spawn);
                payload.entityData.Add(new WorldgenEntities.BasicPrefabEntity(
                    "3e199d12-2d75-4c58-a819-d78beeb24e2c", // small shroomy
                    spawn.ToVector3(),
                    Quaternion.identity,
                    Vector3.one * UnityEngine.Random.Range(0.8f, 1.2f),
                    false
                ));
            }

            for (int i = 0; i < 10; i++) 
            {
                // add a random rock
                var voxelend = new Int2(dimensions.VoxelSize.x - 1, dimensions.VoxelSize.z - 1);
                var centerVoxel = RandomUtils.RandomInsideBounds(prng, Int2.zero, voxelend);
                var center = new Vector3(centerVoxel.x, heightmap[centerVoxel.y][centerVoxel.x], centerVoxel.y);

                // var sdf = new SignedDistanceBox ( center, a, b, c );
                var radius = prng.Next(4, 20);
                var sdf = new SignedDistanceNoise
                (
                    new SignedDistanceSphere(center, radius),
                    new FractalNoise(seed + 2, 3, Vector3.one * 0.02f, -15, 15, .6f, Vector3.one * 2.5f)
                );

                var bounds = new Int3.Bounds
                (
                    Int3.Floor(center - Vector3.one * radius),
                    Int3.Ceil(center + Vector3.one * radius)
                );

                bounds.mins = Int3.Max(bounds.mins, Int3.zero);
                bounds.maxs = Int3.Min(bounds.maxs, dimensions.VoxelSize - 1);

                build.ApplySDF(bounds, sdf, 191);
            }
        }

        private static IEnumerable<T> EnumerateMap<T> (T[][] map, int outerSize, int innerSize)
        {
            for (int y = 0; y < outerSize; y++) 
            {
                for (int x = 0; x < innerSize; x++)
                {
                    yield return map[y][x];
                }
            }
        }
        private T SelectRandomFromArray<T> (T[] array) => array[prng.Next(array.Length)];

        private int SqrDistance2(int a_x, int a_y, int b_x, int b_y)
        {
            return (a_x - b_x) * (a_x - b_x) + (a_y - b_y) * (a_y - b_y);
        }
    }
}