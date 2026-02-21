namespace CustomChartLoader;

public interface IChartProvider
{
    CustomChartInfo? GetChartInfo();
    byte[] GetDataFile(string relativePath);
    IEnumerable<string> RegisterDependentFiles();
    bool TrySymlinkDataFile(string relativePath, string linkPath);
}