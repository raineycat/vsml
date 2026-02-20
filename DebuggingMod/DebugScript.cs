using ModContract;

namespace DebuggingMod;

public class DebugScript : IAdditionalScript
{
    public string FunctionName { get; private set; }
    public string SourceCode { get; private set; }

    public DebugScript(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        FunctionName = "gml_GlobalScript_VSMLDebugScript_" + fileName.Replace(" ", "_");
        SourceCode = File.ReadAllText(filePath);
    }
}