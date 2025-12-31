using System.Diagnostics;
using System.Text.Json;
using ModContract;
using Serilog;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace CustomChartLoader;

public class ChartMod : IModInit
{
    public string ModName => "CustomChartLoader";
    public string ModVersion => "0.0.1";

    private IGameEnv _gameEnv = null!;
    private IProgressTracker _progressTracker = null!;
    private ILogger _logger = null!;
    private List<IChartProvider> _customCharts = [];

    public void SetupMod(IGameEnv gameEnv, IProgressTracker progressTracker, ILogger logger)
    {
        _gameEnv = gameEnv;
        _progressTracker = progressTracker;
        _logger = logger;

        var chartsDir = Path.Combine(gameEnv.GameFolder, "CustomCharts");
        if (!Directory.Exists(chartsDir))
            Directory.CreateDirectory(chartsDir);

        foreach (var dir in Directory.EnumerateDirectories(chartsDir))
        {
            var infoPath = Path.Combine(dir, "chart.json");
            if (!File.Exists(infoPath))
            {
                _logger.Warning("No chart.json file in dir {Dir}!", dir);
                continue;
            }

            _customCharts.Add(new FolderChartProvider(dir));
        }
        
        foreach(var file in Directory.EnumerateFiles(chartsDir, "*.zip"))
        {
            _customCharts.Add(new ZippedChartProvider(file));
        }

        _logger.Information("Loaded {Count} custom charts!", _customCharts.Count);

        var tempDir = Path.Combine(Path.GetTempPath(), "vsml_chart");
        if(Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);
        Directory.CreateDirectory(tempDir);
        
        var manifest = new ChartManifest
        {
            TempChartDir = tempDir,
            Charts = _customCharts.Select(p => p.GetChartInfo()).WhereNotNull().ToList()
        };

        var manifestPath = Path.Combine(chartsDir, "_manifest");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest));
        _logger.Information("Written manifest to {Path}", manifestPath);

        string[] filesToExtract = ["OPENING.vsb", "MIDDLE.vsb", "FINALE.vsb", "ENCORE.vsb"];
        foreach (var chart in _customCharts)
        {
            var meta = chart.GetChartInfo();
            if(meta == null)
                continue;
            foreach (var file in filesToExtract)
            {
                var buf = chart.GetDataFile(file);
                if (buf.Length > 0)
                {
                    var path = Path.Combine(tempDir, meta.Id);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    File.WriteAllBytes(Path.Combine(path, file), buf);
                }
            }
        }
    }

    public void ApplyPatches(IPatchApplicator applicator)
    {
        applicator.ApplyScript(new CustomChartScript());
        applicator.ApplyPatch(new CustomChartScriptHook());
        
        applicator.ApplyScript(new CustomSongPackScript());
        applicator.ApplyPatch(new CustomSongPackScriptHook());
        
        applicator.ApplyScript(new CustomChartFileScript());
        applicator.ApplyPatch(new CustomChartFileScriptHook("gml_GlobalScript_LoadSong"));
        applicator.ApplyPatch(new CustomChartFileScriptHook("gml_GlobalScript_LoadSongData"));

        var nextAudioGroupId = applicator.GameData.AudioGroups.Count;
        var modAudioGroup = new UndertaleAudioGroup
        {
            Name = applicator.MakeString($"vsml_charts"),
        };
        applicator.GameData.AudioGroups.Add(modAudioGroup);
        applicator.ApplyPatch(new AudioGroupLoadPatch(nextAudioGroupId));
        
        var groupSounds = new List<UndertaleEmbeddedAudio>();
        var audioFlags = UndertaleSound.AudioEntryFlags.Regular | UndertaleSound.AudioEntryFlags.IsEmbedded;
        
        foreach (var provider in _customCharts)
        {
            var chart = provider.GetChartInfo();
            if(chart == null)
                continue;

            var nextAudioId = groupSounds.Count;

            var audioFile = new UndertaleEmbeddedAudio
            {
                Name = applicator.MakeString(chart.SongFileName),
                Data = provider.GetDataFile(chart.SongFileName)
            };
            groupSounds.Add(audioFile);

            applicator.GameData.Sounds.Add(new UndertaleSound
            {
                AudioFile = null,
                AudioGroup = modAudioGroup,
                AudioID = nextAudioId,
                GroupID = nextAudioGroupId,
                Flags = audioFlags,
                Name = applicator.MakeString("music_chart_" + chart.Id),
                File = applicator.MakeString(Path.GetFileName(chart.SongFileName)),
                Type = applicator.MakeString(Path.GetExtension(chart.SongFileName))
            });
            
            applicator.GameData.Sounds.Add(new UndertaleSound
            {
                AudioFile = null,
                AudioGroup = modAudioGroup,
                AudioID = nextAudioId,
                GroupID = nextAudioGroupId,
                Flags = audioFlags,
                Name = applicator.MakeString("preview_" + chart.Id),
                File = applicator.MakeString(Path.GetFileName(chart.SongFileName)),
                Type = applicator.MakeString(Path.GetExtension(chart.SongFileName))
            });

            var jacketImg = GMImage.FromPng(provider.GetDataFile(chart.JacketFileName));
            var jacketTex = new UndertaleEmbeddedTexture
            {
                Name = applicator.MakeString("JacketTex_" + chart.Id),
                TextureData = new UndertaleEmbeddedTexture.TexData
                {
                    Image = jacketImg
                }
            };
            var jacketTexPage = new UndertaleTexturePageItem()
            {
                Name = applicator.MakeString("JacketTexPage_" + chart.Id),
                SourceX = 0,
                SourceY = 0,
                SourceWidth = (ushort)jacketImg.Width,
                SourceHeight = (ushort)jacketImg.Height,
                TargetX = 0,
                TargetY = 0,
                TargetWidth = (ushort)jacketImg.Width,
                TargetHeight = (ushort)jacketImg.Height,
                BoundingWidth = (ushort)jacketImg.Width,
                BoundingHeight = (ushort)jacketImg.Height,
                TexturePage = jacketTex
            };
            var jacketSprite = new UndertaleSprite
            {
                Name = applicator.MakeString("song_" + chart.Id),
                Textures = [
                    new UndertaleSprite.TextureEntry { Texture = jacketTexPage }
                ],
                Width = (uint)jacketImg.Width,
                Height = (uint)jacketImg.Height
            };
            
            applicator.GameData.EmbeddedTextures.Add(jacketTex);
            applicator.GameData.TexturePageItems.Add(jacketTexPage);
            applicator.GameData.Sprites.Add(jacketSprite);
            
            _logger.Debug("Created assets for chart {Name}", chart.Name);
        }
        
        WriteAudioGroup($"audiogroup{nextAudioGroupId}.dat", groupSounds);
        _logger.Information("Finished custom chart patching");
    }

    private void WriteAudioGroup(string filename, List<UndertaleEmbeddedAudio> sounds)
    {
        var audioGroupFile = Path.Combine(_gameEnv.GameFolder, filename);
        _logger.Debug("Audio group path: {Path}; {SoundCount} sounds", audioGroupFile, sounds.Count);
        
        _logger.Debug("Creating audio group...");
        var sw = Stopwatch.StartNew();

        UndertaleData data;
        // taken from the UndertaleModTool script for audio import
        // https://github.com/UnderminersTeam/UndertaleModTool/blob/master/UndertaleModTool/Scripts/Resource%20Importers/ImportSingleSound.csx#L84
        using (var ms = new MemoryStream(Convert.FromBase64String("Rk9STQwAAABBVURPBAAAAAAAAAA=")))
        {
            data = UndertaleIO.Read(ms);
        }
            
        foreach (var sound in sounds)
        {
            data.EmbeddedAudio.Add(sound);
        }
        
        using (var stream = File.Open(audioGroupFile, FileMode.Create))
        {
            UndertaleIO.Write(stream, data);
        }
        _logger.Information("Finished writing audio group! Took {ElapsedTime}", sw.Elapsed);
    }
}