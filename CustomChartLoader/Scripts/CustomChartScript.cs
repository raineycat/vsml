using ModContract;

namespace CustomChartLoader;

public class CustomChartScript : IAdditionalScript
{
    public string FunctionName => "gml_GlobalScript_VSMLChartLoadHook";

    public string SourceCode { get; }

    public CustomChartScript(IEnumerable<CustomChartInfo> charts)
    {
        var songStructs = charts.Select((chart, i) =>
        {
            var code = $"""
                        song_id: lastID + {i},
                        chart_id: {chart.Id.Quote()},
                        name: {chart.Name.Quote()},
                        formatted_name: {chart.Name.Quote()},
                        artist: {chart.SongArtist.Quote()},
                        jacket_artist: {chart.JacketArtist.Quote()},
                        bpm_display: {chart.DisplayBpm.Quote()},
                        
                        audio_id: asset_get_index("music_chart_{chart.Id}"),
                        preview_id: asset_get_index("preview_{chart.Id}"),
                        jacket: asset_get_index("song_{chart.Id}"),
                        
                        has_encore: {chart.HasEncore.ToString().ToLower()},
                        is_original: false,
                        is_published: true,
                        is_modded: true,
                    """;

            code += """
                        unlock: {
                            type: 0,
                            enc_type: 0,
                            per_difficulty: false,
                            hidden: false,
                            hint: "",
                            enc_hint: "",
                    """;

            code += $"song_id: lastID + {i}";
            code += "},";

            var difficultyIndex = 0;
            foreach (var diff in chart.Difficulties.PadLengthTo(4))
            {
                difficultyIndex++;
                code = code
                    .AppendLine($"difficulty_constant_{difficultyIndex}: {diff.DifficultyConstant},")
                    .AppendLine($"difficulty_display_{difficultyIndex}: {diff.DifficultyDisplay.Quote()},")
                    .AppendLine($"note_designer_{difficultyIndex}: {diff.NoteDesigner.Quote()},");
            }
            
            return code;
        });
        
        SourceCode = """
                        function VSMLChartLoadHook(songList) {
                            var lastID = 0;
                            for (var i = 0; i < array_length(songList); i++)
                            {
                                var song = songList[i];
                                if(song.song_id > lastID) {
                                    lastID = song.song_id;
                                }
                            }
                            lastID++;
                            debug("VSML songs start at:", lastID);
                            
                            global.vsml_songs = [
                                { $$STRUCTS$$ }
                            ];
                            
                            debug("PATCHING SONG LIST WITH:", global.vsml_songs);
                            return array_concat(global.vsml_songs, songList);
                        }
                     """.Replace("$$STRUCTS$$", string.Join(" }, { ", songStructs));
    }
}