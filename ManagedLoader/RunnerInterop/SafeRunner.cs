namespace ManagedLoader.RunnerInterop;

public class SafeRunner(RunnerInterface native)
{
    private RunnerInterface _interface = native;

    public unsafe void DebugConsoleOutput(string text) => _interface.DebugConsoleOutput(text, new RuntimeArgumentHandle());
    public unsafe void ReleaseConsoleOutput(string text) => _interface.ReleaseConsoleOutput(text, new RuntimeArgumentHandle());
    public unsafe void ShowMessage(string text) => _interface.ShowMessage(text);
    public unsafe void RaiseError(string text) => _interface.YYError(text, new RuntimeArgumentHandle());

    public unsafe IntPtr NativeAlloc(int size) => _interface.YYAlloc(size);
    public unsafe IntPtr NativeRealloc(IntPtr old, int size) => _interface.YYReAlloc(old, size);
    public unsafe void NativeFree(IntPtr ptr) => _interface.YYFree(ptr);
}