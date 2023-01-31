using System.Collections.Generic;
using UnityEngine;

namespace NewPlugin.WorldgenAPI
{
    public class VoxelPayload
    {
        public float signedDistance;
        private int solidType;
        public int SolidType {
            get => solidType; 
            set
            {
                if (value <= 0) throw new System.ArgumentException();
                solidType = value;
            }
        }
        
        public readonly HashSet<BuildEntity> entityData = new();

        public byte Blocktype => (byte)((signedDistance > 0 || Density > 0) ? solidType : 0);
        public byte Density => OctNodeData.EncodeDensity(signedDistance);

        public VoxelPayload(float signedDistance, int solidType)
        {
            this.signedDistance = signedDistance;
            SolidType = solidType;
        }

        public bool IsNearSurface() => Mathf.Abs(signedDistance) < 1;
        public void CopyFrom(VoxelPayload another)
        {
            signedDistance = another.signedDistance;
            solidType = another.solidType;
            entityData.Clear();
            entityData.AddRange(another.entityData);
        }

        internal OctNodeData EncodeAsOctNodeData()
        {
            return new NewPlugin.OctNodeData(
                Blocktype,
                Density
            );
        }
    }
    
    public delegate VoxelPayload TreePopulatingFunction(Int3 voxel);
}