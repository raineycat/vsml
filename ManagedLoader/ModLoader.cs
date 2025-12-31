using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
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
    internal static Logger Logger { get; private set; } = null!;
    internal  LoaderConfig Config { get; private set; } = new();
    internal IProgressTracker ProgressTracker { get; }
    internal bool ShouldSkipPatching { get; }
    
    public string ModsFolder { get; }
    public string GameFolder { get; }

    private const string _logFilePath = "vsml.log";
    private const string _configFilePath = "vsml.json";

    private UndertaleData _gameData = null!;
    private List<IModInit> _modInitializers = [];
    private PatchState _currentState = new();

    private static readonly JsonSerializerOptions _jsonConfigOptions = new()
    {
        WriteIndented = true,
        IndentSize = 4,
        Converters = { new JsonStringEnumConverter() }
    };
    
    public ModLoader()
    {
        ProgressTracker = new DummyProgressTracker();
        
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
        
        if (Config.EnableLoadingScreen)
        {
            const string pipeName = "VSMLProgressTracker";
            var proc = Process.Start(Path.Combine(ModsFolder, "LoadingWindow.exe"), [pipeName]);
            Logger.Debug("Started loading window process: {Proc}", proc);
            ProgressTracker = new NamedPipeProgressTracker(pipeName);
        }

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
        
        ProgressTracker.SetCurrentStep("Hashing data file");
        // (this points to data.win NOT shadow.win because of the path switch)
        _currentState.OriginalDataHash = HashFile(Path.Combine(GameFolder, "shadow.win"));
        Logger.Debug("Hashed data.win: {DataHash}", _currentState.OriginalDataHash);
        
        Logger.Debug("Starting to scan mods...");
        ProgressTracker.SetCurrentStep("Searching for mods");
        ScanMods();
        
        Logger.Information("Finished loader pre-init");

        var stateFilePath = Path.Combine(GameFolder, "vsml.state");
        if (File.Exists(stateFilePath))
        {
            Logger.Debug("Checking previous state file");
            var oldState = JsonSerializer.Deserialize<PatchState>(File.ReadAllText(stateFilePath)) ?? new();
            var matches = _currentState.OriginalDataHash == oldState.OriginalDataHash &&
                          _currentState.ModHashes.Values.ToImmutableSortedSet()
                              .SequenceEqual(oldState.ModHashes.Values.ToImmutableSortedSet());
            Logger.Debug("State match: {Matches}", matches);
            ShouldSkipPatching = matches;
        }
        
        File.WriteAllText(stateFilePath, JsonSerializer.Serialize(_currentState));
    }

    public void RunPatching()
    {
        // These are SWITCHED!
        var shadowFilePath = Path.Combine(GameFolder, "data.win");
        var dataFilePath = Path.Combine(GameFolder, "shadow.win");

        using (var stream = File.Open(dataFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Logger.Debug("Loading data...");
            ProgressTracker.SetCurrentStep("Loading data from disk");
            var sw = Stopwatch.StartNew();
            _gameData = UndertaleIO.Read(stream);
            Logger.Information("Finished loading data! Took {ElapsedTime}", sw.Elapsed);
        }

        {
            Logger.Debug("Applying patches...");
            ProgressTracker.SetCurrentStep("Patching");
            var sw = Stopwatch.StartNew();
            ApplyPatches();
            Logger.Information("Finished applying patches! Took {ElapsedTime}", sw.Elapsed);
        }

        using (var stream = File.Open(shadowFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
        {
            Logger.Debug("Writing shadow file...");
            ProgressTracker.SetCurrentStep("Writing data");
            var sw = Stopwatch.StartNew();
            UndertaleIO.Write(stream, _gameData, msg => ProgressTracker.SetCurrentStep("Writing data: " + msg));
            Logger.Information("Finished writing shadow file! Took {ElapsedTime}", sw.Elapsed);
        }
        
        _gameData.Dispose();
        Logger.Information("Finished loader init");
        ProgressTracker.SetCurrentStep("Finished!");
    }

    private void ScanMods()
    {
        foreach (var dll in Directory.EnumerateFiles(ModsFolder, "*.dll", SearchOption.AllDirectories))
        {
            if(File.Exists(Path.ChangeExtension(dll, "exe")))
                continue;

            ProgressTracker.SetCurrentStep("Hashing: " + Path.GetFileName(dll));
            var dllHash = HashFile(dll);
            _currentState.ModHashes.Add(dll, dllHash);
            
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
        ProgressTracker.SetCurrentStep("Loading: " + Path.GetFileName(path));
        var assembly = Assembly.LoadFrom(path);
        foreach (var type in assembly.GetTypes().Where(t => t.GetInterfaces().Any(i => i.Name == "IModInit")))
        {
            Logger.Debug("Found IModInit: {Type}", type);
            var mod = Activator.CreateInstance(type) as IModInit;
            if(mod == null) 
                continue;

            mod.SetupMod(this, ProgressTracker, Logger);
            _modInitializers.Add(mod);
            Logger.Information("Loaded mod: {Name} v{Version}", mod.ModName, mod.ModVersion);
        }
    }

    private static string HashFile(string path)
    {
        using var ms = File.OpenRead(path);
        var sha = SHA256.Create();
        var hash = sha.ComputeHash(ms);
        return Convert.ToHexStringLower(hash);
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