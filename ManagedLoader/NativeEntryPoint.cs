using System.Runtime.InteropServices;

namespace ManagedLoader;

public static class NativeEntryPoint
{
    private static ModLoader loaderInst = null!;
    
    [UnmanagedCallersOnly]
    public static int LoaderMain(IntPtr arg, int argByteSize)
    {
        try
        {
            RunLoader();
            return 0;
        }
        catch (Exception e)
        {
            File.WriteAllText("VSML Exception.txt", e.ToString());
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

        if (loaderInst.Config.PatchingEnabled)
        {
            loaderInst.RunPatching();
        }
    }
}
