using NewPlugin.WorldgenAPI;
using NewPlugin.WorldgenEntities;
using UnityEngine;

namespace NewPlugin
{
    public class KelpForestPopulator
    {
        private readonly float[][] heightmap;
        private readonly System.Random prng;
        private readonly NewPlugin.ProcGen.OpenSimplexNoise noise;

        private readonly string[] creepvineSetSparse = {
            "1fd4d86f-3b06-4369-945c-ca65f50b4800", // young 1
            "de0e28a2-7a17-4254-b520-5f0e28355059", // young 4
            "a17ef178-6952-4a91-8f66-44e1d8ca0575", // 02 short
            "77a95f14-434e-46bd-8fbb-0a7c591849c3", // None
        };
        private readonly string[] creepvineSetDense = {
            "ee1baf03-0560-4f4d-ad29-13a337bef0d7", // dense 1
            "9bfe02bd-60a3-401b-b7a0-627c3bdc4451", // dense 3
            "de972f1f-daab-41d6-b274-5173b0dd23d8", // 01 long (seeds)
            "7329db6b-7385-4e77-8afa-71830ead9350", // 01 mid (seeds)
        };

        private readonly string[] creatureSlots = {
            "15a6954f-f89b-49b1-a2d7-ac566c6b703e",
            "26a0da27-53a7-4191-82e0-3e2f650a015d",
            "56898c2c-6d9e-459d-ba18-ca7f74c43fbf",
            "12458be9-f541-41e0-94b6-bcc5f077f171",
            "7842e723-43a0-4579-9d0d-ce4de2bf79d9",
            "274964eb-b8b0-41f8-947f-f493b342736d",
            "f4b5674b-8d19-4fc0-90f1-363c1769e745",
            "882a9d32-ef0a-40b2-a636-835ff4dfef6f",
            "c0a5e85b-23cb-4e2a-b90e-85df780717cc",
            "1862748b-ebc8-4b00-8284-1b7763bee766",
            "2123161e-09bf-4256-bae0-c17bd2cb3a2b",
            "06882740-cf0b-415e-ae9c-b8346cea00c8",
            "dd7ac9b2-20ff-4282-8d27-fe22e124cd44",
            "a8639442-471c-4958-9f53-b1c6a7acab6e",
            "3649ee5a-9c6c-4581-9d3d-94de1403de4f",
            "5f96da0b-1e62-4cef-a92f-6127a2f30875",
        };

        public KelpForestPopulator(BuildDimensions dims)
        {
            heightmap = new float[dims.VoxelSize.z][];

            noise = new NewPlugin.ProcGen.OpenSimplexNoise("Subnatica".GetHashCode());
            prng = new System.Random();

            for (int z = 0; z < heightmap.Length; z++)
            {
                heightmap[z] = new float[dims.VoxelSize.x];
                for (int x = 0; x < heightmap[z].Length; x++)
                {
                    var pos = new Vector2(x + 0.5f, z + 0.5f);

                    const double persistense = 0.6d;
                    const float lacunarity_x = 1.6f;
                    const float lacunarity_y = 1.6f;
                    var amp = 1d;
                    var scale = new Vector2(0.03f, 0.04f);
                    var amp_sum = 0d;

                    var stripeyGroundNoise = 0d;
                    for (int octave = 0; octave < 4; octave++)
                    {
                        stripeyGroundNoise += amp * noise.Evaluate(pos.x * scale.x, pos.y * scale.y);
                        amp_sum += amp;
                        scale.x *= lacunarity_x;
                        scale.y *= lacunarity_y;
                        amp *= persistense;
                    }
                    stripeyGroundNoise /= amp_sum;
                    heightmap[z][x] = (float)(stripeyGroundNoise * 10) + 10;
                }
            }
        }

        public VoxelPayload PopulateVoxel(Int3 voxel)
        {
            var height = heightmap[voxel.z][voxel.x];
            var pos = voxel.ToVector3() + Vector3.one * 0.5f;
            var payload = new VoxelPayload(
                signedDistance: height - pos.y,
                solidType: 2
            );


            if (payload.IsNearSurface())
            {
                const double persistense = 0.5d;
                const float lacunarity_x = 1.45f;
                const float lacunarity_y = 3.2f;
                var amp = 1d;
                var scale = new Vector2(0.04f, 0.05f);
                var amp_sum = 0d;

                var stripeyGroundNoise = 0d;
                for (int octave = 0; octave < 4; octave++)
                {
                    stripeyGroundNoise += amp * 0.5 * (noise.Evaluate(pos.x * scale.x, pos.z * scale.y) + 1);
                    amp_sum += amp;
                    scale.x *= lacunarity_x;
                    scale.y *= lacunarity_y;
                    amp *= persistense;
                }
                stripeyGroundNoise /= amp_sum;

                payload.SolidType = stripeyGroundNoise > 0.35f ? 110 : 2;

                var lootPrefab = "f2107564-5928-4ce5-82fb-1f58b8d98ec3"; //KelpForest_Loot_Sand

                if (stripeyGroundNoise > 0.5f)
                {
                    if (prng.Next(400) < 5)
                    {
                        var prefabSet = stripeyGroundNoise > 0.8f ? creepvineSetDense : creepvineSetSparse;
                        var kelpEntity = new BasicPrefabEntity(prefabSet[prng.Next(prefabSet.Length)], pos, false);
                        payload.entityData.Add(kelpEntity);

                        lootPrefab = "c0e771ef-15f8-435f-8dd6-aab35a03478c"; //KelpForest_Loot_VineBase
                    } 
                    else
                    {
                        lootPrefab = "c6a57a6d-de07-47e8-8ee2-7fb6e7dd3686";
                    } 
                }

                
                if (prng.Next(500) < 5)
                {
                    payload.entityData.Add(new BasicPrefabEntity
                    (
                        lootPrefab, 
                        pos,
                        false
                    ));
                }

            } else 
            {
                if (payload.signedDistance < -2 && payload.signedDistance > -12)
                {
                    // attempt open water spawn
                    if (prng.Next(5000) < 5)
                    {
                        var slotClassId = SelectRandomFromArray(creatureSlots);
                        payload.entityData.Add(new BasicPrefabEntity
                        (
                            slotClassId,
                            pos,
                            false
                        ));
                    }
                }
            }

            return payload;
        }
        

        private T SelectRandomFromArray<T> (T[] array) => array[prng.Next(array.Length)];
    }
}