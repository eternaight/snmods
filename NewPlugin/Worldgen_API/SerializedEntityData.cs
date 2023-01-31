using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewPlugin.WorldgenAPI 
{
    public class SerializedEntityData 
    {
        public string classId;
        public bool isBatchEntity;
        public readonly Queue<Action<GameObject>> objectMods = new();

        public static SerializedEntityData BatchEntityRoot(Vector3 globalPosition) 
        {
            var data = new SerializedEntityData
            {
                classId = "94a577fe-b9bc-4f37-a2d4-24a59b0bba2d" // batch root
            };
            data.SetPosition(globalPosition);

            return data;
        }

        public static SerializedEntityData CellRoot(Vector3 globalPosition)
        {
            var data = new SerializedEntityData
            {
                classId = "55d7ab35-de97-4d95-af6c-ac8d03bb54ca" // cell root
            };
            data.SetPosition(globalPosition);

            return data;
        }

        internal bool IsBatchEntity()
        {
            return isBatchEntity;
        }

        public void SetPosition(Vector3 globalPosition)
        {
            void setPosition(GameObject go) { go.transform.position = globalPosition; }
            objectMods.Enqueue(setPosition);
        }
    }
}