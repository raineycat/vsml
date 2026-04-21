using System.Runtime.InteropServices;

namespace ModContract;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct RunnerInterface
{
    public delegate* unmanaged<string, RuntimeArgumentHandle, void> DebugConsoleOutput;
    public delegate* unmanaged<string, RuntimeArgumentHandle, void> ReleaseConsoleOutput;
    public delegate* unmanaged<string, void> ShowMessage;
    public delegate* unmanaged<string, RuntimeArgumentHandle, void> YYError;
}