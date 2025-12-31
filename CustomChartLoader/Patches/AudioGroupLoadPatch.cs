using ModContract;
using Underanalyzer;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace CustomChartLoader;

public class AudioGroupLoadPatch(int audioGroupId) : ICodePatch
{
    public string PatchName => "Load the VSML audio group";
    public string TargetCodeName => "gml_Object_obj_resource_loader_Create_0";

    public int? Target(List<UndertaleInstruction> instructions) =>
        instructions.FindIndex(i => i is
        {
            Kind: UndertaleInstruction.Opcode.Call,
            Type1: UndertaleInstruction.DataType.Int32,
            ArgumentsCount: 0,
            ValueFunction:
            {
                Name: { Content: "ini_close" }
            }
        });

    public IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode)
    {
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Break,
            ExtendedKind = (short)IGMInstruction.ExtendedOpcode.PushReference,
            Type1 = UndertaleInstruction.DataType.Int32,
            ValueInt = 2 << 24 | audioGroupId
        };

        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Push,
            Type1 = UndertaleInstruction.DataType.Variable,
            TypeInst = UndertaleInstruction.InstanceType.Builtin,
            ReferenceType = UndertaleInstruction.VariableType.Normal,
            ValueVariable = targetCode.FindReferencedVar(v => v is
            {
                Name: { Content: "audioGroups" }
            })
        };
        
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Call,
            Type1 = UndertaleInstruction.DataType.Int32,
            ArgumentsCount = 2,
            ValueFunction = gameData.Functions.ByName("array_push")
        };

        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Popz,
            Type1 = UndertaleInstruction.DataType.Variable
        };
    }
}