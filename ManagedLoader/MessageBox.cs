using System.Runtime.InteropServices;

namespace ManagedLoader;

public static class MessageBox
{
    [DllImport("user32.dll", CharSet = CharSet.Ansi)]
    private static extern int MessageBoxA(IntPtr hwnd, string text, string caption, int flags);
    
    public static void Info(string text) => MessageBoxA(IntPtr.Zero, text, "VSML Info", 64);
}