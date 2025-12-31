namespace ManagedLoader;

public class PatchState
{
    public string OriginalDataHash { get; set; } = "";
    public Dictionary<string, string> ModHashes { get; set; } = [];
}