using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ManagedLoader.Patches;

public class AppendToVersionPatch(string textToAppend) : ICodePatch
{
    public string PatchName => "Add to version text";
    public string TargetCodeName => "gml_Object_initiategame_Create_0";

    public int? Target(List<UndertaleInstruction> instructions)
    {
        for(var i = 0; i < instructions.Count; i++)
        {
            if (instructions[i] is
                {
                    Kind: UndertaleInstruction.Opcode.Pop, 
                    Type1: UndertaleInstruction.DataType.Variable,
                    Type2: UndertaleInstruction.DataType.Variable,
                    ValueVariable:
                    {
                        Name:
                        {
                            Content: "version_number"
                        },
                        InstanceType: UndertaleInstruction.InstanceType.Global
                    }
                })
            {
                return i;
            }
        }

        return null;
    }

    public IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode)
    {
        // push.s [textToAppend]
        var textResource = gameData.Strings.MakeString(textToAppend);
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Push,
            Type1 = UndertaleInstruction.DataType.String,
            ValueString = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>(textResource)
        };
        
        // add.s.v
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Add,
            Type1 = UndertaleInstruction.DataType.String,
            Type2 = UndertaleInstruction.DataType.Variable
        };
    }
}