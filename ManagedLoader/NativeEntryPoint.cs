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
    }

    private static void RunLoader()
    {
        loaderInst = new ModLoader();

        if (ModLoader.Config.PatchingEnabled)
        {
            MessageBox.Info("Starting patching...");
            loaderInst.RunPatching();
            MessageBox.Info("Finished patching!");
        }
    }
}
