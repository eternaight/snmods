using System;
using System.Collections.Generic;
using UnityEngine;

namespace NewPlugin.WorldgenAPI 
{
    public class SerializedEntityData 
    {
        public string classId;
        public bool overridePrefab = false;
        public bool createEmpty = false;

        public readonly Queue<Action<GameObject>> objectMods = new();

        public static SerializedEntityData BatchEntityRoot(Vector3 localPosition) 
        {
            var data = new SerializedEntityData
            {
                classId = "94a577fe-b9bc-4f37-a2d4-24a59b0bba2d" // batch root
            };
            data.SetPosition(localPosition);

            return data;
        }

        public static SerializedEntityData CellRoot(Vector3 localPosition)
        {
            var data = new SerializedEntityData
            {
                classId = "55d7ab35-de97-4d95-af6c-ac8d03bb54ca" // cell root
            };
            data.SetPosition(localPosition);

            return data;
        }

        public void SetPosition(Vector3 localPosition)
        {
            void setPosition(GameObject go) { go.transform.localPosition = localPosition; }
            objectMods.Enqueue(setPosition);
        }
        public void SetTransform(Vector3 localPosition, Quaternion rotation, Vector3 scale)
        {
            void updateTransform(GameObject go) 
            { 
                go.transform.localPosition = localPosition; 
                go.transform.rotation = rotation; 
                go.transform.localScale = scale; 
            }
            objectMods.Enqueue(updateTransform);
        }
    }
}