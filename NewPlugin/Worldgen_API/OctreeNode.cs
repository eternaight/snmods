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
            this.payload = new VoxelPayload(float.NegativeInfinity, 1);
            this.origin = origin;
            this.size = size;
            children = null;
        }

        public bool HasChildren { get { return children != null; } }

        public void Subdivide()
        {
            if (!HasChildren) 
            {
                children = new OctreeNode[8];
                for (int c = 0; c < 8; c++) 
                {
                    children[c] = new OctreeNode(origin + childOffsets[c] * size / 2, size / 2)
                    {
                        payload = new VoxelPayload(payload.signedDistance, payload.SolidType)
                    };
                    children[c].payload.entityData.AddRange(payload.entityData.Where(ent => children[c].CoversVoxelPosition(ent.GetVoxelPosition())));
                }
            }

            payload.entityData.Clear();
        }

        public void CollectPayloadsAndBecomeLeaf()
        {
            payload = children[0].payload;

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

        public bool CoversVoxelPosition(UnityEngine.Vector3 voxelPosition)
        {
            return  voxelPosition.x >= origin.x && voxelPosition.y >= origin.y && voxelPosition.z >= origin.z && 
                    voxelPosition.x < origin.x + size && voxelPosition.y < origin.y + size && voxelPosition.z < origin.z + size;
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