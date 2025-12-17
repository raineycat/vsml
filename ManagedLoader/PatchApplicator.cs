using ModContract;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace ManagedLoader;

public class PatchApplicator(UndertaleData gameData) : IPatchApplicator
{
    public UndertaleData GameData { get; } = gameData;

    public void ApplyPatch(ICodePatch patch)
    {
        ModLoader.Logger.Debug("ApplyPatch: {PatchName} (to {CodeName})", patch.PatchName, patch.TargetCodeName);
        
        var targetCode = GameData.Code.ByName(patch.TargetCodeName);
        var injectPoint = patch.Target(targetCode.Instructions);
        if (injectPoint == null)
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
            if (ins.Kind is UndertaleInstruction.Opcode.B or UndertaleInstruction.Opcode.Bf or UndertaleInstruction.Opcode.Bt or UndertaleInstruction.Opcode.PushEnv or UndertaleInstruction.Opcode.PopEnv)
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
}