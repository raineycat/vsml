using UndertaleModLib;
using UndertaleModLib.Models;

namespace ModContract;

public interface IPatchApplicator
{
    UndertaleData GameData { get; }
    UndertaleString MakeString(string s) => GameData.Strings.MakeString(s);
    void ApplyPatch(ICodePatch patch);
    void ApplyScript(IAdditionalScript script);
    void HookFunction(string targetName, string hookName, string codeBody);
}