# VSML - Chart Mod

This is a VSML mod to load custom charts.

Charts should be places in a subdirectory of `CustomCharts` in the game folder.

### Chart format
Each chart needs a metadata file, `chart.json` in the root of its directory. This follows the format:

Note: The two filename fields get appended to the directory of the chart.json file
```json
{
    "Id": "mychart",
    "Name": "A Song Name",
    "Version": "0.0.1",
    "HasEncore": false,
    
    "SongFileName": "my_song.wav",
    "SongArtist": "An Artist",
    "DisplayBpm": "120",
    
    "JacketFileName": "cover_art.png",
    "JacketArtist": "An(other) Artist",
    
    "Difficulties": [
        {
            "DifficultyConstant": 1,
            "DifficultyDisplay": "1.0",
            "NoteDesigner": "Rainey"
        }
    ]
}
```

An entry should be added into the `Difficulties` array for each chart you provide, but set `HasEncore` to true if you provide one.
You should then add your binary chart files (`OPENING/MIDDLE/etc.vsb`) into the chart directory as well.

The game currently doesn't expect there to be less than three difficulties though, so even if you don't include them they will show in the song select menu.
> TODO: Write a patch to fix this somehow in the future.