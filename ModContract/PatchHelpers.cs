using System.Reflection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ModContract;

public static class PatchHelpers
{
    public static IEnumerable<UndertaleInstruction> Debug(UndertaleData gameData, string text)
    {
        var stringObject = gameData.Strings.MakeString(text);
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Push,
            Type1 = UndertaleInstruction.DataType.String,
            ValueString = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>(stringObject)
        };

        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Conv,
            Type1 = UndertaleInstruction.DataType.String,
            Type2 = UndertaleInstruction.DataType.Variable
        };

        var debugFunc = gameData.Functions.ByName("gml_Script_debug");
        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Call,
            Type1 = UndertaleInstruction.DataType.Int32,
            ValueFunction = debugFunc,
            ArgumentsCount = 1
        };

        yield return new UndertaleInstruction
        {
            Kind = UndertaleInstruction.Opcode.Popz,
            Type1 = UndertaleInstruction.DataType.Variable
        };
    }

    public static UndertaleVariable FindLocalByName(this UndertaleCode code, string name)
    {
        return code.FindReferencedLocalVars().First(l => l.Name.Content == name);
    }

    public static UndertaleVariable FindReferencedVar(this UndertaleCode code, Predicate<UndertaleVariable> cond)
    {
        return code.Instructions
            .Where(i => i.ValueVariable != null)
            .Select(i => i.ValueVariable)
            .First(v => cond(v));
    }

    public static void ApplyScriptFromResource(this IPatchApplicator applicator, string codeEntryName)
    {
        using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(codeEntryName + ".gml");
        if (stream == null)
        {
            throw new ApplicationException("Missing script resource: " + codeEntryName);
        }

        applicator.ApplyScript(new EmbeddedResourceScript(codeEntryName, stream));
    }

    public static void ApplyScriptFromResource(this IPatchApplicator applicator, string codeEntryName, string resourceName)
    {
        using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new ApplicationException("Missing script resource: " + resourceName);
        }

        applicator.ApplyScript(new EmbeddedResourceScript(codeEntryName, stream));
    }

    public static void HookFunctionFromResource(this IPatchApplicator applicator, string targetName, string hookName, string resourceName)
    {
        using var stream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new ApplicationException("Missing hook resource: " + resourceName);
        }

        using var reader = new StreamReader(stream);
        var hookCode = reader.ReadToEnd();
        applicator.HookFunction(targetName, hookName, hookCode);
    }

    private class EmbeddedResourceScript(string codeName, Stream resource) : IAdditionalScript
    {
        public string FunctionName { get; } = codeName;
        public string SourceCode { get; } = new StreamReader(resource).ReadToEnd();
    }
}