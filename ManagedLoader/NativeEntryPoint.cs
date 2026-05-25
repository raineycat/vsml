using System.Runtime.InteropServices;
using ManagedLoader.RunnerInterop;

namespace ManagedLoader;

public static class NativeEntryPoint
{
    private static ModLoader? loaderInst;
    
    [UnmanagedCallersOnly]
    public static int LoaderMain(IntPtr arg, int argByteSize)
    {
        if (loaderInst != null)
        {
            ModLoader.Logger.Information("Second call LoaderMain: 0x{Argument:x8}", arg);
            ModLoader.Logger.Debug("{ManagedSize}/{NativeSize} bytes accounted for ({FuncCount} functions)",
                Marshal.SizeOf<RunnerInterface>(), argByteSize, Marshal.SizeOf<RunnerInterface>() / 8);
            
            var runner = Marshal.PtrToStructure<RunnerInterface>(arg);
            loaderInst.SetupInterop(new SafeRunner(runner));
            
            return 0;
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

    [UnmanagedCallersOnly]
    public static void InteropCall(IntPtr result, IntPtr self, IntPtr other, int argc, IntPtr args)
    {
        ModLoader.Logger.Debug("InteropCall(argc={Argc})", argc);
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
