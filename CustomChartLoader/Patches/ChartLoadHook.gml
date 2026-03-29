function VSMLOpenJsonFile(filePath) {
   var file = file_text_open_read(filePath);
   var json = "";
   while(!file_text_eof(file)) {
       json += file_text_read_string(file);
       file_text_readln(file);
   }
   file_text_close(file);
   
   debug("Loaded VSML json:", json);
   return json_parse(json);
}

function VSMLChartLoadHook(songList) {
   var lastId = 0;
   for (var i = 0; i < array_length(songList); i++)
   {
       var song = songList[i];
       if(song.song_id > lastId) {
           lastId = song.song_id;
       }
   }
   lastId++;
   debug("VSML songs start at:", lastId);
   
   var manifest = VSMLOpenJsonFile("VSML/CustomCharts/_manifest");
   global.vsml_chart_dir = manifest.TempChartDir;
   global.vsml_songs = [];
   
   for (var i = 0; i < array_length(manifest.Charts); i++) {
       var chart = manifest.Charts[i];
       var data = {
           song_id: lastId + i,
           chart_id: chart.Id,
           name: chart.Name,
           formatted_name: chart.Name,
           artist: chart.SongArtist,
           jacket_artist: chart.JacketArtist,
           bpm_display: chart.DisplayBpm,
           
           audio_id: asset_get_index("music_chart_" + chart.Id),
           preview_id: asset_get_index("preview_" + chart.Id),
           jacket: asset_get_index("song_" + chart.Id),
           
           has_encore: chart.HasEncore,
           is_original: false,
           is_published: true,
           is_modded: true,
           
           unlock: {
               type: 0,
               enc_type: 0,
               per_difficulty: false,
               hidden: false,
               hint: "",
               enc_hint: "",
               song_id: lastId + i,
           },
           
           difficulty_constant_1: 0,
           difficulty_display_1: "0",
           note_designer_1: "?",
           
           difficulty_constant_2: 0,
           difficulty_display_2: "0",
           note_designer_2: "?",

           difficulty_constant_3: 0,
           difficulty_display_3: "0",
           note_designer_3: "?",

           difficulty_constant_4: 0,
           difficulty_display_4: "0",
           note_designer_4: "?",
           
           has_difficulties: [],
       };
       
       var presentDifficulties = variable_struct_get_names(chart.Difficulties);
       var difficultyNames = ["OPENING", "MIDDLE", "FINALE", "ENCORE"];
       
       for (var j = 0; j < array_length(presentDifficulties); j++) {
           var diffName = presentDifficulties[j];
           var diffInfo = variable_struct_get(chart.Difficulties, diffName);
           var diffNum = array_get_index(difficultyNames, diffName) + 1;
           
           if (diffNum < 1) {
               continue;
           }
           
           struct_set(data, string("difficulty_constant_{0}", diffNum), diffInfo.Value);
           struct_set(data, string("difficulty_display_{0}", diffNum), diffInfo.Display);
           struct_set(data, string("note_designer_{0}", diffNum), diffInfo.NoteDesigner);
       }
       
       data.has_difficulties = presentDifficulties;
       array_push(global.vsml_songs, data);
   }
   
   debug("PATCHING SONG LIST WITH:", global.vsml_songs);
   return array_concat(global.vsml_songs, songList);
}