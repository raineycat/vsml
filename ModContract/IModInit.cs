using Serilog;

namespace ModContract;

public interface IModInit
{
    public string ModName { get; }
    public string ModVersion { get; }
    
    void SetupMod(IGameEnv gameEnv, IProgressTracker progressTracker, ILogger logger);
    void ApplyPatches(IPatchApplicator applicator);
}
