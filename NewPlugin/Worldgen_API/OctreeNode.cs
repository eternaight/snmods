using System.Linq;

namespace NewPlugin.WorldgenAPI
{
    public class OctreeNode
    {
        public VoxelPayload payload;
        public OctreeNode[] children;

        public readonly Int3 origin;
        public readonly int size;

        public OctreeNode(Int3 origin, int size)
        {
            this.payload = null;
            this.origin = origin;
            this.size = size;
            children = null;
        }

        public bool HasChildren { get { return children != null; } }

        public void Subdivide()
        {
            for (int c = 0; c < 8; c++) 
            {
                children[c] = new OctreeNode(origin + childOffsets[c] * size / 2, size / 2);
            }
        }

        public void CollectPayloadsAndBecomeLeaf()
        {
            payload.CopyFrom(children[0].payload);

            for (int c = 1; c < 8; c++)
            {
                payload.entityData.AddRange(children[c].payload.entityData);
            }

            children = null;
        }

        public void AssumeDownsampledPayload()
        {
            if (children == null)
                return;

            var enumNearSD = children.Where(child => child.payload.IsNearSurface()).Select(child => child.payload.signedDistance);

            if (enumNearSD.Count() == 0)
            {
                payload = new VoxelPayload(children[0].payload.signedDistance, children[0].payload.SolidType);
            }
            else
            {
                var mostCommonChildType = children.Where(child => child.payload.IsNearSurface()).GroupBy(child => child.payload.SolidType).OrderByDescending(entry => entry.Count()).First().Key;
                payload = new VoxelPayload(enumNearSD.Average(), mostCommonChildType);
            }
        }

        private static readonly Int3[] childOffsets = 
        {
            new Int3(0, 0, 0),
            new Int3(0, 0, 1),
            new Int3(0, 1, 0),
            new Int3(0, 1, 1),
            new Int3(1, 0, 0),
            new Int3(1, 0, 1),
            new Int3(1, 1, 0),
            new Int3(1, 1, 1)
        };
    }
}