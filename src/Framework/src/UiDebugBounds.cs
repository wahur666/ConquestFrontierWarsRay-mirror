using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

public static class UiDebugBounds {
	public static bool Enabled { get; set; }

	private static readonly Color InputOutline = new(0, 255, 255, 220);
	private static readonly Color TextOutline = new(255, 220, 0, 220);
	private static readonly Color LabelFill = new(0, 0, 0, 180);
	private static readonly Color LabelText = new(255, 255, 255, 255);

	public static void DrawInput(Rectangle bounds, string label) {
		DrawBounds(bounds, label, InputOutline);
	}

	public static void DrawText(Rectangle bounds, string label = "UiText") {
		DrawBounds(bounds, label, TextOutline);
	}

	public static Rectangle Union(Rectangle first, Rectangle second) {
		var left = MathF.Min(first.X, second.X);
		var top = MathF.Min(first.Y, second.Y);
		var right = MathF.Max(first.X + first.Width, second.X + second.Width);
		var bottom = MathF.Max(first.Y + first.Height, second.Y + second.Height);
		return new Rectangle(left, top, right - left, bottom - top);
	}

	private static void DrawBounds(Rectangle bounds, string label, Color outline) {
		if (!Enabled || bounds.Width <= 0f || bounds.Height <= 0f) {
			return;
		}

		Raylib.DrawRectangleLinesEx(bounds, 1f, outline);
		DrawLabel(bounds, label, outline);
	}

	private static void DrawLabel(Rectangle bounds, string label, Color outline) {
		if (string.IsNullOrWhiteSpace(label)) {
			return;
		}

		const int fontSize = 10;
		var textSize = Raylib.MeasureTextEx(Raylib.GetFontDefault(), label, fontSize, 0f);
		var labelBounds = new Rectangle(
			bounds.X,
			MathF.Max(0f, bounds.Y - fontSize - 3f),
			textSize.X + 4f,
			fontSize + 3f);

		Raylib.DrawRectangleRec(labelBounds, LabelFill);
		Raylib.DrawRectangleLinesEx(labelBounds, 1f, outline);
		Raylib.DrawTextEx(
			Raylib.GetFontDefault(),
			label,
			new Vector2(labelBounds.X + 2f, labelBounds.Y + 1f),
			fontSize,
			0f,
			LabelText);
	}
}
