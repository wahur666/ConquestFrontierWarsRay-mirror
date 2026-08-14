using Raylib_cs;

namespace RaySharp;

public static class UiLayout
{
    public static readonly Rectangle MenuButton = new(14, 66, 121, 25);
    public static Rectangle OptionsPanel => new((Raylib.GetScreenWidth() - 515) * 0.5f, (Raylib.GetScreenHeight() - 407) * 0.5f, 515, 407);
    public static Rectangle CloseButton
    {
        get
        {
            Rectangle optionsPanel = OptionsPanel;
            return new Rectangle(optionsPanel.X + optionsPanel.Width - 139, optionsPanel.Y + 18, 121, 25);
        }
    }
}