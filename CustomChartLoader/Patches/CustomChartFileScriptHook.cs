using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace CustomChartLoader;

public class CustomChartFileScriptHook(string targetCodeName) : ICodePatch
{
    public string PatchName => "Redirect custom chart files";
    public string TargetCodeName => targetCodeName;
    
    public int? Target(List<UndertaleInstruction> instructions) =>
        instructions.FindIndex(i => i is
        {
            Kind: UndertaleInstruction.Opcode.Call,
            Type1: UndertaleInstruction.DataType.Int32,
            ValueFunction:
            {
                Name: { Content: "gml_Script_read_binary_chart" }
            }
        });

    public IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode)
    {
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Call,
            Type1 = UndertaleInstruction.DataType.Int32,
            ArgumentsCount = 1,
            ValueFunction = gameData.Functions.ByName("gml_Script_VSMLChartFileHook")
        };
    }
}