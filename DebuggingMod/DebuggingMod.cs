using ModContract;
using Serilog;

namespace DebuggingMod;

public class DebuggingMod : IModInit
{
    public string ModName => "DebuggingMod";
    public string ModVersion => "0.0.1";

    private ILogger _logger = null!;
    private List<string> _scriptPaths = [];

    public void SetupMod(IGameEnv gameEnv, IProgressTracker progressTracker, ILogger logger)
    {
        _logger = logger;

        var scriptDir = Path.Combine(gameEnv.GameFolder, "DebugScripts");
        if (!Directory.Exists(scriptDir))
            Directory.CreateDirectory(scriptDir);

        foreach (var file in Directory.EnumerateFiles(scriptDir, "*.gml", SearchOption.AllDirectories))
        {
            _logger.Debug("Found script: {FilePath}", file);
            _scriptPaths.Add(file);
        }
    }

    public IEnumerable<string> RegisterDependentFiles()
    {
        return _scriptPaths;
    }

    public void ApplyPatches(IPatchApplicator applicator)
    {
        foreach (var file in _scriptPaths)
        {
            applicator.ApplyScript(new DebugScript(file));
        }
        
        _logger.Information("Applied debug scripts!");
    }
}