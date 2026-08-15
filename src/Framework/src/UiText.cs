using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Shared text helpers for lightweight framework UI nodes.
/// </summary>
public static class UiText {
	private const int DefaultFontResolution = 64;
	private static Font _bodyFont;
	private static Font _titleFont;
	private static Font _monoFont;
	private static bool _fontsResolved;
	private static bool _bodyFontLoaded;
	private static bool _titleFontLoaded;
	private static bool _monoFontLoaded;

	public static float MeasureWidth(string text, float fontSize, UiTextStyle style = UiTextStyle.Body) {
		return Raylib.MeasureTextEx(GetFont(style), text, fontSize, 0f).X;
	}

	public static Vector2 MeasureSize(string text, float fontSize, UiTextStyle style = UiTextStyle.Body) {
		return Raylib.MeasureTextEx(GetFont(style), text, fontSize, 0f);
	}

	public static void Draw(string text, float x, float y, float fontSize, Color color, UiTextStyle style = UiTextStyle.Body) {
		Raylib.DrawTextEx(
			GetFont(style),
			text,
			new Vector2(MathF.Round(x), MathF.Round(y)),
			fontSize,
			0f,
			color);
	}

	public static void DrawCentered(string text, float centerX, float y, float fontSize, Color color, UiTextStyle style = UiTextStyle.Body) {
		var size = MeasureSize(text, fontSize, style);
		Draw(text, centerX - (size.X * 0.5f), y, fontSize, color, style);
	}

	private static Font GetFont(UiTextStyle style) {
		EnsureFontsResolved();

		return style switch {
			UiTextStyle.Title when _titleFontLoaded => _titleFont,
			UiTextStyle.Mono when _monoFontLoaded => _monoFont,
			UiTextStyle.Body when _bodyFontLoaded => _bodyFont,
			_ => Raylib.GetFontDefault()
		};
	}

	private static void EnsureFontsResolved() {
		if (_fontsResolved) {
			return;
		}

		_bodyFontLoaded = TryLoadFont(["arialnarrow_bold.ttf", "ARIALNB.TTF", "arial.ttf", "ARIAL.TTF"], out _bodyFont);
		_titleFontLoaded = TryLoadFont(["bauhmib.ttf", "BAUHS93.TTF", "arialbd.ttf", "ARIALBD.TTF"], out _titleFont);
		_monoFontLoaded = TryLoadFont(["OCRAEXT.TTF", "cour.ttf", "COUR.TTF", "consola.ttf", "CONSOLA.TTF"], out _monoFont);
		_fontsResolved = true;
	}

	private static bool TryLoadFont(IReadOnlyList<string> candidates, out Font font) {
		font = default;
		var fontPath = FindFontPath(candidates);
		if (fontPath is null) {
			return false;
		}

		font = Raylib.LoadFontEx(fontPath, DefaultFontResolution, null, 0);
		if (font.Texture.Id == 0) {
			font = default;
			return false;
		}

		Raylib.SetTextureFilter(font.Texture, TextureFilter.Bilinear);
		return true;
	}

	private static string? FindFontPath(IReadOnlyList<string> candidates) {
		foreach (var fileName in candidates) {
			var workspacePath = FindWorkspaceFontPath(fileName);
			if (workspacePath is not null) {
				return workspacePath;
			}
		}

		foreach (var fileName in candidates) {
			var systemPath = FindSystemFontPath(fileName);
			if (systemPath is not null) {
				return systemPath;
			}
		}

		return null;
	}

	private static string? FindWorkspaceFontPath(string fileName) {
		var current = new DirectoryInfo(AppContext.BaseDirectory);
		while (current is not null) {
			var candidate = Path.Combine(current.FullName, "Fonts", fileName);
			if (File.Exists(candidate)) {
				return candidate;
			}

			current = current.Parent;
		}

		return null;
	}

	private static string? FindSystemFontPath(string fileName) {
		if (!OperatingSystem.IsWindows()) {
			return null;
		}

		var windowsFontsPath = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.Windows),
			"Fonts",
			fileName);

		return File.Exists(windowsFontsPath) ? windowsFontsPath : null;
	}
}

public enum UiTextStyle {
	Body,
	Title,
	Mono
}
