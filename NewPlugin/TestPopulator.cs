using NewPlugin.WorldgenAPI;
using UnityEngine;

namespace NewPlugin
{
    public class TestPopulator
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

        public TestPopulator(BuildDimensions dims)
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

                if (stripeyGroundNoise > 0.5f && prng.Next(400) < 5)
                {
                    var prefabSet = stripeyGroundNoise > 0.8f ? creepvineSetDense : creepvineSetSparse;
                    var kelpEntity = new SerializedEntityData() {
                        classId = prefabSet[prng.Next(prefabSet.Length)],
                    };
                    kelpEntity.SetPosition(pos);
                    
                    payload.entityData.Add(kelpEntity);
                }
            }

            return payload;
        }
    }
}