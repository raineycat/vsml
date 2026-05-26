using System.Reflection;
using System.Runtime.InteropServices;
using K4os.Hash.xxHash;

namespace ManagedLoader;

public static class Utils
{
    public static string HashFileFast(string path)
    {
        const int bufferSize = 1024 * 1024;
        using var stream = new FileStream(path, 
            FileMode.Open, 
            FileAccess.Read, 
            FileShare.Read, 
            bufferSize, 
            FileOptions.SequentialScan);
        
        var hash = new XXH64();
        var buffer = new byte[bufferSize];
        int bytesRead;
        
        do
        {
            bytesRead = stream.Read(buffer);
            hash.Update(buffer.AsSpan(0, bytesRead));
        } while (bytesRead > 0);
        ModLoader.Logger.Verbose("Finished hash loading");

        var digest = Convert.ToHexStringLower(hash.DigestBytes());
        ModLoader.Logger.Verbose("Hashed {Path}: {Digest}", path, digest);
        return digest;
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

    [DllImport("kernel32.dll")]
    static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("kernel32.dll")]
    static extern int AllocConsole();

    static bool consoleAllocated = false;
    public static void SetConsoleShown(bool showConsole)
    {
        if (showConsole && !consoleAllocated)
        {
            AllocConsole();
            StreamWriter standardOutput = new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            };
            Console.SetOut(standardOutput);
            consoleAllocated = true;
        }
        ShowWindow(GetConsoleWindow(), showConsole ? 5 : 0);
    }
}