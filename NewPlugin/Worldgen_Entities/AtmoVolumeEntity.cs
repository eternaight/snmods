using System.Collections.Generic;
using NewPlugin.WorldgenAPI;
using UnityEngine;

namespace NewPlugin.WorldgenEntities
{
    public class AtmoVolumeEntity : BuildEntity
    {
        private readonly string classId;
        private readonly Vector3 voxelPosition;
        private readonly Vector3 size;

        public AtmoVolumeEntity(string classId, Vector3 globalPosition, Vector3 size)
        {
            this.classId = classId;
            this.voxelPosition = globalPosition;
            this.size = size;
        }

        public override void AddToTree(Vector3 parentVoxel, List<SerializedEntityData> entList)
        {
            var data = new SerializedEntityData() {
                classId = classId,
            };

            data.SetPosition(voxelPosition - parentVoxel);
            void ModCollider(GameObject obj) {
                var coll = obj.GetComponent<BoxCollider>() ?? obj.AddComponent<BoxCollider>();
                coll.isTrigger = true;
                coll.size = size;
            }
            data.objectMods.Enqueue(ModCollider);
            
            entList.Add(data);
        }

        public override int GetCellLevel() => 0;

        public override Vector3 GetVoxelPosition() => voxelPosition;

        public override bool IsBatchEntity() => true;
    }
}