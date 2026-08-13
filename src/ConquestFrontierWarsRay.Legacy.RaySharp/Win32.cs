using System.Runtime.InteropServices;

namespace RaySharp;

public static class Win32
{
    
    [StructLayout(LayoutKind.Sequential)]
    public struct NativePoint
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern bool GetClientRect(nint hWnd, out NativeRect lpRect);

    [DllImport("user32.dll")]
    public static extern bool ClientToScreen(nint hWnd, ref NativePoint lpPoint);

    [DllImport("user32.dll")]
    public static extern bool ClipCursor(ref NativeRect lpRect);

    [DllImport("user32.dll")]
    public static extern bool ClipCursor(nint lpRect);
}