using System;
using System.Collections.Generic;
using NewPlugin;
using NewPlugin.ProcGen;
using NewPlugin.WorldgenAPI;
using NewPlugin.WorldgenEntities;
using UnityEngine;

namespace WarperTravel
{
    /*
        Idea for this area:
        Lots of floating islands
        Central island has a cove tree
        Central island sprouts roots towards other islands
        Other islands contain portals to different places
        Area is patrolled by ghosties outside following a circle that go hostile if you try to go out of bounds
    */

    public class WarperSubspacePopulator : IPopulator
    {
        private readonly Array3<TreeFillingFunction> octreeFillers;
        private readonly System.Random prng;
        public WarperSubspacePopulator(int seed, BuildDimensions dimensions)
        {
            prng = new System.Random(seed);
            octreeFillers = new Array3<TreeFillingFunction>(dimensions.octreeCounts.x, dimensions.octreeCounts.y, dimensions.octreeCounts.z);
            var chamber = new MainChamberFiller(seed, dimensions, Plugin.config.subspaceBatch, GenerateRandomRoot);
            chamber.SubscribeToOctrees(octreeFillers);
        }

        public VoxelPayload Populate(Int3 voxel) // (deprecated kinda)
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

            // fishes
            var voxelspace_pos = Quaternion.Euler(0, 15, 0) * Vector3.forward * 50 + new Vector3(80, 80, 80);
            build.GetVoxelPayload(Int3.Floor(voxelspace_pos)).entityData.Add(new BasicPrefabEntity(
                "ce23b9ee-fd98-4677-9919-20248356f7cf", voxelspace_pos, false
            ));
            voxelspace_pos = Quaternion.Euler(0, 135, 0) * Vector3.forward * 50 + new Vector3(80, 80, 80);
            build.GetVoxelPayload(Int3.Floor(voxelspace_pos)).entityData.Add(new BasicPrefabEntity(
                "8ffbb5b5-21b4-4687-9118-730d59330c9a", voxelspace_pos, false
            ));
            voxelspace_pos = Quaternion.Euler(0, 255, 0) * Vector3.forward * 50 + new Vector3(80, 80, 80);
            build.GetVoxelPayload(Int3.Floor(voxelspace_pos)).entityData.Add(new BasicPrefabEntity(
                "2d3ea578-e4fa-4246-8bc9-ed8e66dec781", voxelspace_pos, false
            ));

            // ghosties
            for (int i = 0; i < 2; i++)
            {
                voxelspace_pos = new Vector3(80, 120, 80) + Quaternion.Euler(0, i * 180, 0) * new Vector3(0, 0, 50);
                build.GetVoxelPayload(Int3.Floor(voxelspace_pos)).entityData.Add(new BasicPrefabEntity(
                    "5ea36b37-300f-4f01-96fa-003ae47c61e5", voxelspace_pos, false
                ));
            }

            voxelspace_pos = (Plugin.config.subspaceBatch.ToVector3() + Vector3.one * 0.5f) * 160;
            var subspace_atmo = new AtmoVolumeEntity(Plugin.config.subspaceVolumeClassID, voxelspace_pos, Vector3.one * 160);
            build.GetVoxelPayload(Int3.Floor(voxelspace_pos)).entityData.Add(subspace_atmo);
        }

        private ISignedDistance GenerateRandomRoot(Vector3 from, Vector3 to, float r_from, float r_to)
        {
            var dist = new SignedDistanceUnion();
            const float segments_per_voxel = 1 / 10f;
            int num_nodes = Mathf.CeilToInt(2 + Vector3.Distance(from, to) * segments_per_voxel);

            var node_positions = new Vector3[num_nodes];
            node_positions[0] = from;
            node_positions[num_nodes - 1] = to;
            var node_radii = new float[num_nodes];
            // generate node positions

            var rotation = Quaternion.Euler(Mathf.Lerp(-90, 30, (float)prng.NextDouble()), Mathf.Lerp(-90, 90, (float)prng.NextDouble()), 0);
            var initial_direction = rotation * (to - from).normalized;

            for (int n = 1; n < num_nodes - 1; n++) {
                var random_direction = initial_direction / segments_per_voxel;
                var to_direction = node_positions[num_nodes - 1] - node_positions[n - 1];
                var offset = Vector3.Lerp(random_direction, to_direction, Mathf.InverseLerp(1, num_nodes - 2, n));
                node_positions[n] = node_positions[n - 1] + offset;
            }

            for (int n = 0; n < num_nodes; n++) node_radii[n] = Mathf.Lerp(r_from, r_to, Mathf.InverseLerp(0, num_nodes - 1, n));

            for (int s = 0; s < num_nodes; s++)
            {
                dist.members.Add(new SignedDistanceSphere(node_positions[s], node_radii[s]));
                if (s > 0) dist.members.Add(new SignedDistanceCappedCone(node_positions[s - 1], node_positions[s], node_radii[s - 1], node_radii[s]));
            }

            return dist;
        }

        private class MainChamberFiller
        {
            private readonly HashSet<Int3> octrees_of_interest = new();

            private Int3 tree_voxel;

            private readonly HashSet<Int3> portalVoxels = new();
            private readonly List<ISignedDistance> densities = new();
            private readonly List<int> blocktypes = new();

            public MainChamberFiller(int seed, BuildDimensions dims, Int3 main_chamber_batch_id, Func<Vector3, Vector3, float, float, ISignedDistance> rootgen)
            {
                UnityEngine.Random.InitState(seed);

                var fractal_noise = new FractalNoise(
                    seed, 3, Vector3.one / dims.VoxelSize.x * 30, -5, 5, 0.4f, Vector3.one * 2.5f
                );

                var central_island_voxel = main_chamber_batch_id * dims.BatchVoxelSize + dims.BatchVoxelSize.ToVector3() * 0.5f;
                var central_island_size = 16;
                tree_voxel = Int3.Floor(central_island_voxel + Vector3.up * central_island_size * 0.85f);
                AddIsland(central_island_voxel, central_island_size, fractal_noise);

                var other_island_count = 7;
                var island_ring_radius = 64;
                var other_island_size = 9;
                for (int i = 0; i < other_island_count; i++)
                {
                    var direction = Quaternion.Euler(0, 360 * Mathf.InverseLerp(0, other_island_count, i), 0) * Vector3.forward;
                    var island_center = central_island_voxel + direction * island_ring_radius + Vector3.down * (UnityEngine.Random.value - 0.5f) * 20;
                    AddIsland(island_center, other_island_size, fractal_noise);

                    // add portal above the island
                    portalVoxels.Add(Int3.Floor(island_center + Vector3.up * other_island_size * 1.2f));
                    
                    var root_start = central_island_voxel + direction * 3 + Vector3.down * 3;
                    var root_end = Quaternion.Euler((UnityEngine.Random.value - 0.5f) * 180, 0, 0) * (root_start - island_center).normalized * other_island_size + island_center;
                    AddRoot(root_start, root_end, 3, 0.25f, rootgen);
                }

                var misc_root_count = 7;
                for (int i = 0; i < misc_root_count; i++)
                {
                    var end_pos = central_island_voxel + Vector3.down * 50;
                    end_pos += Quaternion.Euler(0, 360 * i / (float)misc_root_count, 0) * Vector3.forward * 35;
                    AddRoot(central_island_voxel, end_pos, 3, 1f, rootgen);
                }

                foreach (var index in Int3.Range(main_chamber_batch_id * dims.treesPerBatch, (main_chamber_batch_id + 1) * dims.treesPerBatch - 1))
                {
                    octrees_of_interest.Add(index);
                }
            }

            private void AddIsland(Vector3 center, float size, FractalNoise noise)
            {
                var island_base = new SignedDistanceUnion();
                island_base.members.Add(new SignedDistanceSphere(center, size));
                island_base.members.Add(new SignedDistanceCappedCone(center, center + Vector3.down * size * 1.7f, size, 3));
                densities.Add(new SignedDistanceNoise(island_base, noise));
                blocktypes.Add(145); // LR_Canyon_MiddleToRock
            }

            private void AddRoot(Vector3 a, Vector3 b, float r_a, float r_b, Func<Vector3, Vector3, float, float, ISignedDistance> rootgen)
            {
                densities.Add(rootgen(a, b, r_a, r_b));
                blocktypes.Add(147); //LR_Canyon_Bottom
            }

            public VoxelPayload Fill(Int3 voxel)
            {
                var sd = float.NegativeInfinity;
                var type = 1;

                var pos = voxel.ToVector3();
                for (int i = 0; i < densities.Count; i++)
                {
                    var distance = densities[i].Evaluate(pos);
                    if (distance > sd) {
                        sd = distance;
                        type = blocktypes[i];
                    }
                } 

                var payload = new VoxelPayload(sd, type);

                if (voxel == tree_voxel)
                {
                    var tree_entity = new BasicPrefabEntity("0e7cc3b9-cdf2-42d9-9c1f-c11b94277c19", voxel.ToVector3(), false)
                    {
                        scale = Vector3.one * 0.75f,
                        cellLevel = 2
                    };
                    payload.entityData.Add(tree_entity);
                    Plugin.logSource.LogInfo($"Found tree at voxel: {voxel}");
                }

                if (portalVoxels.Contains(voxel))
                {
                    var portal_entity = new BasicPrefabEntity(Plugin.config.subspaceGateClassID, voxel.ToVector3(), false) {
                        cellLevel = 0
                    };
                    payload.entityData.Add(portal_entity);
                    WarperSubspace.RegisterGate(voxel.ToVector3() + Plugin.config.voxelZero);
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