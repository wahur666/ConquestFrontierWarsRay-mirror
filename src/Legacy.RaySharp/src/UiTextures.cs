using System.Text.Json;
using Raylib_cs;

namespace RaySharp;

internal sealed class UiTextures
{
    public Texture2D ButtonBase { get; private init; }
    public Texture2D ButtonHover { get; private init; }
    public Texture2D OptionsPanel { get; private init; }
    public bool HasButtons { get; private init; }
    public bool HasOptionsPanel { get; private init; }

    public static UiTextures Load()
    {
        Texture2D buttonBase = default;
        Texture2D buttonHover = default;
        Texture2D optionsPanel = default;
        bool hasButtons = false;
        bool hasOptionsPanel = false;

        string? buttonFramesPath = FindAssetPath("buttonOk_frames.json");
        string? optionsFramesPath = FindAssetPath("options_frames.json");

        if (buttonFramesPath is not null)
        {
            buttonBase = LoadTextureFrame(buttonFramesPath, "frame_0001");
            buttonHover = LoadTextureFrame(buttonFramesPath, "frame_0002");
            hasButtons = buttonBase.Id != 0 && buttonHover.Id != 0;
        }

        if (optionsFramesPath is not null)
        {
            optionsPanel = LoadTextureFrame(optionsFramesPath, "frame_0000");
            hasOptionsPanel = optionsPanel.Id != 0;
        }

        return new UiTextures
        {
            ButtonBase = buttonBase,
            ButtonHover = buttonHover,
            OptionsPanel = optionsPanel,
            HasButtons = hasButtons,
            HasOptionsPanel = hasOptionsPanel
        };
    }

    public void Unload()
    {
        if (ButtonBase.Id != 0)
        {
            Raylib.UnloadTexture(ButtonBase);
        }

        if (ButtonHover.Id != 0)
        {
            Raylib.UnloadTexture(ButtonHover);
        }

        if (OptionsPanel.Id != 0)
        {
            Raylib.UnloadTexture(OptionsPanel);
        }
    }

    private static Texture2D LoadTextureFrame(string jsonPath, string frameName)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(jsonPath));
            string dataUrl = document.RootElement
                .GetProperty("frames")
                .GetProperty(frameName)
                .GetProperty("data")
                .GetString() ?? string.Empty;

            int commaIndex = dataUrl.IndexOf(',');
            if (commaIndex < 0)
            {
                return default;
            }

            byte[] pngBytes = Convert.FromBase64String(dataUrl[(commaIndex + 1)..]);
            Image image = Raylib.LoadImageFromMemory(".png", pngBytes);
            Texture2D texture = Raylib.LoadTextureFromImage(image);
            Raylib.UnloadImage(image);
            if (texture.Id != 0)
            {
                Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
            }

            return texture;
        }
        catch
        {
            return default;
        }
    }

    private static string? FindAssetPath(string fileName)
    {
        string[] candidates =
        [
            Path.Combine(Environment.CurrentDirectory, "web-rts", fileName),
                Path.Combine(Environment.CurrentDirectory, "RaySharp", "web-rts", fileName),
                Path.Combine(AppContext.BaseDirectory, "web-rts", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "web-rts", fileName),
                Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "RaySharp", "web-rts", fileName)
        ];

        foreach (string candidate in candidates)
        {
            string fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}
