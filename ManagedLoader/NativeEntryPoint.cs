using System.Runtime.InteropServices;
using ModContract;

namespace ManagedLoader;

public static class NativeEntryPoint
{
    private static ModLoader? loaderInst = null!;
    
    [UnmanagedCallersOnly]
    public static int LoaderMain(IntPtr arg, int argByteSize)
    {
        if (loaderInst != null)
        {
            ModLoader.Logger.Information("Second call LoaderMain: {Argument}", arg);
            var runner = Marshal.PtrToStructure<RunnerInterface>(arg);

            unsafe
            {
                runner.YYError("MEOW", new RuntimeArgumentHandle());
            }
            
            return 1;
        }
        
        try
        {
            RunLoader();
            return 0;
        }
        catch (Exception e)
        {
            var exceptionPath = Path.Combine("VSML", "Logs");
            if (!Directory.Exists(exceptionPath))
                Directory.CreateDirectory(exceptionPath);
            
            File.WriteAllText(Path.Combine(exceptionPath, "exception.txt"), e.ToString());
            return 1;
        } 
        finally
        {
            if(loaderInst?.ProgressTracker is IDisposable d)
                d.Dispose();
        }
    }

    private static void RunLoader()
    {
        loaderInst = new ModLoader();

        if (loaderInst.Config.PatchingEnabled && !loaderInst.ShouldSkipPatching)
        {
            loaderInst.RunPatching();
        }
    }
}
