namespace ManagedLoader;

public class PatchState
{
    public string OriginalDataHash { get; set; } = "";
    public Dictionary<string, string> DependentFileHashes { get; set; } = [];
}