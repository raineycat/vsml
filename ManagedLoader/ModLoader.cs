using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ManagedLoader.Patches;
using ManagedLoader.RunnerInterop;
using ModContract;
using Serilog;
using Serilog.Core;
using UndertaleModLib;

namespace ManagedLoader;

public class ModLoader : IGameEnv
{
    internal static Logger Logger { get; private set; } = null!;
    internal LoaderConfig Config { get; } = new();
    internal IProgressTracker ProgressTracker { get; }
    internal bool ShouldSkipPatching { get; }
    
    public string GameFolder { get; }
    public string LoaderDataFolder { get; }

    private UndertaleData _gameData = null!;
    private List<IModInit> _modInitializers = [];
    private PatchState _currentState = new();
    private InteropHelper _interopHelper = null!;

    private static readonly JsonSerializerOptions _jsonConfigOptions = new()
    {
        WriteIndented = true,
        IndentSize = 4,
        Converters = { new JsonStringEnumConverter() }
    };
    
    public ModLoader()
    {
        ProgressTracker = new DummyProgressTracker();
        
        var gamePath = Process.GetCurrentProcess().MainModule?.FileName
                       ?? throw new ApplicationException("Failed to get the game path");
        GameFolder = Path.GetDirectoryName(gamePath)
                     ?? throw new ApplicationException("Failed to get the game folder");
        LoaderDataFolder = Path.Combine(GameFolder, "VSML");
        
        Utils.AddToLoadPath(Path.Combine(LoaderDataFolder, "Core"));
        Utils.AddToLoadPath(Path.Combine(LoaderDataFolder, "Mods"));

        var configFilePath = Path.Combine(LoaderDataFolder, "config.json");
        var logFilePath = Path.Combine(LoaderDataFolder, "Logs", "managed.log");
        
        if (File.Exists(configFilePath))
        {
            Config = JsonSerializer.Deserialize<LoaderConfig>(File.ReadAllText(configFilePath), _jsonConfigOptions) 
                     ?? new LoaderConfig();
        }
        else
        {
            File.WriteAllText(configFilePath, JsonSerializer.Serialize(Config, _jsonConfigOptions));
        }
        
        if (File.Exists(logFilePath)) 
            File.Move(logFilePath, logFilePath + ".old", true);
        
        if (Config.EnableLoaderConsole)
            Utils.SetConsoleShown(true);

        Logger = new LoggerConfiguration()
            .MinimumLevel.Is(Config.LogLevel)
            .WriteTo.File(logFilePath)
            .WriteTo.Console()
            .CreateLogger();
        
        Logger.Information("Starting loader!");
        Logger.Debug("Found game dir: {GameDir}", GameFolder);
        Logger.Debug("Found loader dir: {ModsDir}", LoaderDataFolder);
        
        if (Config.EnableLoadingScreen)
        {
            var loadingWindowPath = Path.Combine(LoaderDataFolder, "Core", "LoadingWindow.exe");
            const string pipeName = "VSMLProgressTracker";
            var proc = Process.Start(loadingWindowPath, [pipeName]);
            Logger.Debug("Started loading window process: {Proc}", proc);
            ProgressTracker = new NamedPipeProgressTracker(pipeName);
        }
        
        ProgressTracker.SetCurrentStep("Hashing data file");
        // (this points to data.win NOT shadow.win because of the path switch)
        _currentState.OriginalDataHash = Utils.HashFileFast(Path.Combine(GameFolder, "shadow.win"));
        Logger.Debug("Hashed data.win: {DataHash}", _currentState.OriginalDataHash);
        
        Logger.Debug("Starting to scan mods...");
        ProgressTracker.SetCurrentStep("Searching for mods");
        ScanMods();
        
        Logger.Information("Finished loader pre-init");

        _currentState.DependentFileHashes.Add("_LOADER", Utils.HashFileFast(Path.Combine(LoaderDataFolder, "Core", "ManagedLoader.dll")));
        
        Logger.Debug("Registering mod dependency files");
        foreach (var file in _modInitializers.SelectMany(m => m.RegisterDependentFiles()))
        {
            if (!File.Exists(file))
            {
                Logger.Warning("Dependency file {Path} doesnt exist!", file);
                // have to add it here to still force a repatch
                _currentState.DependentFileHashes.Add(file, "");
                continue;
            }

            var hash = Utils.HashFileFast(file);
            _currentState.DependentFileHashes.Add(file, hash);
        }

        var stateFilePath = Path.Combine(LoaderDataFolder, "state.json");
        if (File.Exists(stateFilePath))
        {
            Logger.Debug("Checking previous state file");
            var oldState = JsonSerializer.Deserialize<PatchState>(File.ReadAllText(stateFilePath)) ?? new();
            var matches = _currentState.OriginalDataHash == oldState.OriginalDataHash &&
                          _currentState.DependentFileHashes.Values.ToImmutableSortedSet()
                              .SequenceEqual(oldState.DependentFileHashes.Values.ToImmutableSortedSet());
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
            stream.Close();
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
            stream.Close();
        }
        
        _gameData.Dispose();
        _gameData = null!;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        Logger.Information("Finished loader init");
        ProgressTracker.SetCurrentStep("Finished!");
    }

    public void SetupInterop(SafeRunner runner)
    {
        runner.ShowMessage("meowing!");
    }

    private void ScanMods()
    {
        var modsDir = Path.Combine(LoaderDataFolder, "Mods");
        foreach (var dll in Directory.EnumerateFiles(modsDir, "*.dll", SearchOption.AllDirectories))
        {
            if(File.Exists(Path.ChangeExtension(dll, "exe")))
                continue;

            ProgressTracker.SetCurrentStep("Hashing: " + Path.GetFileName(dll));
            var dllHash = Utils.HashFileFast(dll);
            _currentState.DependentFileHashes.Add(dll, dllHash);
            
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

    private void ApplyPatches()
    {
        _interopHelper = new InteropHelper(_gameData, this);
        _interopHelper.AddExtension();
        
        var patcher = new PatchApplicator(_gameData);
        patcher.ApplyPatch(new DebugFunctionPatchOld());
        patcher.ApplyPatch(new RatingHook());
        patcher.ApplyPatch(new RatingHook(true));
        patcher.ApplyPatch(new CompletionRatingHook());
        
        var version = GetType().Assembly.GetName().Version?.ToString() ?? "???";
        var appendText = $"  -  VSML {version}: {_modInitializers.Count} mods";
        patcher.ApplyPatch(new AppendToVersionPatch(appendText));

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