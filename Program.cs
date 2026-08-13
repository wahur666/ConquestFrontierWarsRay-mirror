using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Numerics;
using Raylib_cs;
using Color = Raylib_cs.Color;
using Font = System.Drawing.Font;
using RayRectangle = Raylib_cs.Rectangle;

const int windowWidth = 1600;
const int windowHeight = 960;
const string sampleText = "The quick brown fox jumps over the lazy dog. 0123456789 AaBbCcXx";

string baseDirectory = AppContext.BaseDirectory;
string fontsDirectory = Path.Combine(baseDirectory, "Fonts");
string previewListPath = Path.Combine(fontsDirectory, "fonttest.txt");
string previewImagePath = Path.Combine(fontsDirectory, "font-preview.png");

FontPreviewSheet sheet = FontPreviewBuilder.Build(previewListPath, fontsDirectory, previewImagePath, sampleText);

if (args.Contains("--render-only", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine($"Rendered {sheet.Entries.Count} font variants to {sheet.OutputPath}");
    return;
}

Raylib.InitWindow(windowWidth, windowHeight, "ConquestFrontierWarsRay Font Preview");
Raylib.SetTargetFPS(60);

Texture2D previewTexture = Raylib.LoadTexture(previewImagePath);
float scrollY = 0f;
float maxScroll = Math.Max(0f, previewTexture.Height - (windowHeight - 48f));

while (!Raylib.WindowShouldClose())
{
    scrollY = Math.Clamp(scrollY - Raylib.GetMouseWheelMove() * 48f, 0f, maxScroll);

    if (Raylib.IsKeyDown(KeyboardKey.Down))
    {
        scrollY = Math.Min(maxScroll, scrollY + 8f);
    }

    if (Raylib.IsKeyDown(KeyboardKey.Up))
    {
        scrollY = Math.Max(0f, scrollY - 8f);
    }

    Raylib.BeginDrawing();
    Raylib.ClearBackground(new Color(18, 18, 20, 255));

    Raylib.DrawText("Font layout preview", 16, 12, 28, Color.RayWhite);
    Raylib.DrawText($"Source: {Path.GetFileName(previewListPath)}", 16, 44, 18, Color.LightGray);
    Raylib.DrawText("Mouse wheel or Up/Down to scroll", 16, 66, 18, Color.LightGray);
    Raylib.DrawText($"Rendered variants: {sheet.Entries.Count}", 16, 88, 18, Color.LightGray);

    RayRectangle sourceRect = new(0, scrollY, previewTexture.Width, Math.Min(previewTexture.Height - scrollY, windowHeight - 128f));
    RayRectangle destRect = new(16, 128, previewTexture.Width, sourceRect.Height);
    Raylib.DrawTexturePro(previewTexture, sourceRect, destRect, Vector2.Zero, 0f, Color.White);

    Raylib.EndDrawing();
}

Raylib.UnloadTexture(previewTexture);
Raylib.CloseWindow();

internal sealed record FontPreviewEntry(string FamilyName, float Size, int Weight);

internal sealed record FontPreviewSheet(IReadOnlyList<FontPreviewEntry> Entries, int Width, int Height, string OutputPath);

internal static class FontPreviewBuilder
{
    private const int SheetWidth = 1500;
    private const int Margin = 24;
    private const int HeaderGap = 10;
    private const int EntryGap = 16;
    private const int LabelFontSize = 18;

    public static FontPreviewSheet Build(string fontListPath, string fontsDirectory, string outputPath, string sampleText)
    {
        List<FontPreviewEntry> entries = ParseEntries(fontListPath);
        PrivateFontCollection fontCollection = LoadPrivateFonts(fontsDirectory);

        int imageHeight = EstimateHeight(entries);
        using Bitmap bitmap = new(SheetWidth, imageHeight);
        using Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(System.Drawing.Color.FromArgb(245, 245, 245));
        graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

        using SolidBrush labelBrush = new(System.Drawing.Color.FromArgb(20, 20, 20));
        using SolidBrush sampleBrush = new(System.Drawing.Color.FromArgb(8, 8, 8));
        using SolidBrush noteBrush = new(System.Drawing.Color.FromArgb(90, 90, 90));
        using Font labelFont = new("Segoe UI", LabelFontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        using Font noteFont = new("Segoe UI", 14, FontStyle.Regular, GraphicsUnit.Pixel);

        int y = Margin;
        graphics.DrawString("ConquestFrontierWarsRay font layout readability test", labelFont, labelBrush, Margin, y);
        y += 34;
        graphics.DrawString("MS Sans Serif entries are rendered with Microsoft Sans Serif.", noteFont, noteBrush, Margin, y);
        y += 32;

        foreach (FontPreviewEntry entry in entries)
        {
            string label = $"{entry.FamilyName} | {entry.Size:0} px | weight {DescribeWeight(entry.Weight)}";
            using Font renderFont = CreateFont(entry, fontCollection, fontsDirectory);

            SizeF labelSize = graphics.MeasureString(label, labelFont);
            SizeF sampleSize = graphics.MeasureString(sampleText, renderFont, SheetWidth - (Margin * 2));
            float sampleHeight = Math.Max(sampleSize.Height, entry.Size + 8f);

            graphics.DrawString(label, labelFont, labelBrush, Margin, y);
            y += (int)Math.Ceiling(labelSize.Height) + HeaderGap;

            RectangleF sampleBounds = new(Margin, y, SheetWidth - (Margin * 2), sampleHeight + 8f);
            graphics.FillRectangle(Brushes.White, sampleBounds);
            graphics.DrawRectangle(Pens.LightGray, sampleBounds.X, sampleBounds.Y, sampleBounds.Width, sampleBounds.Height);
            graphics.DrawString(sampleText, renderFont, sampleBrush, sampleBounds.X + 8f, sampleBounds.Y + 4f);
            y += (int)Math.Ceiling(sampleBounds.Height) + EntryGap;
        }

        bitmap.Save(outputPath, ImageFormat.Png);
        return new FontPreviewSheet(entries, bitmap.Width, bitmap.Height, outputPath);
    }

    private static List<FontPreviewEntry> ParseEntries(string fontListPath)
    {
        List<FontPreviewEntry> entries = new();
        string? currentFamily = null;

        foreach (string rawLine in File.ReadLines(fontListPath))
        {
            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            if (!char.IsDigit(line[0]))
            {
                currentFamily = line;
                continue;
            }

            if (currentFamily is null)
            {
                continue;
            }

            string[] parts = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !float.TryParse(parts[0], out float size))
            {
                continue;
            }

            int weight = 400;
            if (parts.Length > 1 && !string.Equals(parts[1], "unspecified", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(parts[1], out weight);
            }

            entries.Add(new FontPreviewEntry(currentFamily, size, weight));
        }

        return entries;
    }

    private static PrivateFontCollection LoadPrivateFonts(string fontsDirectory)
    {
        PrivateFontCollection collection = new();

        foreach (string path in Directory.GetFiles(fontsDirectory, "*.ttf"))
        {
            collection.AddFontFile(path);
        }

        return collection;
    }

    private static Font CreateFont(FontPreviewEntry entry, PrivateFontCollection fontCollection, string fontsDirectory)
    {
        string resolvedFamilyName = ResolveFamilyName(entry.FamilyName);
        FontFamily? privateFamily = fontCollection.Families.FirstOrDefault(f =>
            string.Equals(f.Name, resolvedFamilyName, StringComparison.OrdinalIgnoreCase));

        FontStyle style = entry.Weight >= 700 ? FontStyle.Bold : FontStyle.Regular;

        if (privateFamily is not null)
        {
            FontStyle privateStyle = privateFamily.IsStyleAvailable(style) ? style : FontStyle.Regular;
            return new Font(privateFamily, entry.Size, privateStyle, GraphicsUnit.Pixel);
        }

        string systemFontPath = ResolveSystemFontPath(entry, fontsDirectory);
        PrivateFontCollection tempCollection = new();
        tempCollection.AddFontFile(systemFontPath);
        FontFamily tempFamily = tempCollection.Families[0];
        FontStyle tempStyle = tempFamily.IsStyleAvailable(style) ? style : FontStyle.Regular;
        return new Font(tempFamily, entry.Size, tempStyle, GraphicsUnit.Pixel);
    }

    private static string ResolveFamilyName(string familyName) =>
        familyName switch
        {
            "MS Sans Serif" => "Microsoft Sans Serif",
            "BauhausMdITCTT" => "Bauhaus Md BT",
            "OCR A Extended" => "OCR A Extended",
            _ => familyName
        };

    private static string ResolveSystemFontPath(FontPreviewEntry entry, string fontsDirectory)
    {
        string windowsFonts = Environment.GetFolderPath(Environment.SpecialFolder.Fonts);
        bool bold = entry.Weight >= 700;

        return entry.FamilyName switch
        {
            "Arial" => Path.Combine(windowsFonts, bold ? "arialbd.ttf" : "arial.ttf"),
            "Arial Narrow" => Path.Combine(fontsDirectory, bold ? "arialnarrow_bold.ttf" : "arialnarrow.ttf"),
            "BauhausMdITCTT" => Path.Combine(fontsDirectory, "bauhmib.ttf"),
            "Courier New" => Path.Combine(windowsFonts, bold ? "courbd.ttf" : "cour.ttf"),
            "MS Sans Serif" => Path.Combine(windowsFonts, "micross.ttf"),
            "OCR A Extended" => Path.Combine(fontsDirectory, "OCRAEXT.TTF"),
            _ => Path.Combine(windowsFonts, "arial.ttf")
        };
    }

    private static int EstimateHeight(IReadOnlyList<FontPreviewEntry> entries)
    {
        int height = Margin + 66;

        foreach (FontPreviewEntry entry in entries)
        {
            height += LabelFontSize + HeaderGap;
            height += (int)Math.Ceiling(entry.Size + 24f);
            height += EntryGap;
        }

        return height + Margin;
    }

    private static string DescribeWeight(int weight) =>
        weight <= 0 ? "unspecified" : weight.ToString();
}
