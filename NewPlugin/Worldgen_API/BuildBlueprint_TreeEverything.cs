using System.Collections.Generic;
using System.Linq;

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

        public BuildBaked Bake() 
        {
            var bake = new BuildBaked(dimensions);

            foreach (var batchIndex in Int3.Range(dimensions.BatchCounts))
            {
                if (BakeBatch(batchIndex, out var batch))
                {
                    bake.batches.Add(batchIndex, batch);
                }   
            }

            return bake;
        }

        private bool BakeBatch(Int3 batchIndex, out BuildBaked.BuildBakedBatch batch)
        {
            var firstTree = batchIndex * dimensions.treesPerBatch;
            var octreeCountsInThisBatch = Int3.Min(dimensions.treesPerBatch, dimensions.octreeCounts - firstTree);
            var treeList = new List<OctNodeData[]>();

            // batch entities:
            var pos = Plugin.config.voxelZero + (batchIndex * dimensions.BatchVoxelSize).ToVector3();
            var batchEntities = new List<SerializedEntityData>() {
                SerializedEntityData.BatchEntityRoot(pos)
            };

            // cells:
            var batchCells = new Dictionary<Int3, BuildBaked.BuildEntityCell>();

            var voxelEmpty = true;
            
            foreach (var treeIndex in Int3.Range(octreeCountsInThisBatch))
            {
                var tree = trees.Get(firstTree + treeIndex);
                var treeData = tree.WriteToOctNodeDataList();
                voxelEmpty &= treeData.Count == 1 && (treeData[0].density + treeData[0].type) == 0;
                treeList.Add(treeData.ToArray());

                tree.AddTreeEntities(batchEntities, batchCells);
            }

            {
                // transform to local space batch entities
                void repositionToLocal(UnityEngine.GameObject go) => go.transform.position -= pos;
                foreach (var childEntity in batchEntities.Skip(1))
                {
                    childEntity.objectMods.Enqueue(repositionToLocal);
                }
            }

            foreach (var entry in batchCells)
            {
                // transform to local space cell entities
                void repositionToLocal(UnityEngine.GameObject go) => go.transform.position -= entry.Value.originVoxel + Plugin.config.voxelZero;
                foreach (var childEntity in entry.Value.entityList)
                {
                    childEntity.objectMods.Enqueue(repositionToLocal);
                }
            }

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
            return trees.Get(tree).GetVoxelPayload(voxel - tree * dimensions.octreeSizeVoxels);
        }
    }
        
    // octree containing voxel & entity info
    public class BlueprintTree
    {
        private readonly OctreeNode root;
        private readonly Int3 globalTreeIndex;
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

        public void Populate(TreePopulatingFunction tpf)
        {
            root.payload.CopyFrom(tpf(globalTreeIndex * dimensions.octreeSizeVoxels + Int3.one * dimensions.octreeSizeVoxels / 2));
            PopulateChildrenRecursive(tpf, root);
        }

        private bool PopulateChildrenRecursive(TreePopulatingFunction tpf, OctreeNode node)
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

        public void AddTreeEntities(List<SerializedEntityData> batchEntities, Dictionary<Int3, BuildBaked.BuildEntityCell> batchCells) 
        {
            AddNodeEntities(root, batchEntities, batchCells);
        }

        private void AddNodeEntities(OctreeNode parentNode, List<SerializedEntityData> batchEntities, Dictionary<Int3, BuildBaked.BuildEntityCell> batchCells)
        {
            if (parentNode.HasChildren)
            {
                for (int c = 0; c < 8; c++)
                {
                    AddNodeEntities(parentNode.children[c], batchEntities, batchCells);
                }
            }
            else 
            {
                // add leaf's entity data to cell (only leaf nodes are supposed to have them)
                AddEntities(parentNode.payload.entityData, parentNode.origin, batchEntities, batchCells);
            }
        }

        private void AddEntities(IEnumerable<BuildEntity> entityEnumerable, Int3 entityVoxel, List<SerializedEntityData> batchEntities, Dictionary<Int3, BuildBaked.BuildEntityCell> batchCells) 
        {
            var level = 1;

            var cellSize = dimensions.BatchVoxelSize / dimensions.cellsPerBatchLevels[level];
            var globalCellIndex = entityVoxel / cellSize;
            var batchIndex = globalTreeIndex / dimensions.treesPerBatch;
            var localCellIndex = globalCellIndex - batchIndex * dimensions.cellsPerBatchLevels[level];
            
            if (!batchCells.TryGetValue(localCellIndex, out var cell))
            {
                cell = new BuildBaked.BuildEntityCell(localCellIndex, level, batchIndex * cellSize);
                batchCells.Add(localCellIndex, cell);
            }

            foreach (var ent in entityEnumerable)
            {
                ent.AddToTree(ent.IsBatchEntity() ? batchEntities : cell.entityList);
            }
        }

        internal VoxelPayload GetVoxelPayload(Int3 localVoxel)
        {
            return GetPayloadRecursive(localVoxel, root);
        }

        internal VoxelPayload GetPayloadRecursive(Int3 localVoxel, OctreeNode parentNode)
        {
            if (parentNode.size <= 1)
            {
                return parentNode.payload;
            }

            for (int c = 0; c < 8; c++)
            {
                if (localVoxel.Within(parentNode.children[c].origin, parentNode.children[c].origin + Int3.one * parentNode.children[c].size))
                {
                    return GetPayloadRecursive(localVoxel, parentNode.children[c]);
                }
            }
            throw new System.Exception();
        }
    }
}