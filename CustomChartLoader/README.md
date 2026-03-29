# VSML - Chart Mod

This is a VSML mod to load custom charts.

Charts should be placed in the `[game dir]/VSML/CustomCharts` directory.

Currently, charts can either be a folder containing the needed resources, or a ZIP file with `chart.json` in the root.

### Chart format
Each chart needs a metadata file, `chart.json` in the root. This uses the following format:

> Note: The filename fields get appended to the directory of the chart.json file

```json
{
    "Id": "mychart",
    "Name": "A Song Name",
    "Version": "0.0.1",
    "HasEncore": false,
    
    "SongFileName": "my_song.wav",
    "PreviewFileName": "my_preview.wav",
    "SongArtist": "An Artist",
    "DisplayBpm": "120",
    
    "JacketFileName": "cover_art.png",
    "JacketArtist": "An(other) Artist",
    
    "Difficulties": {
        "OPENING": {
          "DifficultyConstant": 1,
          "DifficultyDisplay": "1.0",
          "NoteDesigner": "Rainey"
        }
    }
}
```

An entry should be added into the `Difficulties` map for each chart you provide, but set `HasEncore` to true if you provide one.
You should then add your binary chart files (`OPENING/MIDDLE/etc.vsb`) into the chart directory as well.

The jacket needs to be a PNG file, and only WAVs are known to work for audio.

`PreviewFileName` is optional, and will use the main song file if not specified.
