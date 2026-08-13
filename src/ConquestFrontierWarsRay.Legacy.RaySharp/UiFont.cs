using Raylib_cs;

namespace RaySharp;

internal sealed class UiFont
{
    public const float Spacing = 1.0f;

    private UiFont(Font font, bool shouldUnload)
    {
        Font = font;
        shouldUnloadFont = shouldUnload;
    }

    private readonly bool shouldUnloadFont;

    public Font Font { get; }

    public static UiFont Load()
    {
        string arialPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf");
        if (File.Exists(arialPath))
        {
            Font arial = Raylib.LoadFontEx(arialPath, 48, null, 0);
            if (Raylib.IsFontValid(arial))
            {
                Raylib.SetTextureFilter(arial.Texture, TextureFilter.Bilinear);
                return new UiFont(arial, true);
            }
        }

        return new UiFont(Raylib.GetFontDefault(), false);
    }

    public void Unload()
    {
        if (shouldUnloadFont)
        {
            Raylib.UnloadFont(Font);
        }
    }
}
