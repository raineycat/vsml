using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace CustomChartLoader;

public class CustomSongPackScriptHook : ICodePatch
{
    public string PatchName => "Custom song pack script (hook)";
    public string TargetCodeName => "gml_GlobalScript_create_song_packs";
    
    public int? Target(List<UndertaleInstruction> instructions) =>
        instructions.FindIndex(i => i is
        {
            Kind: UndertaleInstruction.Opcode.Call,
            Type1: UndertaleInstruction.DataType.Int32,
            ValueFunction:
            {
                Name: { Content: "ds_map_create" }
            }
        });

    public IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode)
    {
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Call,
            Type1 = UndertaleInstruction.DataType.Int32,
            ArgumentsCount = 0,
            ValueFunction = gameData.Functions.ByName("gml_Script_VSMLSongPackHook")
        };

        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Popz,
            Type1 = UndertaleInstruction.DataType.Variable
        };
    }
}