namespace NewPlugin.WorldgenAPI
{
    public readonly struct BuildDimensions
    {
        public readonly int octreeSizeVoxels;
        public readonly Int3 octreeCounts;
        public readonly Int3 treesPerBatch;
        public readonly Int3[] cellsPerBatchLevels;
        public readonly int biomemapDownsampleFactor;

        public Int3 VoxelSize => octreeSizeVoxels * octreeCounts; 
        public Int3 BatchVoxelSize => octreeSizeVoxels * treesPerBatch;
        public Int3 BatchCounts => octreeCounts.CeilDiv(treesPerBatch);
        public Int3 CellVoxelSize(int level) => BatchVoxelSize / cellsPerBatchLevels[level];
        public Int3 CellCounts(int level) => cellsPerBatchLevels[level] * BatchCounts; 
        public Int2 SurfaceBiomemapSize => VoxelSize.xz / biomemapDownsampleFactor;

        public BuildDimensions(int octreeSizeVoxels, Int3 octreeCounts, Int3 treesPerBatch, Int3[] cellsPerBatchLevels, int biomemapDownsampleFactor) 
        {
            this.octreeSizeVoxels = octreeSizeVoxels;
            this.octreeCounts = octreeCounts;
            this.treesPerBatch = treesPerBatch;
            this.cellsPerBatchLevels = cellsPerBatchLevels;
            this.biomemapDownsampleFactor = biomemapDownsampleFactor;

            if (SurfaceBiomemapSize.x % 64 != 0 || SurfaceBiomemapSize.y % 64 != 0) {
                throw new System.ArgumentException("Unable to create BuildDimensions object: biomemap size dimensions aren't divisible by 64 (game requirement)");
            }
        }
    }
}