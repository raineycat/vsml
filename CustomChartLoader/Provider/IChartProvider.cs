namespace CustomChartLoader;

public interface IChartProvider
{
    CustomChartInfo? GetChartInfo();
    byte[] GetDataFile(string relativePath);
}