using System.Diagnostics;
using System.Text.Json;

namespace CustomChartLoader;

public class FolderChartProvider(string chartDir) : IChartProvider
{
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

    public IEnumerable<string> RegisterDependentFiles()
    {
        yield return Path.Combine(chartDir, "chart.json");

        var info = GetChartInfo();
        if(info == null)
            yield break;

        yield return Path.Combine(chartDir, info.JacketFileName);
        yield return Path.Combine(chartDir, info.SongFileName);
    }

    public bool TrySymlinkDataFile(string relativePath, string linkPath)
    {
        var targetPath = Path.Combine(chartDir, relativePath);
        if (!File.Exists(targetPath))
            return false;
        
        return Utils.CreateSymbolicLink(
            linkPath,
            targetPath,
            Utils.SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE);
    }
}