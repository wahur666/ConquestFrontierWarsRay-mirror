using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Shared text helpers for lightweight framework UI nodes.
/// </summary>
internal static class UiText {
	public static float MeasureWidth(string text, float fontSize) {
		return Raylib.MeasureTextEx(Raylib.GetFontDefault(), text, fontSize, 0f).X;
	}

	public static Vector2 MeasureSize(string text, float fontSize) {
		return Raylib.MeasureTextEx(Raylib.GetFontDefault(), text, fontSize, 0f);
	}

	public static void Draw(string text, float x, float y, float fontSize, Color color) {
		Raylib.DrawTextEx(
			Raylib.GetFontDefault(),
			text,
			new Vector2(MathF.Round(x), MathF.Round(y)),
			fontSize,
			0f,
			color);
	}

	public static void DrawCentered(string text, float centerX, float y, float fontSize, Color color) {
		var size = MeasureSize(text, fontSize);
		Draw(text, centerX - (size.X * 0.5f), y, fontSize, color);
	}
}
