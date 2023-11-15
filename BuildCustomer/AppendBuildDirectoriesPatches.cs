using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using WorldStreaming;

namespace BuildCustomer
{
    public static class AppendBuildDirectoriesPatches
    {
        [HarmonyPatch(typeof(BatchOctrees))]
        public static class BatchOctrees_Patch
        {
            [HarmonyPatch(nameof(BatchOctrees.LoadOctrees))]
            [HarmonyTranspiler]
            public static IEnumerable<CodeInstruction> LoadOctrees_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var file_exists_check = typeof(System.IO.File).GetMethod(nameof(System.IO.File.Exists));
                var alternative_batch_path_get = typeof(BatchOctrees_Patch).GetMethod(nameof(GetAlternativeBatchPath));
                var process_next_branch = false;
                var success = false;
                foreach (var instruction in instructions)
                {
                    if (instruction.Calls(file_exists_check))
                    {
                        process_next_branch = true;
                    }
                    if (process_next_branch && instruction.Branches(out Label? label))
                    {
                        // insert identical branch
                        yield return instruction;
                        // assign new path
                        yield return new CodeInstruction(OpCodes.Ldarg_0); // load this
                        yield return new CodeInstruction(OpCodes.Call, typeof(BatchOctrees).GetMethod("get_id", AccessTools.all)); // put batchid onto stack
                        yield return new CodeInstruction(OpCodes.Call, alternative_batch_path_get); // convert into new path
                        yield return new CodeInstruction(OpCodes.Stloc_0); // store path
                        // repeat path exists check
                        yield return new CodeInstruction(OpCodes.Ldloc_0); // load path
                        yield return new CodeInstruction(OpCodes.Call, file_exists_check);

                        // (outside this if statement) the original branch is yielded

                        process_next_branch = false;
                        success = true;
                    }

                    yield return instruction;
                }

                if (!success)
                {
                    Plugin.logSource.LogError("LoadOctrees_Transpiler - failed to do a thing idk im tired");
                }
            }

            public static string GetAlternativeBatchPath(Int3 batchId) {
                var id = batchId;
                return Plugin.FirstFolderWithFile(string.Format("Build/CompiledOctreesCache/compiled-batch-{0}-{1}-{2}.optoctrees", id.x, id.y, id.z));
            }
        }

        [HarmonyPatch(typeof(LargeWorldStreamer))]
        public static class LargeWorldStreamer_Patch
        {
            [HarmonyPatch(nameof(LargeWorldStreamer.LoadBatchObjectsThreaded))]
            [HarmonyTranspiler] [HarmonyDebug]
            public static IEnumerable<CodeInstruction> LoadBatchObjectsThreaded_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                
                var target = typeof(FileUtils).GetMethod(nameof(FileUtils.FileExists), AccessTools.all);

                var instruction_list = new List<CodeInstruction>(instructions);
                var target_index = instruction_list.FindIndex(inst => inst.Calls(target));
                if (target_index == -1) Plugin.logSource.LogError("LoadBatchObjectsThreaded_Transpiler: target not found");
                var branch_index = instruction_list.FindIndex(target_index, inst => inst.Branches(out var label));
                if (branch_index == -1) Plugin.logSource.LogError("LoadBatchObjectsThreaded_Transpiler: branch not found");
                
                // if it's true, branch to a nop past the first branch
                var nop = new CodeInstruction(OpCodes.Nop);
                var past_first_check_nop_label = generator.DefineLabel(); 
                if (past_first_check_nop_label == null) Plugin.logSource.LogError("LoadBatchObjectsThreaded_Transpiler: label is null");
                nop.labels.Add(past_first_check_nop_label);
                instruction_list.Insert(branch_index + 1, nop);

                // add instructions: does my file exist?
                instruction_list.InsertRange(branch_index, new CodeInstruction[] {
                    new CodeInstruction(OpCodes.Brtrue_S, past_first_check_nop_label),
                    new CodeInstruction(OpCodes.Ldarg_1), // push batch index
                    new CodeInstruction(OpCodes.Call, typeof(LargeWorldStreamer_Patch).GetMethod(nameof(GetAlternativeBatchObjectsPath), AccessTools.all)),
                    new CodeInstruction(OpCodes.Stloc_1),
                    new CodeInstruction(OpCodes.Ldloc_1),
                    new CodeInstruction(OpCodes.Ldc_I4_0), // push an evil hidden parameter of false
                    new CodeInstruction(OpCodes.Call, target)
                });

                // if even this fails, it branches to an exit state
                // else it loads the data

                return instruction_list;
            }

            private static string GetAlternativeBatchObjectsPath(Int3 batchId) {
                var id = batchId;
                return Plugin.FirstFolderWithFile(string.Format("Build/BatchObjectsCache/batch-objects-{0}-{1}-{2}.bin", id.x, id.y, id.z));
            }
        }

        [HarmonyPatch(typeof(CellManager))]
        public static class CellManager_Patch
        {
            [HarmonyPatch(nameof(CellManager.TryLoadCacheBatchCells))]
            [HarmonyTranspiler] 
            public static IEnumerable<CodeInstruction> TryLoadCacheBatchCells_Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instruction_list = new List<CodeInstruction>(instructions);
                
                // find instruction that specifies the number of elements in the paths array
                var path_array_len_load_index = instruction_list.FindIndex(ins => ins.opcode == OpCodes.Ldc_I4_2); // find first
                if (path_array_len_load_index != -1)
                {
                    if (instruction_list[path_array_len_load_index - 1].opcode == OpCodes.Ldloca_S)
                        instruction_list[path_array_len_load_index] = new CodeInstruction(OpCodes.Ldc_I4_3); // 3 instead of 2
                }
                else
                {
                    Plugin.logSource.LogError("TryLoadCacheBatchCells_Transpiler - couldn't find OpCodes.Ldc_I4_2 in instructions");   
                }

                // just before instruction 'tryopeneither' add the third path
                var open_either_index = instruction_list.FindIndex(ins => ins.Calls(typeof(UWE.Utils).GetMethod(nameof(UWE.Utils.TryOpenEither), AccessTools.all)));
                if (open_either_index != -1)
                {
                    instruction_list.InsertRange(open_either_index, new CodeInstruction[] {
                        new CodeInstruction(OpCodes.Dup),
                        new CodeInstruction(OpCodes.Ldc_I4_2), // third path in the array 
                        new CodeInstruction(OpCodes.Ldarg_1), // put cells onto the stack
                        new CodeInstruction(OpCodes.Call, typeof(CellManager_Patch).GetMethod(nameof(GetAlternativeCellsPath))), // consumes cells, puts path onto stack
                        new CodeInstruction(OpCodes.Stelem_Ref)
                    });
                }
                else {
                    Plugin.logSource.LogError("TryLoadCacheBatchCells_Transpiler - couldn't find TryOpenEither in instructions");   
                }

                // disable spawn restrictions
                var before_target_call = instruction_list.FindIndex(ins => ins.Calls(typeof(CellManager).GetMethod(nameof(CellManager.LoadCacheBatchCellsFromStream), AccessTools.all)));
                instruction_list.InsertRange(before_target_call, new CodeInstruction[] { 
                    new CodeInstruction(OpCodes.Pop),
                    new CodeInstruction(OpCodes.Ldc_I4_1)
                 });

                return instruction_list;
            }

            public static string GetAlternativeCellsPath(BatchCells cells) {
                var id = cells.batch;
                return Plugin.FirstFolderWithFile(string.Format("Build/CellsCache/baked-batch-cells-{0}-{1}-{2}.bin", id.x, id.y, id.z));
            }
        }
    }
}