using System.Collections.Generic;
using NewPlugin.WorldgenAPI;
using UnityEngine;

namespace NewPlugin.WorldgenEntities
{
    public class BasicPrefabEntity : BuildEntity
    {
        private readonly string classId;
        private readonly Vector3 globalPosition;

        public BasicPrefabEntity(string classId, Vector3 globalPosition)
        {
            this.classId = classId;
            this.globalPosition = globalPosition;
        }

        public override void AddToTree(List<SerializedEntityData> entList)
        {
            var entdata = new SerializedEntityData();
            
            entdata.classId = classId;
            entdata.SetPosition(globalPosition);
            
            entList.Add(entdata);
        }

        public override bool IsBatchEntity()
        {
            throw new System.NotImplementedException();
        }
    }
}