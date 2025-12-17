using UndertaleModLib;
using UndertaleModLib.Models;

namespace ModContract;

public interface ICodePatch
{
    string PatchName { get; }
    string TargetCodeName { get; }
    int? Target(List<UndertaleInstruction> instructions);
    IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode);
}