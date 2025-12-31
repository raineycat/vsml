using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagedLoader.Patches;
using ModContract;
using Serilog;
using Serilog.Core;
using UndertaleModLib;

namespace ManagedLoader;

public class ModLoader : IGameEnv
{
    internal static LoaderConfig Config { get; private set; } = new();
    internal static Logger Logger { get; private set; } = null!;
    
    public string ModsFolder { get; }
    public string GameFolder { get; }

    private const string _logFilePath = "vsml.log";
    private const string _configFilePath = "vsml.json";
    
    private IProgressTracker _progressTracker;
    private UndertaleData _gameData = null!;
    private List<IModInit> _modInitializers = [];

    private static readonly JsonSerializerOptions _jsonConfigOptions = new()
    {
        WriteIndented = true,
        IndentSize = 4,
        Converters = { new JsonStringEnumConverter() }
    };
    
    public ModLoader()
    {
        _progressTracker = new DummyProgressTracker();
        
        if (File.Exists(_configFilePath))
        {
            Config = JsonSerializer.Deserialize<LoaderConfig>(File.ReadAllText(_configFilePath), _jsonConfigOptions) 
                     ?? new LoaderConfig();
        }
        else
        {
            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(Config, _jsonConfigOptions));
        }
        
        if (File.Exists(_logFilePath)) 
            File.Move(_logFilePath, _logFilePath + ".old", true);
        
        Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Config.LogLevel)
            .WriteTo.File(_logFilePath)
            .CreateLogger();
        
        ModsFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? 
                     throw new Exception("Failed to locate mods folder");
        GameFolder = Path.GetDirectoryName(ModsFolder) ??
                  throw new Exception("Failed to locate game folder");
        
        Logger.Debug("Found game dir: {GameDir}", GameFolder);
        Logger.Debug("Found mods dir: {ModsDir}", ModsFolder);

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            try
            {
                var name = new AssemblyName(args.Name);
                return Assembly.LoadFrom(Path.Combine(ModsFolder, name.Name + ".dll"));
            }
            catch (Exception)
            {
                return null;
            }
        };
        
        Logger.Debug("Starting to scan mods...");
        ScanMods();
        
        Logger.Information("Finished loader pre-init");
    }

    public void RunPatching()
    {
        // These are SWITCHED!
        var shadowFilePath = Path.Combine(GameFolder, "data.win");
        var dataFilePath = Path.Combine(GameFolder, "shadow.win");

        using (var stream = File.Open(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Logger.Debug("Loading data...");
            var sw = Stopwatch.StartNew();
            _gameData = UndertaleIO.Read(stream);
            Logger.Information("Finished loading data! Took {ElapsedTime}", sw.Elapsed);
        }

        {
            Logger.Debug("Applying patches...");
            var sw = Stopwatch.StartNew();
            ApplyPatches();
            Logger.Information("Finished applying patches! Took {ElapsedTime}", sw.Elapsed);
        }

        using (var stream = File.Open(shadowFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            Logger.Debug("Writing shadow file...");
            var sw = Stopwatch.StartNew();
            UndertaleIO.Write(stream, _gameData);
            Logger.Information("Finished writing shadow file! Took {ElapsedTime}", sw.Elapsed);
        }
        
        _gameData.Dispose();
        Logger.Information("Finished loader init");
    }

    private void ScanMods()
    {
        foreach (var dll in Directory.EnumerateFiles(ModsFolder, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                LoadMod(dll);
            }
            catch (Exception e)
            {
                Logger.Warning("Failed to load {Name}: {Exception}", Path.GetFileName(dll), e);
            }
        }
    }

    private void LoadMod(string path)
    {
        var assembly = Assembly.LoadFrom(path);
        foreach (var type in assembly.GetTypes().Where(t => t.GetInterfaces().Any(i => i.Name == "IModInit")))
        {
            Logger.Debug("Found IModInit: {Type}", type);
            var mod = Activator.CreateInstance(type) as IModInit;
            if(mod == null) 
                continue;

            mod.SetupMod(this, _progressTracker, Logger);
            _modInitializers.Add(mod);
            Logger.Information("Loaded mod: {Name} v{Version}", mod.ModName, mod.ModVersion);
        }
    }

    private void ApplyPatches()
    {
        var patcher = new PatchApplicator(_gameData);
        
        patcher.ApplyPatch(new DebugFunctionPatchOld());
        patcher.ApplyPatch(new AppendToVersionPatch(" (VSML 0.1.1)"));

        if (Config.EnableGameConsole)
        {
            patcher.ApplyPatch(new LogOnStartupPatch());
        }

        foreach (var mod in _modInitializers)
        {
            mod.ApplyPatches(patcher);
        }
    }
}