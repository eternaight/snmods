namespace NewPlugin
{
    public interface IPopulator
    {
        public WorldgenAPI.VoxelPayload Populate(Int3 voxel);
        public void Sprinkle(WorldgenAPI.BuildBlueprint_TreeEverything build);
    }
}