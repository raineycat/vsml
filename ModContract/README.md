# VSML Mod Contract

This library defines the API that VSML mods have access to.

To make a mod, you should define a class that implements `IModInit`, which defines properties and callbacks that the loader will call when ready.

### Example
```csharp
public class ChartMod : IModInit
{
    public string ModName => "MyMod";
    public string ModVersion => "1.0.0";

    private Logger _logger = null!;

    public void SetupMod(IGameEnv gameEnv, Logger logger)
    {
        _logger = logger; 
        _logger.Information("Setting up!");
    }

    public void ApplyPatches(IPatchApplicator applicator)
    {
        _logger.Information("Applying patches!");
        
        // HookFunction usage example
        applicator.HookFunction("read_binary_chart", "MyTestHook", """
function $$hook(filepath, verifySig, modsFlag) {
    debug("HOOKED read_binary_chart: ", filepath, verifySig, modsFlag);
    return $$original(filepath, false, modsFlag);
}""");
    }
}
```

### Mod API
- `Logger` is just a Serilog logger instance, that writes to the main loader log file.
- `IPatchApplicator` is a helper class to allow you to patch the game. It provides helpers for patching code, but also gives you direct access to the game data object.
- `IGameEnv` provides data about the environment the game's running in, e.g. folder paths.
- `ICodePatch` represents a low-level GM:S assembly patch.
- `SourcePatch` is a wrapper over `ICodePatch` that compiles given GML code and inserts that instead of you writing assembly. Note: this might not play well with locals, I haven't tested it thoroughly.
- `IAdditionalScript` represents a new script to add into the game. This isn't inserted anywhere, you need to use one of the patch types above to call it from somewhere.
- `PatchHelpers` is a utility class for common patching operations. Currently, it provides a helper to generate assembly for calling `debug()` with a given string.