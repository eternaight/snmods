using System.Collections.Generic;
using UnityEngine;

namespace NewPlugin.WorldgenAPI
{
    public abstract class BuildEntity
    {
        public abstract bool IsBatchEntity();
        public abstract int GetCellLevel();
        public abstract void AddToTree(Vector3 parentVoxel, List<SerializedEntityData> entList);
        public abstract Vector3 GetVoxelPosition();
    }
}