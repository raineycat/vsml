using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Reloaded.Memory.Sigscan;
using Serilog.Core;

namespace ManagedLoader;

public class FunctionLogger
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

    [StructLayout(LayoutKind.Sequential)]
    struct RValue
    {
        public IntPtr valPtr;
        public uint flags;
        public uint type;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    struct RToken
    {
        public int m_Kind;
        public uint m_Type;
        public int m_Ind;
        public int m_Ind2;
        public RValue m_Value;
        public int m_ItemNumber;
        public unsafe RToken* m_Items;
        public int m_Position;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    struct CCode
    {
        public IntPtr vt;
        public IntPtr next;
        public int kind;
        public int compiled;
        public IntPtr str;
        public RToken token;
        public RValue value;
        public IntPtr vmInstance;
        public IntPtr vmDebugInfo;
        public IntPtr name;
        public int codeIndex;
        public IntPtr functions;
        public bool watch;
        public int offset;
        public int localCount;
        public int argCount;
        public int flags;
        public IntPtr prototype;
    }
    
    public unsafe void RunPatching()
    {
        ModLoader.Logger.Information("Run patching!");

        var thisProcess = Process.GetCurrentProcess();
        var scanner = new Scanner(thisProcess, thisProcess.MainModule);
        var offset = scanner.FindPattern("E8 ?? ?? ?? ?? 3C 01 74 ??");
        ModLoader.Logger.Debug($"Got offset {offset.Offset}");

        IntPtr callInstructionAddress = IntPtr.Add(thisProcess.MainModule.BaseAddress, offset.Offset);
        int relativeOffset = Marshal.ReadInt32(IntPtr.Add(callInstructionAddress, 1));
        IntPtr targetFunctionAddress = IntPtr.Add(callInstructionAddress, 5 + relativeOffset);

        ModLoader.Logger.Debug($"Got offset {targetFunctionAddress}");

        var manager = new PageCodeManager();

        var hook = FunctionHook.Create(
            manager, (void*)targetFunctionAddress, (delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr, int, byte>)&HookExecuteIt);

        hook.IsActive = true;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    // bool ExecuteIt(*CInstance, *CInstance, *CCode, *RValue, int)
    private static unsafe byte HookExecuteIt(IntPtr _self, IntPtr _other, IntPtr _code, IntPtr _args, int flags)
    {
        CCode* code = (CCode*)_code.ToPointer();
        ModLoader.Logger.Debug($"ExevuteIt: {Marshal.PtrToStringAnsi(code->name)}");

        return ((delegate* unmanaged[Stdcall]<IntPtr, IntPtr, IntPtr, IntPtr, int, byte>)FunctionHook.Current.OriginalCode)(_self, _other, _code, _args, flags);
    }
}