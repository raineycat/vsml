using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ManagedLoader.Patches;

public class CompletionRatingHook() : ICodePatch
{
    public string PatchName => "Completion rating hook";
    public string TargetCodeName => "gml_GlobalScript_get_rating_completion_value";
    UndertaleVariable? local;
    int jumpOffset;

    public int? Target(List<UndertaleInstruction> instructions)
    {
        for (var i = 5; i < instructions.Count; i++)
        {
            if (instructions[i - 5] is
            {
                Kind: UndertaleInstruction.Opcode.PushLoc,
                ValueVariable:
                {
                    Name: { Content: "song" }
                }
            } && instructions[i - 4] is
            {
                Kind: UndertaleInstruction.Opcode.PushI,
                Type1: UndertaleInstruction.DataType.Int16,
                ValueShort: -9
            } && instructions[i - 3] is
            {
                Kind: UndertaleInstruction.Opcode.Push,
                Type1: UndertaleInstruction.DataType.Variable,
                ValueVariable:
                {
                    Name: { Content: "name" }
                }
            } && instructions[i - 2] is
            {
                Kind: UndertaleInstruction.Opcode.Push,
                Type1: UndertaleInstruction.DataType.String
            } && instructions[i - 1] is
            {
                Kind: UndertaleInstruction.Opcode.Cmp,
                Type1: UndertaleInstruction.DataType.String,
                Type2: UndertaleInstruction.DataType.Variable,
                ComparisonKind: UndertaleInstruction.ComparisonType.EQ
            } && instructions[i] is
            {
                Kind: UndertaleInstruction.Opcode.Bt
            })
            {
                local = instructions[i - 5].ValueVariable;
                jumpOffset = instructions[i].JumpOffset;
                return i - 5; // we want to patch before not after
            }
        }

        return null;
    }

    public IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode)
    {
        // push.s "is_modded"
        var isModdedString = gameData.Strings.MakeString("is_modded");
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Push,
            Type1 = UndertaleInstruction.DataType.String,
            ValueString = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>(isModdedString)
        };

        // conv.s.v
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Conv,
            Type1 = UndertaleInstruction.DataType.String,
            Type2 = UndertaleInstruction.DataType.Variable
        };

        // pushloc.v local.song
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.PushLoc,
            Type1 = UndertaleInstruction.DataType.Variable,
            ValueVariable = local
        };

        // call.i variable_struct_get(argc=2)
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Call,
            ValueFunction = gameData.Functions.ByName("variable_struct_get"),
            ArgumentsCount = 2,
            Type1 = UndertaleInstruction.DataType.Int32
        };

        // conv.v.b
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Conv,
            Type1 = UndertaleInstruction.DataType.Variable,
            Type2 = UndertaleInstruction.DataType.Boolean
        };

        // bt [inside if statement]
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Bt,
            JumpOffset = jumpOffset + 9 // should figure out how to calculate this sometime
        };

        /*
        if (variable_struct_get(song, "is_modded") || ...) {
            continue;
        }
        */
    }
}