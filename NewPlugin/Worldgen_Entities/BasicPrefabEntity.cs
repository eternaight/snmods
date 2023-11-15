using System.Collections.Generic;
using NewPlugin.WorldgenAPI;
using UnityEngine;

namespace NewPlugin.WorldgenEntities
{
    public class BasicPrefabEntity : BuildEntity
    {
        public string classId;
        public Vector3 voxelPosition;
        public Quaternion globalRotation;
        public Vector3 scale;
        public bool isBatchEntity;
        public int cellLevel;

        public BasicPrefabEntity(string classId, Vector3 voxelPosition, bool isBatchEntity)
        {
            this.classId = classId;
            this.voxelPosition = voxelPosition;
            this.globalRotation = Quaternion.identity;
            this.scale = Vector3.one;
            this.isBatchEntity = isBatchEntity;
        }
        public BasicPrefabEntity(string classId, Vector3 voxelPosition, Quaternion globalRotation, Vector3 scale, bool isBatchEntity)
        {
            this.classId = classId;
            this.voxelPosition = voxelPosition;
            this.globalRotation = globalRotation;
            this.scale = scale;
            this.isBatchEntity = isBatchEntity;
        }

        public override void AddToTree(Vector3 parentVoxel, List<SerializedEntityData> entList)
        {
            var entdata = new SerializedEntityData
            {
                classId = classId
            };
            
            entdata.SetTransform(voxelPosition - parentVoxel, globalRotation, scale);
            
            entList.Add(entdata);
        }

        public override int GetCellLevel() => cellLevel;

        public override Vector3 GetVoxelPosition() => voxelPosition;

        public override bool IsBatchEntity() => isBatchEntity;
    }
}