namespace CustomChartLoader;

public class CustomChartInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    
    public required string SongFileName { get; set; }
    public required string SongArtist { get; set; }
    public required string DisplayBpm { get; set; }
    
    public required string JacketFileName { get; set; }
    public required string JacketArtist { get; set; }
    
    public bool HasEncore { get; set; }
    public List<CustomDifficultyInfo> Difficulties { get; set; } = [];
}