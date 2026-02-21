using System.Reflection;
using K4os.Hash.xxHash;

namespace ManagedLoader;

public static class Utils
{
    public static string HashFileFast(string path)
    {
        const int bufferSize = 1024 * 256;
        using var stream = new FileStream(path, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.Read, 
            bufferSize, 
            FileOptions.SequentialScan);
        
        var hash = new XXH64();
        var buffer = new byte[bufferSize];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
            hash.Update(buffer.AsSpan(0, bytesRead));

        return Convert.ToHexStringLower(hash.DigestBytes());
    }

    public static void AddToLoadPath(string dir)
    {
        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            try
            {
                var name = new AssemblyName(args.Name);
                return Assembly.LoadFrom(Path.Combine(dir, name.Name + ".dll"));
            }
            catch (Exception)
            {
                return null;
            }
        };
    }
}