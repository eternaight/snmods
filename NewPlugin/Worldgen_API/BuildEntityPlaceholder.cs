using System.Collections.Generic;

namespace NewPlugin.WorldgenAPI
{
    public abstract class BuildEntity
    {
        public abstract bool IsBatchEntity();
        public abstract void AddToTree(List<SerializedEntityData> entList);
    }
}