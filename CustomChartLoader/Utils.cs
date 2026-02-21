using System.Runtime.InteropServices;

namespace CustomChartLoader;

public static partial class Utils
{
    public const uint SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE = 2;
    
    [LibraryImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkA", StringMarshalling = StringMarshalling.Utf8)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CreateSymbolicLink(string symlinkFileName, string targetFileName, uint flags);
}