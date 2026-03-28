using ModContract;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace ManagedLoader;

public class PatchApplicator(UndertaleData gameData) : IPatchApplicator
{
    public UndertaleData GameData { get; } = gameData;

    public void ApplyPatch(ICodePatch patch)
    {
        ModLoader.Logger.Debug("ApplyPatch: {PatchName} (to {CodeName})", patch.PatchName, patch.TargetCodeName);
        
        var targetCode = GameData.Code.ByName(patch.TargetCodeName);
        int? injectPoint = null;
        try
        {
            injectPoint = patch.Target(targetCode.Instructions);
        }
        catch (Exception e)
        {
            ModLoader.Logger.Warning("Patch {PatchName} targeter threw: {Exception}", patch.PatchName, e);
        }

        if (injectPoint == null || injectPoint < 0)
        {
            ModLoader.Logger.Warning("Patch {PatchName} failed to find an injection point", patch.PatchName);
            return;
        }
        
        var newCode = patch.Codegen(GameData, targetCode).ToList();
        var patchSize = (int)newCode.Sum(i => i.CalculateInstructionSize());
        
        var patchStartIdx = injectPoint.Value;
        var patchEndIdx = injectPoint.Value + newCode.Count;
        targetCode.Instructions.InsertRange(patchStartIdx, newCode);
        
        var patchStartOffset = targetCode.Instructions.Take(patchStartIdx).Sum(i => i.CalculateInstructionSize());
        var patchEndOffset = targetCode.Instructions.Take(patchEndIdx).Sum(i => i.CalculateInstructionSize());
        
        ModLoader.Logger.Debug("Patch spans instructions {StartIdx}-{EndIdx} ({StartOffset}-{EndOffset})",
            patchStartIdx, patchEndIdx, patchStartOffset, patchEndOffset);
        
        for(var i = 0; i < targetCode.Instructions.Count; i++)
        {
            var ins = targetCode.Instructions[i];
            if (ins.IsOfType(UndertaleInstruction.InstructionType.GotoInstruction))
            {
                ModLoader.Logger.Verbose("Branch: {Ins}", ins);
                var jumpTarget = targetCode.Instructions.Take(int.Max(0, i - newCode.Count))
                    .Sum(it => it.CalculateInstructionSize()) + ins.JumpOffset;
                
                if (i < patchStartIdx && jumpTarget > patchStartOffset)
                {
                    ins.JumpOffset += patchSize;
                }
                else if (i > patchEndIdx && jumpTarget < patchEndOffset)
                {
                    ins.JumpOffset -= patchSize;
                }
                ModLoader.Logger.Verbose(" -fix-> {Ins}", ins);
            }
        }

        foreach (var child in targetCode.ChildEntries)
        {
            ModLoader.Logger.Verbose("Child entry: {Name} @ {Offset}", child.Name.Content, child.Offset);
            if (child.Offset > patchStartOffset * 4)
            {
                var adjustment = patchSize * 4;
                child.Offset += (uint)adjustment;
                ModLoader.Logger.Verbose("Adjusted to {NewOffset} (+{Adjust})", child.Offset, adjustment);
            }
        }
        
        targetCode.UpdateLength();
    }

    public void ApplyScript(IAdditionalScript script)
    {
        var group = new CodeImportGroup(GameData);
        group.QueueReplace(script.FunctionName, script.SourceCode);
        
        var result = group.Import(false);
        if (!result.Successful)
        {
            ModLoader.Logger.Error("Failed to compile {Name}: {Errors}", script.FunctionName, result.Errors);
        }
    }

    public void HookFunction(string targetName, string hookName, string codeBody)
    {
        var realHookName = $"HOOK_{targetName}_{hookName}";
        // var trampolineName = $"ORIG_{targetName}_{hookName}";
        var trampolineName = targetName;
        ModLoader.Logger.Debug("Applying hook: {HookName}", realHookName);
        
        var importGroup = new CodeImportGroup(GameData);
        importGroup.QueueReplace($"gml_GlobalScript_{realHookName}", codeBody
            .Replace("$$hook", realHookName)
            .Replace("$$original", trampolineName));
        var importResult = importGroup.Import(false);

        if (!importResult.Successful)
        {
            ModLoader.Logger.Error("Hook compilation failed! {Errors}", importResult.PrintAllErrors(true));
            return;
        }
        ModLoader.Logger.Debug("Compiled hook code");

        var hookFunc = GameData.Functions.ByName($"gml_Script_{realHookName}");
        foreach (var code in GameData.Code)
        {
            if (code.Name.Content == $"gml_GlobalScript_{realHookName}")
            {
                continue;
            }
            
            foreach (var ins in code.Instructions)
            {
                if (ins.IsOfType(UndertaleInstruction.InstructionType.CallInstruction) && ins.ValueFunction != null)
                {
                    if (ins.ValueFunction.Name.Content != $"gml_Script_{targetName}")
                    {
                        continue;
                    }
                    
                    ModLoader.Logger.Verbose("Patching ValueFunction: [{CodeEntry}] {Old} -> {New}",
                        code, ins.ValueFunction, hookFunc);
                    ins.ValueFunction = hookFunc;
                }
            }
        }
        
        ModLoader.Logger.Debug("Finished hook application");
    }
}