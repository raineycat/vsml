using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace ModContract;

public abstract class SourcePatch : ICodePatch
{
    public abstract string PatchName { get; }
    public abstract string TargetCodeName { get; }
    public abstract string PatchSourceCode { get; }

    public abstract int? Target(List<UndertaleInstruction> instructions);

    public IEnumerable<UndertaleInstruction> Codegen(UndertaleData gameData, UndertaleCode targetCode)
    {
        var nameString = gameData.Strings.MakeString("gml_Script___TEMP_PATCH");
        var codeObject = new UndertaleCode
        {
            Name = nameString
        };
        
        var compile = new CompileGroup(gameData);
        compile.QueueCodeReplace(codeObject, PatchSourceCode);
        
        var result = compile.Compile();
        if (!result.Successful)
        {
            throw new Exception("meow");
        }

        return codeObject.Instructions;
    }
}