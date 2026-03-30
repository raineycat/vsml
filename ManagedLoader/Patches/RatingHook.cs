using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ManagedLoader.Patches;

public class RatingHook(bool isImagePatch = false) : ICodePatch
{
    public string PatchName => "Rating hook" + (isImagePatch ? " for images" : null);
    public string TargetCodeName => "gml_GlobalScript_add_value_to_ratingscore_table";
    UndertaleInstruction? inst;
    bool firstSkipped = false;

    public int? Target(List<UndertaleInstruction> instructions)
    {
        for (var i = 4; i < instructions.Count; i++)
        {
            if (instructions[i - 3] is
            {
                Kind: UndertaleInstruction.Opcode.PushGlb,
                ValueVariable:
                {
                    Name: { Content: "song_list" }
                }
            } && instructions[i - 2] is
            {
                Kind: UndertaleInstruction.Opcode.Call,
                ValueFunction:
                {
                    Name: { Content: "array_length" }
                }
            } && instructions[i - 1] is
            {
                Kind: UndertaleInstruction.Opcode.Cmp,
                ComparisonKind: UndertaleInstruction.ComparisonType.LT
            } && instructions[i] is
            {
                Kind: UndertaleInstruction.Opcode.Bf
            })
            {
                if (isImagePatch && !firstSkipped)
                {
                    firstSkipped = true;
                    continue;
                }
                inst = instructions[i];
                return i;
            }
        }

        return null;
    }

    public IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode)
    {
        // bf [end of loop]
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Bf,
            JumpOffset = inst!.JumpOffset + 4
        };
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
        
        /*
        if(variable_struct_get(global.song_list[i], "is_modded")) {
            continue;
        }
        */
    }
}