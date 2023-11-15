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
                "safeShallows",         // 0
                "kelpForest",           // 1
                "grassyPlateaus",       // 2
                "underwaterIslands",    // 3
                "mushroomForest",       // 4
                "kooshZone",            // 5
                "grandReef",            // 6
                "arctic",               // 7
                "inactiveLavaZone",     // 8
                "unassigned",           // 9
                "crashZone",            // 10
                "void",                 // 11
                "sparseReef",
                "dunes",
                "bloodKelp",            // 14
                "mountains",
                "seaTreaderPath",
                "bloodKelpTwo",
                "CragField"             // 18
            };

            var biomemapSize = dimensions.SurfaceBiomemapSize;
            biomemap = new byte[biomemapSize.Product() + 2];
            for (int z = 0; z < biomemapSize.y; z++)
            {
                for (int x = 0; x < biomemapSize.x; x++)
                {
                    biomemap[x + z * biomemapSize.x] = 11;
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
            public readonly BuildEntityCell[][] batchCells;

            public BuildBakedBatch(OctNodeData[][] octNodeDatas, List<SerializedEntityData> batchEntities, BuildEntityCell[][] batchCells)
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

            public BuildEntityCell(Int3 localCellId, int level, UnityEngine.Vector3 rootPosition) {
                this.localCellId = localCellId;
                this.level = level;

                entityList = new List<SerializedEntityData>() {
                    SerializedEntityData.CellRoot(rootPosition)
                };
            }
        }
    }
}