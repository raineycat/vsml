using System.Text.Json;
using ModContract;
using Serilog.Core;
using UndertaleModLib;
using UndertaleModLib.Models;
using UndertaleModLib.Util;

namespace CustomChartLoader;

public class ChartMod : IModInit
{
    public string ModName => "CustomChartLoader";
    public string ModVersion => "0.0.1";

    private Logger _logger = null!;
    private List<CustomChartInfo> _customCharts = [];

    public void SetupMod(IGameEnv gameEnv, Logger logger)
    {
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

            try
            {
                var chartInfo = JsonSerializer.Deserialize<CustomChartInfo>(File.ReadAllText(infoPath))!;

                chartInfo.SongFileName = Path.Combine(dir, chartInfo.SongFileName);
                chartInfo.JacketFileName = Path.Combine(dir, chartInfo.JacketFileName);
                _customCharts.Add(chartInfo);
            }
            catch (Exception e)
            {
                _logger.Warning("Failed to load chart from {Dir}: {Exception}", dir, e);
            }
            
            _logger.Information("Loaded {Count} custom charts!", _customCharts.Count);
        }
    }

    public void ApplyPatches(IPatchApplicator applicator)
    {
        applicator.ApplyScript(new CustomChartScript(_customCharts));
        applicator.ApplyPatch(new CustomChartScriptHook());
        
        applicator.ApplyScript(new CustomSongPackScript());
        applicator.ApplyPatch(new CustomSongPackScriptHook());
        
        applicator.ApplyScript(new CustomChartFileScript());
        applicator.ApplyPatch(new CustomChartFileScriptHook("gml_GlobalScript_LoadSong"));
        applicator.ApplyPatch(new CustomChartFileScriptHook("gml_GlobalScript_LoadSongData"));
        
        foreach (var chart in _customCharts)
        {
            var audio = new UndertaleEmbeddedAudio
            {
                Name = applicator.MakeString("VSML_CustomChart_" + chart.Name),
                Data = File.ReadAllBytes(chart.SongFileName)
            };
            applicator.GameData.EmbeddedAudio.Add(audio);

            applicator.GameData.Sounds.Add(new UndertaleSound
            {
                AudioFile = audio,
                AudioGroup = applicator.GameData.AudioGroups.ByName("audiogroup_default"),
                Flags = UndertaleSound.AudioEntryFlags.Regular | UndertaleSound.AudioEntryFlags.IsEmbedded,
                Name = applicator.MakeString("music_chart_" + chart.Id),
                File = applicator.MakeString(Path.GetFileName(chart.SongFileName)),
                Type = applicator.MakeString(Path.GetExtension(chart.SongFileName))
            });
            
            applicator.GameData.Sounds.Add(new UndertaleSound
            {
                AudioFile = audio,
                AudioGroup = applicator.GameData.AudioGroups.ByName("audiogroup_default"),
                Flags = UndertaleSound.AudioEntryFlags.Regular | UndertaleSound.AudioEntryFlags.IsEmbedded,
                Name = applicator.MakeString("preview_" + chart.Id),
                File = applicator.MakeString(Path.GetFileName(chart.SongFileName)),
                Type = applicator.MakeString(Path.GetExtension(chart.SongFileName))
            });

            var jacketImg = GMImage.FromPng(File.ReadAllBytes(chart.JacketFileName));
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
    }
}