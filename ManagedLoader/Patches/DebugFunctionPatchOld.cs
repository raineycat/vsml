using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ManagedLoader.Patches;

public class DebugFunctionPatchOld : ICodePatch
{
    public string PatchName => "Enable debug() function";
    public string TargetCodeName => "gml_GlobalScript_gmlMacros";

    public int? Target(List<UndertaleInstruction> instructions)
    {
        return instructions.FindIndex(i => i is
            { Kind: UndertaleInstruction.Opcode.Exit, Type1: UndertaleInstruction.DataType.Int32 });
    }

    public IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode)
    {
        // pushloc.v local.output
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.PushLoc,
            Type1 = UndertaleInstruction.DataType.Variable,
            ValueVariable = targetCode.FindLocalByName("output"),
            TypeInst = UndertaleInstruction.InstanceType.Local,
            ReferenceType = UndertaleInstruction.VariableType.Normal
        };
        
        // call.i show_message(argc=1)
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Call,
            Type1 = UndertaleInstruction.DataType.Int32,
            ArgumentsCount = 1,
            ValueFunction = gameData.Functions.ByName("show_debug_message")
        };
        
        // popz.v
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Popz,
            Type1 = UndertaleInstruction.DataType.Variable,
        };
    }
}