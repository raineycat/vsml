using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ManagedLoader.Patches;

public class RatingHook(bool isImagePatch = false) : ICodePatch
{
    public string PatchName => "Rating hook" + (isImagePatch ? " for images" : null);
    public string TargetCodeName => "gml_GlobalScript_add_value_to_ratingscore_table";
    UndertaleVariable? local;
    int instOffset;
    bool firstSkipped = false;

    public int? Target(List<UndertaleInstruction> instructions)
    {
        for (var i = 5; i < instructions.Count; i++)
        {
            if (instructions[i - 4] is
            {
                Kind: UndertaleInstruction.Opcode.PushGlb,
                ValueVariable:
                {
                    Name: { Content: "song_list" }
                }
            } && instructions[i - 3] is
            {
                Kind: UndertaleInstruction.Opcode.Call,
                ValueFunction:
                {
                    Name: { Content: "array_length" }
                }
            } && instructions[i - 2] is
            {
                Kind: UndertaleInstruction.Opcode.Cmp,
                ComparisonKind: UndertaleInstruction.ComparisonType.LT
            } && instructions[i - 1] is
            {
                Kind: UndertaleInstruction.Opcode.Bf
            })
            {
                if (isImagePatch && !firstSkipped)
                {
                    firstSkipped = true;
                    continue;
                }
                instOffset = i;
                local = instructions[i - 5].ValueVariable;
                return i;
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

        // call.i @@Global@@(argc=0)
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Call,
            Type1 = UndertaleInstruction.DataType.Int32,
            ArgumentsCount = 0,
            ValueFunction = gameData.Functions.ByName("@@Global@@") // VMConstants.GlobalFunction
        };

        // pushi.e -9
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.PushI,
            Type1 = UndertaleInstruction.DataType.Int16,
            ValueShort = -9
        };

        // pushloc.v local.i
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.PushLoc,
            Type1 = UndertaleInstruction.DataType.Variable,
            ValueVariable = local
        };

        // conv.v.i
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Conv,
            Type1 = UndertaleInstruction.DataType.Variable,
            Type2 = UndertaleInstruction.DataType.Int32
        };

        // push.v [array]self.song_list
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Push,
            Type1 = UndertaleInstruction.DataType.Variable,
            ValueVariable = gameData.Variables.First(v => v.Name.Content == "song_list" && v.InstanceType == UndertaleInstruction.InstanceType.Self),
            ReferenceType = UndertaleInstruction.VariableType.Array,
            TypeInst = UndertaleInstruction.InstanceType.Self
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

        UndertaleInstruction continueLoop = targetCode.Instructions.Skip(instOffset).First(
            i => i.Kind == UndertaleInstruction.Opcode.Bf
        );

        // bt [loop continue]
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Bt,
            JumpOffset = continueLoop.JumpOffset + 15 // should figure out how to calculate this sometime
        };

        /*
        if(variable_struct_get(global.song_list[i], "is_modded")) {
            continue;
        }
        */
    }
}