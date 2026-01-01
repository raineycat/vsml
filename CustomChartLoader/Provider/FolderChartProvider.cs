using System.Text.Json;

namespace CustomChartLoader;

public class FolderChartProvider(string chartDir) : IChartProvider
{
    public string ChartDir => chartDir;
    
    public CustomChartInfo? GetChartInfo()
    {
        var path = Path.Combine(chartDir, "chart.json");
        if(!File.Exists(path))
        {
            return null;
        }

        var text = File.ReadAllText(path);
        return JsonSerializer.Deserialize<CustomChartInfo>(text);
    }

    public byte[] GetDataFile(string relativePath)
    {
        var path = Path.Combine(chartDir, relativePath);
        if(!File.Exists(path))
        {
            return [];
        }

        return File.ReadAllBytes(path);
    }
}