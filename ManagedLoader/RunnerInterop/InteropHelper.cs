using ModContract;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ManagedLoader.RunnerInterop;

public class InteropHelper
{
    private UndertaleData _data;
    private string _dllPath;

    public InteropHelper(UndertaleData gameData, IGameEnv env)
    {
        _data = gameData;
        _dllPath = Path.Combine(env.GameFolder, "version.dll");
    }
    
    public void AddExtension()
    {
        ModLoader.Logger.Debug("Adding interop extension");
        
        var funcTest = new UndertaleExtensionFunction
        {
            ID = 90,
            Kind = 11,
            Name = _data.Strings.MakeString("vsml_interop_test"),
            ExtName = _data.Strings.MakeString("vsml_interop_test"),
            RetType = UndertaleExtensionVarType.String,
        };
        
        var extensionDll = new UndertaleExtensionFile
        {
            Kind = UndertaleExtensionKind.Dll,
            Filename = _data.Strings.MakeString("version.dll"),
            InitScript = _data.Strings.MakeString(""),
            CleanupScript = _data.Strings.MakeString("")
        };
        extensionDll.Functions.Add(funcTest);

        var version = GetType().Assembly.GetName().Version?.ToString() ?? "0.0.0";
        var ext = new UndertaleExtension
        {
            Name = _data.Strings.MakeString("VSMLInterop"),
            Version = _data.Strings.MakeString(version),
            ClassName = _data.Strings.MakeString(""),
            FolderName = _data.Strings.MakeString("")
        };
        ext.Files.Add(extensionDll);
        _data.Extensions.Add(ext);
    }
}