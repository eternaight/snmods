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

        public static void WriteCellsCache(BuildBaked.BuildEntityCell[] batchCells, Stream stream) {

            if (batchCells.Length == 0) return;

            var numCells = batchCells.Select(cells => cells.entityList.Count).Sum();
            var proto = ProtobufSerializerPool.GetProxy().Value;

            if (numCells == 0) return;

            proto.Serialize(stream, new CellManager.CellsFileHeader() {
                numCells = numCells,
                version = 10
            });

            var cellHeader = new CellManager.CellHeaderEx();
            foreach (var cell in batchCells) 
            {
                var serialData = new SerialData();
                if (cell.entityList.Count == 0) continue;

                // insert cell root?
                var globalCellPosition = cell.originVoxel.ToVector3() + Plugin.config.voxelZero;
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

            var testGameObject = new GameObject("test_ignore");
            var goData = goDataProxy.Value;
            var componentHeader = componentHeaderProxy.Value;
            var componentList = new List<Component>();

            foreach (var entity in entityData)
            {
                goData.Reset();
                goData.CreateEmptyObject = false;
                goData.MergeObject = false;
                goData.IsActive = true;
                goData.Parent = parentGuid;
                goData.Id = System.Guid.NewGuid().ToString();

                if (parentGuid.Length == 0) 
                    parentGuid = goData.Id;

                goData.ClassId = entity.classId;
                goData.OverridePrefab = false;

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
                    proto.Serialize(stream, componentHeader);

                    proto.Serialize(stream, comp, type);

                    if (!(type == typeof(Transform)))
                        Component.Destroy(comp);
                }

                componentList.Clear();
            }

            GameObject.Destroy(testGameObject);
        } 
    }
}