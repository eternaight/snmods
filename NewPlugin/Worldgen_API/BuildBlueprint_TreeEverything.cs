using System;
using System.Collections.Generic;
using System.Linq;
using NewPlugin.ProcGen;

namespace NewPlugin.WorldgenAPI
{
    public class BuildBlueprint_TreeEverything
    {
        private readonly BuildDimensions dimensions;
        public readonly Array3<BlueprintTree> trees;

        public BuildBlueprint_TreeEverything(BuildDimensions dimensions) 
        {
            this.dimensions = dimensions;
            trees = new Array3<BlueprintTree>(dimensions.octreeCounts.x, dimensions.octreeCounts.y, dimensions.octreeCounts.z);

            foreach (var globalTreeIndex in Int3.Range(dimensions.octreeCounts))
            {
                trees.Set(globalTreeIndex, new BlueprintTree(globalTreeIndex, dimensions));
            }
        }

        public BuildBaked Bake(UnityEngine.Vector3 voxelZeroGlobalPos) 
        {
            var bake = new BuildBaked(dimensions);

            foreach (var batchIndex in Int3.Range(dimensions.BatchCounts))
            {
                if (BakeBatch(batchIndex, voxelZeroGlobalPos, out var batch))
                {
                    bake.batches.Add(batchIndex, batch);
                }   
            }

            return bake;
        }

        private bool BakeBatch(Int3 batchIndex, UnityEngine.Vector3 voxelZeroGlobalPos, out BuildBaked.BuildBakedBatch batch)
        {
            var firstTree = batchIndex * dimensions.treesPerBatch;
            var octreeCountsInThisBatch = Int3.Min(dimensions.treesPerBatch, dimensions.octreeCounts - firstTree);
            var treeList = new List<OctNodeData[]>();

            // batch entities:
            var batchLocalPos = voxelZeroGlobalPos + (batchIndex * dimensions.BatchVoxelSize).ToVector3();
            var batchEntities = new List<SerializedEntityData>() {
                SerializedEntityData.BatchEntityRoot(batchLocalPos)
            };

            // cells:
            var batchCells = new Dictionary<Int3, BuildBaked.BuildEntityCell[]>();

            var voxelEmpty = true;
            
            foreach (var treeIndex in Int3.Range(octreeCountsInThisBatch))
            {
                var tree = trees.Get(firstTree + treeIndex);
                var treeData = tree.WriteToOctNodeDataList();
                voxelEmpty &= treeData.Count == 1 && (treeData[0].density + treeData[0].type) == 0;
                treeList.Add(treeData.ToArray());

                tree.AddTreeEntities(batchEntities, batchCells, voxelZeroGlobalPos);
            }

            // {
            //     // transform to local space batch entities
            //     void repositionToLocal(UnityEngine.GameObject go) => go.transform.position -= batchOrigin;
            //     foreach (var childEntity in batchEntities.Skip(1))
            //     {
            //         childEntity.objectMods.Enqueue(repositionToLocal);
            //     }
            // }

            // foreach (var entry in batchCells)
            // {
            //     // transform to local space cell entities
            //     void repositionToLocal(UnityEngine.GameObject go) => go.transform.position -= entry.Value.originVoxel + Plugin.config.voxelZero;
            //     foreach (var childEntity in entry.Value.entityList.Skip(1))
            //     {
            //         childEntity.objectMods.Enqueue(repositionToLocal);
            //     }
            // }

            if (voxelEmpty && batchEntities.Count == 0 && batchCells.Count == 0) 
            {
                batch = null;
                return false;
            }

            Plugin.logSource.LogInfo($"Baked {treeList.Count} trees for batch {batchIndex}");
            batch = new BuildBaked.BuildBakedBatch
            (
                treeList.ToArray(), 
                batchEntities.Count == 0 ? null : batchEntities, 
                batchCells.Count == 0 ? null : batchCells.Values.ToArray()
            );
            return true;
        }

        public VoxelPayload GetVoxelPayload(Int3 voxel)
        {
            var tree = voxel / dimensions.octreeSizeVoxels;
            return trees.Get(tree).GetVoxelPayload(voxel);
        }

        public void ApplySDF(Int3.Bounds bounds, ISignedDistance sd, int solidType)
        {
            var treeMin = bounds.mins / dimensions.octreeSizeVoxels;
            var treeMax = bounds.maxs / dimensions.octreeSizeVoxels;

            foreach (var treeIndex in new Int3.RangeEnumerator(treeMin, treeMax))
            {
                trees.Get(treeIndex).ApplySDF(sd, solidType);
            }
        }
    }
        
    // octree containing voxel & entity info
    public class BlueprintTree
    {
        public readonly OctreeNode root;
        public readonly Int3 globalTreeIndex;
        private readonly BuildDimensions dimensions;

        public BlueprintTree(Int3 globalTreeIndex, BuildDimensions dimensions)
        {
            this.globalTreeIndex = globalTreeIndex;
            this.dimensions = dimensions;
            root = new OctreeNode(globalTreeIndex * dimensions.octreeSizeVoxels, dimensions.octreeSizeVoxels);
        }


        public List<OctNodeData> WriteToOctNodeDataList() {
            var dataArray = new List<OctNodeData>()
            {
                root.payload.EncodeAsOctNodeData()
            };
            WriteChildrenToArray(root, dataArray, 0);

            return dataArray;
        }

        private static void WriteChildrenToArray(OctreeNode node, List<OctNodeData> dataarray, int myPos) {

            if (node.HasChildren) {

                // get new child index
                var newChildIndex = (ushort)dataarray.Count;
                dataarray[myPos].childPosition = newChildIndex;

                dataarray.AddRange(node.children.Select(child => child.payload.EncodeAsOctNodeData()));

                for (int i = 0; i < 8; i++) {
                    WriteChildrenToArray(node.children[i], dataarray, newChildIndex + i);
                }
            }
        }

        public void Populate(TreeFillingFunction tpf)
        {
            root.payload = tpf(globalTreeIndex * dimensions.octreeSizeVoxels + Int3.one * dimensions.octreeSizeVoxels / 2);
            PopulateChildrenRecursive(tpf, root);
        }

        private bool PopulateChildrenRecursive(TreeFillingFunction tpf, OctreeNode node)
        {
            if (node.size <= 1) {
                node.payload = tpf(node.origin);
                return true;
            }

            node.Subdivide();

            bool childrenAreLeafNodes = true;
            bool childrenDataIdentical = true;
            
            for (int b = 0; b < 8; b++) 
            {
                childrenAreLeafNodes &= PopulateChildrenRecursive(tpf, node.children[b]);
                
                // this blocktype check is really important for some reason
                childrenDataIdentical &= node.children[b].payload.Blocktype == node.children[0].payload.Blocktype;
                
                childrenDataIdentical &= node.children[b].payload.Density == node.children[0].payload.Density;
            }
            
            if (childrenDataIdentical & childrenAreLeafNodes) 
            {
                node.CollectPayloadsAndBecomeLeaf();
                return true;
            } 

            node.AssumeDownsampledPayload();
            return false;
        }

        public void AddTreeEntities(List<SerializedEntityData> batchEntities, Dictionary<Int3, BuildBaked.BuildEntityCell[]> batchCells,  UnityEngine.Vector3 voxelZero) 
        {
            AddNodeEntities(root, batchEntities, batchCells, voxelZero);
        }

        private void AddNodeEntities(OctreeNode parentNode, List<SerializedEntityData> batchEntities, Dictionary<Int3, BuildBaked.BuildEntityCell[]> batchCells,  UnityEngine.Vector3 voxelZero)
        {
            if (parentNode.HasChildren)
            {
                for (int c = 0; c < 8; c++)
                {
                    AddNodeEntities(parentNode.children[c], batchEntities, batchCells, voxelZero);
                }
            }
            else 
            {
                // add leaf's entity data to cell (only leaf nodes are supposed to have them)
                AddEntities(parentNode.payload.entityData, parentNode.origin, voxelZero, batchEntities, batchCells);
            }
        }

        private void AddEntities(IEnumerable<BuildEntity> entityEnumerable, Int3 voxel, UnityEngine.Vector3 voxelZero, List<SerializedEntityData> batchEntities, Dictionary<Int3, BuildBaked.BuildEntityCell[]> batchCells) 
        {
            var levelCount = dimensions.cellsPerBatchLevels.Length;
            var batchIndex = globalTreeIndex / dimensions.treesPerBatch;

            // for (int level = 0; level < levelCount; level++)
            // {
            //     var cellSize = dimensions.CellVoxelSize(level);
            //     var globalCellIndex = voxel / cellSize;
            //     var localCellIndex = globalCellIndex - batchIndex * dimensions.cellsPerBatchLevels[level];
                
            //     if (!batchCells.TryGetValue(localCellIndex, out cellLevels))
            //     {
            //         cellLevels[level] = new BuildBaked.BuildEntityCell(localCellIndex, level, (globalCellIndex * cellSize).ToVector3() + voxelZero);
            //         batchCells.Add(localCellIndex, cellLevels);
            //     }
            // }

            foreach (var ent in entityEnumerable)
            {
                if (ent.IsBatchEntity())
                {
                    var batchVoxel = (batchIndex * dimensions.BatchVoxelSize).ToVector3();
                    ent.AddToTree(batchVoxel, batchEntities);
                } 
                else
                {
                    var level = ent.GetCellLevel();
                    var globalCellIndex = voxel / dimensions.CellVoxelSize(level);
                    var localCellIndex = globalCellIndex - batchIndex * dimensions.cellsPerBatchLevels[level];
                    
                    if (!batchCells.TryGetValue(localCellIndex, out var cellLevels))
                    {
                        cellLevels = new BuildBaked.BuildEntityCell[levelCount];
                        batchCells.Add(localCellIndex, cellLevels);
                    }
                    if (cellLevels[level] == null)
                    {
                        cellLevels[level] = new BuildBaked.BuildEntityCell(localCellIndex, level, (globalCellIndex * dimensions.CellVoxelSize(level)).ToVector3() + voxelZero);
                    }
                    var cell = cellLevels[level];

                    var cellVoxel = (cell.localCellId * dimensions.CellVoxelSize(ent.GetCellLevel())).ToVector3();
                    ent.AddToTree(cellVoxel, cell.entityList);
                }
            }
        }

        internal VoxelPayload GetVoxelPayload(Int3 voxel)
        {
            return GetPayloadRecursive(voxel, root);
        }

        internal VoxelPayload GetPayloadRecursive(Int3 voxel, OctreeNode parentNode)
        {
            if (!parentNode.HasChildren)
            {
                return parentNode.payload;
            }

            for (int c = 0; c < 8; c++)
            {
                if (voxel.Within(parentNode.children[c].origin, parentNode.children[c].origin + Int3.one * parentNode.children[c].size))
                {
                    return GetPayloadRecursive(voxel, parentNode.children[c]);
                }
            }
            throw new System.Exception();
        }

        internal void ApplySDF(ISignedDistance sd, int solidType)
        {
            ApplySDFRecursive(root, sd, solidType);
        }

        internal static bool ApplySDFRecursive(OctreeNode node, ISignedDistance sd, int solidType)
        {
            if (node.size <= 1)
            {
                var newvalue = sd.Evaluate(node.origin.ToVector3());

                if (newvalue > node.payload.signedDistance)
                {
                    node.payload.SolidType = solidType;
                    node.payload.signedDistance = newvalue;
                }
                return true;
            }

            node.Subdivide();
            
            bool childrenAreLeafNodes = true;
            bool childrenDataIdentical = true;
            
            for (int b = 0; b < 8; b++) 
            {
                childrenAreLeafNodes &= ApplySDFRecursive(node.children[b], sd, solidType);
                
                // this blocktype check is really important for some reason
                childrenDataIdentical &= node.children[b].payload.Blocktype == node.children[0].payload.Blocktype;
                
                childrenDataIdentical &= node.children[b].payload.Density == node.children[0].payload.Density;
            }
            
            if (childrenDataIdentical & childrenAreLeafNodes) 
            {
                node.CollectPayloadsAndBecomeLeaf();
                return true;
            } 

            node.AssumeDownsampledPayload();
            return false;
        }
    }
}