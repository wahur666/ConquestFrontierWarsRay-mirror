using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.Core;

internal static class CombatUiAssets {
	private static Font _font;
	private static Texture2D _heartIcon;
	private static Texture2D _shieldIcon;
	private static Texture2D _chevronIcon;
	private static bool _loaded;

	public static Font Font {
		get {
			EnsureLoaded();
			return _font;
		}
	}

	public static Texture2D HeartIcon {
		get {
			EnsureLoaded();
			return _heartIcon;
		}
	}

	public static Texture2D ShieldIcon {
		get {
			EnsureLoaded();
			return _shieldIcon;
		}
	}

	public static Texture2D ChevronIcon {
		get {
			EnsureLoaded();
			return _chevronIcon;
		}
	}

	public static void Load() {
		EnsureLoaded();
	}

	public static void Unload() {
		if (!_loaded) {
			return;
		}

		Raylib.UnloadFont(_font);

		if (_heartIcon.Id != 0) {
			Raylib.UnloadTexture(_heartIcon);
		}

		if (_shieldIcon.Id != 0) {
			Raylib.UnloadTexture(_shieldIcon);
		}

		if (_chevronIcon.Id != 0) {
			Raylib.UnloadTexture(_chevronIcon);
		}

		_loaded = false;
	}

	public static float MeasureText(string text, float fontSize) {
		return Raylib.MeasureTextEx(Font, text, fontSize, 0f).X;
	}

	public static Vector2 MeasureTextSize(string text, float fontSize) {
		return Raylib.MeasureTextEx(Font, text, fontSize, 0f);
	}

	public static void DrawText(string text, float x, float y, float fontSize, Color color) {
		Raylib.DrawTextEx(Font, text, new Vector2(MathF.Round(x), MathF.Round(y)), fontSize, 0f, color);
	}

	public static void DrawTextCentered(string text, float centerX, float y, float fontSize, Color color) {
		var size = MeasureTextSize(text, fontSize);
		DrawText(text, centerX - (size.X / 2f), y, fontSize, color);
	}

	public static void DrawShadowedText(string text, float centerX, float y, float fontSize, Color color,
		float shadowOffset = 1f) {
		var size = MeasureTextSize(text, fontSize);
		var x = centerX - (size.X / 2f);
		Raylib.DrawTextEx(Font, text, new Vector2(MathF.Round(x + shadowOffset), MathF.Round(y + shadowOffset)),
			fontSize, 0f, Color.Black);
		Raylib.DrawTextEx(Font, text, new Vector2(MathF.Round(x), MathF.Round(y)), fontSize, 0f, color);
	}

	private static void EnsureLoaded() {
		if (_loaded) {
			return;
		}

		_font = Raylib.LoadFontEx(FindFontPath(), 72, null, 0);
		_heartIcon = LoadTexture(Path.Combine("icons", "heart.svg"));
		_shieldIcon = LoadTexture(Path.Combine("icons", "shield.svg"));
		_chevronIcon = LoadTexture(Path.Combine("icons", "chevron.svg"));
		_loaded = true;
	}

	private static string FindFontPath() {
		string[] preferredFonts = [
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "tahoma.ttf"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "segoeui.ttf"),
			FindAssetPath(Path.Combine("fonts", "Roboto-Regular.ttf"))
		];

		foreach (var candidate in preferredFonts) {
			if (File.Exists(candidate)) {
				return candidate;
			}
		}

		throw new FileNotFoundException("Could not find a readable UI font.");
	}

	private static Texture2D LoadTexture(string relativePath) {
		var path = FindAssetPath(relativePath);
		return Raylib.LoadTexture(path);
	}

	private static string FindAssetPath(string relativePath) {
		string[] candidates = [
			Path.Combine(AppContext.BaseDirectory, "assets", relativePath),
			Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "assets", relativePath),
			Path.Combine("assets", relativePath),
			Path.Combine("EventHorizon", "assets", relativePath)
		];

		foreach (var candidate in candidates) {
			var fullPath = Path.GetFullPath(candidate);
			if (File.Exists(fullPath)) {
				return fullPath;
			}
		}

		throw new FileNotFoundException($"Could not find asset '{relativePath}'.", relativePath);
	}
}
