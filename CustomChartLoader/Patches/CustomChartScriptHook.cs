using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace CustomChartLoader;

public class CustomChartScriptHook : ICodePatch
{
    public string PatchName => "Custom chart script (hook)";
    public string TargetCodeName => "gml_GlobalScript_load_song_information";
    
    public int? Target(List<UndertaleInstruction> instructions)
    {
        return instructions.FindIndex(i => i is
        {
            Kind: UndertaleInstruction.Opcode.Pop,
            Type1: UndertaleInstruction.DataType.Variable,
            Type2: UndertaleInstruction.DataType.Variable,
            ValueVariable:
            {
                Name: { Content: "songList" },
                InstanceType: UndertaleInstruction.InstanceType.Local 
            }
        });
    }

    public IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode)
    {
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Call,
            Type1 = UndertaleInstruction.DataType.Int32,
            ArgumentsCount = 1,
            ValueFunction = gameData.Functions.ByName("gml_Script_VSMLChartLoadHook")
        };
    }
}