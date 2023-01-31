using System.Collections.Generic;

namespace NewPlugin.WorldgenAPI
{
    public class BuildBaked
    {
        public readonly BuildDimensions dimensions;
        public readonly Dictionary<Int3, BuildBakedBatch> batches = new();

        public readonly byte[] biomemap;
        public readonly string[] biomenames;

        public BuildBaked(BuildDimensions dimensions) 
        {
            this.dimensions = dimensions;

            biomenames = new string[] {
                "safeShallows",
                "kelpForest",
                "grassyPlateaus",
                "underwaterIslands",
                "mushroomForest",
                "kooshZone",
                "grandReef",
                "arctic",
                "inactiveLavaZone",
                "unassigned",
                "crashZone",
                "void",
                "sparseReef",
                "dunes",
                "bloodKelp",
                "mountains",
                "seaTreaderPath",
                "bloodKelpTwo",
                "CragField"
            };

            var biomemapSize = dimensions.SurfaceBiomemapSize;
            biomemap = new byte[biomemapSize.Product() + 2];
            for (int z = 0; z < biomemapSize.y; z++)
            {
                for (int x = 0; x < biomemapSize.x; x++)
                {
                    biomemap[x + z * biomemapSize.x] = 1;
                }
            }
            biomemap[biomemap.Length - 2] = (byte)(biomemapSize.x / 64);
            biomemap[biomemap.Length - 1] = (byte)(biomemapSize.y / 64);
        }

        public class BuildBakedBatch
        {
            // voxel data
            public readonly OctNodeData[][] octNodeDatas;
            // batch objects array
            public readonly List<SerializedEntityData> batchEntities;
            // cell data
            public readonly BuildEntityCell[] batchCells;

            public BuildBakedBatch(OctNodeData[][] octNodeDatas, List<SerializedEntityData> batchEntities, BuildEntityCell[] batchCells)
            {
                this.octNodeDatas = octNodeDatas;
                this.batchEntities = batchEntities;
                this.batchCells = batchCells;
            }
        }

        public class BuildEntityCell
        {
            public readonly List<SerializedEntityData> entityList;
            public readonly Int3 localCellId;
            public readonly int level;
            public readonly Int3 originVoxel;

            public BuildEntityCell(Int3 localCellId, int level, Int3 originVoxel) {
                this.localCellId = localCellId;
                this.level = level;
                this.originVoxel = originVoxel;

                entityList = new List<SerializedEntityData>() {
                    SerializedEntityData.CellRoot(originVoxel.ToVector3() + Plugin.config.voxelZero)
                };
            }
        }
    }
}