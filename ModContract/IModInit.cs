using Serilog.Core;
using UndertaleModLib;

namespace ModContract;

public interface IModInit
{
    public string ModName { get; }
    public string ModVersion { get; }
    
    void SetupMod(IGameEnv gameEnv, Logger logger);
    void ApplyPatches(IPatchApplicator applicator);
}
