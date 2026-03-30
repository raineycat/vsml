using Serilog.Events;

namespace ManagedLoader;

public class LoaderConfig
{
    public bool PatchingEnabled { get; set; } = true;
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Debug;
    public bool EnableGameConsole { get; set; } = false;
    public bool EnableLoaderConsole { get; set; } = false;
    public bool EnableLoadingScreen { get; set; } = true;
}