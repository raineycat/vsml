using System.Text.Json;
using System.IO.Compression;

namespace CustomChartLoader;

public class ZippedChartProvider(string zipFilePath) : IChartProvider, IDisposable
{
    public string ZipFilePath => zipFilePath;
    private ZipArchive _archive = ZipFile.OpenRead(zipFilePath);

    public CustomChartInfo? GetChartInfo()
    {
        var entry = _archive.GetEntry("chart.json");
        if(entry == null)
            return null;
        
        using var stream = entry.Open();
        return JsonSerializer.Deserialize<CustomChartInfo>(stream);
    }

    public byte[] GetDataFile(string relativePath)
    {
        var entry = _archive.GetEntry(relativePath);
        if(entry == null)
            return [];
        
        using var zs = entry.Open();
        using var ms = new MemoryStream();
        zs.CopyTo(ms);
        return ms.GetBuffer();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _archive.Dispose();
    }
}