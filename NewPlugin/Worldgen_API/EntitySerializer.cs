using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace NewPlugin.WorldgenAPI
{
    public static class EntitySerializer
    {
        public static void WriteBatchEntities(List<SerializedEntityData> entityData, Stream stream) {
            
            if (entityData.Count == 0) return;
            var proto = ProtobufSerializerPool.GetProxy().Value;
            WriteEntities(proto, entityData, stream);
        }

        public static void WriteCellsCache(BuildBaked.BuildEntityCell[][] batchCells, Stream stream) {

            if (batchCells.Length == 0) return;

            var numCells = batchCells.Select(cells => cells != null ? cells.Select(cell => cell != null ? cell.entityList.Count : 0).Sum() : 0).Sum();

            if (numCells == 0) return;

            var proto = ProtobufSerializerPool.GetProxy().Value;
            proto.Serialize(stream, new CellManager.CellsFileHeader() {
                numCells = numCells,
                version = 10
            });

            var cellHeader = new CellManager.CellHeaderEx();
            foreach (var stratifiedCells in batchCells) 
            {
                if (stratifiedCells == null) continue;
                foreach (var cell in stratifiedCells)
                {
                    var serialData = new SerialData();
                    if (cell == null) continue;
                    if (cell.entityList.Count == 0) continue;

                    // insert cell root?
                    // var globalCellPosition = cell.originVoxel.ToVector3() + Plugin.config.voxelZero;
                    var entityDataCopy = new List<SerializedEntityData>(cell.entityList);
                    
                    using (var memstream = new ScratchMemoryStream())
                    {
                        WriteEntities(proto, entityDataCopy, memstream);
                        serialData.CopyFrom(memstream);
                    }

                    cellHeader.cellId = cell.localCellId;
                    cellHeader.level = cell.level;
                    cellHeader.allowSpawnRestrictions = false; // should it enforce spawn restrictions? see SpawnRestrictionEnforcer
                    cellHeader.dataLength = serialData.Length;
                    cellHeader.waiterDataLength = 0;
                    cellHeader.legacyDataLength = 0;
                    proto.Serialize(stream, cellHeader);
                    stream.Write(serialData.Data.Array, serialData.Data.Offset, serialData.Data.Length);
                }
            }
        }

        private static void WriteEntities(ProtobufSerializer proto, List<SerializedEntityData> entityData, Stream stream) { 

            proto.SerializeStreamHeader(stream);

            var parentGuid = ""; 
            var parentPosition = Vector3.zero;
            
            using var loopHeaderProxy = ProtobufSerializer.loopHeaderPool.GetProxy();
            
            var loopHeader = loopHeaderProxy.Value;
            loopHeader.Reset();
            loopHeader.Count = entityData.Count;
            proto.Serialize(stream, loopHeader);

            using var goDataProxy = ProtobufSerializer.gameObjectDataPool.GetProxy();
            using var componentHeaderProxy = ProtobufSerializer.componentHeaderPool.GetProxy();

            var goData = goDataProxy.Value;
            var componentHeader = componentHeaderProxy.Value;
            var componentList = new List<Component>();

            int i = 0;

            foreach (var entity in entityData)
            {
                var testGameObject = new GameObject("test_ignore");
                
                goData.Reset();
                goData.CreateEmptyObject = entity.createEmpty;
                goData.MergeObject = false;
                goData.IsActive = true;
                goData.Parent = parentGuid;
                goData.Tag = "Untagged";
                goData.Id = System.Guid.NewGuid().ToString();

                if (parentGuid.Length == 0) 
                    parentGuid = goData.Id;

                goData.ClassId = entity.classId;
                goData.OverridePrefab = entity.overridePrefab;

                proto.Serialize(stream, goData);
                
                foreach (var gameObjectMod in entity.objectMods)
                {
                    gameObjectMod(testGameObject);
                }

                testGameObject.GetComponents(componentList);
                // assuming everything should be serialized. whoops? 

                // components:
                loopHeader.Reset();
                loopHeader.Count = componentList.Count;
                proto.Serialize(stream, loopHeader);
                
                foreach (var comp in componentList)
                {
                    var type = comp.GetType();

                    componentHeader.Reset();
                    componentHeader.TypeName = type.FullName;
                    componentHeader.IsEnabled = false;
                    proto.Serialize(stream, componentHeader);

                    proto.Serialize(stream, comp, type);

                    if (!(type == typeof(Transform)))
                    {
                        Component.Destroy(comp);
                    }
                }

                componentList.Clear();
                i += 1;
                GameObject.Destroy(testGameObject);
            }
        } 

        private static void DeserializeCellEntities(ProtobufSerializer proto, Stream stream) 
        {
            if (!proto.TryDeserializeStreamHeader(stream))
            {
                Plugin.logSource.LogError("Couldn't deserialize stream header");
                return;
            }

            using var loopheaderProxy = ProtobufSerializer.loopHeaderPool.GetProxy();

            var loopHeader = loopheaderProxy.Value;
            loopHeader.Reset();
            proto.Deserialize(stream, loopheaderProxy, false);

            using var gameObjectDataProxy = ProtobufSerializer.gameObjectDataPool.GetProxy();
            using var componentHeaderProxy = ProtobufSerializer.componentHeaderPool.GetProxy();

            var deserializedEntityDatas = new List<SerializedEntityData>();

            for (var i = 0; i < loopHeader.Count; i++)
            {
                var goData = gameObjectDataProxy.Value;
                proto.Deserialize(stream, goData, false);

                var entdata = new SerializedEntityData();
                entdata.classId = goData.ClassId;
                entdata.createEmpty = goData.CreateEmptyObject;
                entdata.overridePrefab = goData.OverridePrefab;
                deserializedEntityDatas.Add(entdata);
            }
        }
    }
}