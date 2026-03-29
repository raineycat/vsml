function $$hook() {
    $$original();
    
    for(var i = 0; i < array_length(global.vsml_songs); i++) {
        var song = global.vsml_songs[i];
        
        global.unlocked_songs[song.song_id] = [
            array_contains(song.has_difficulties, "OPENING"),
            array_contains(song.has_difficulties, "MIDDLE"),
            array_contains(song.has_difficulties, "FINALE"),
            array_contains(song.has_difficulties, "ENCORE")
        ];
        
        debug("VSML UNLOCK: ", song.chart_id, global.unlocked_songs[song.song_id]);
    }  
}