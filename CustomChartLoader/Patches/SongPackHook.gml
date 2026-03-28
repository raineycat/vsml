function $$hook(arg0) {
    $$original(arg0);
    
    if(array_length(global.vsml_songs) < 1) {
        return;
    }
     
    modded_song_ids = [];
    for(var i = 0; i < array_length(global.vsml_songs); i++) {
        array_push(modded_song_ids, global.vsml_songs[i].song_id);
    }
    
    array_insert(global.song_packs, 1, {
        name: "Modded songs",
        songs: modded_song_ids,
        color1: 16724480,
        color2: 16711935,
        description: "Songs added by VSML."
    });
}