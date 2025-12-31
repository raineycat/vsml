namespace CustomChartLoader;

public class ChartManifest
{
    public required string TempChartDir { get; set; }
    public List<CustomChartInfo> Charts { get; set; } = [];
}