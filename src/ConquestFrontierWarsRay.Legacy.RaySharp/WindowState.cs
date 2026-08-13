namespace RaySharp;

internal sealed class WindowState(int windowedWidth, int windowedHeight)
{
    public int WindowedWidth { get; set; } = windowedWidth;
    public int WindowedHeight { get; set; } = windowedHeight;
    public bool ConfineMouse { get; set; }
}
