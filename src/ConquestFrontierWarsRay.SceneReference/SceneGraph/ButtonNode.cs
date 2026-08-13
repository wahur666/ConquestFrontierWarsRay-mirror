using ConquestFrontierWarsRay.SceneReference.Core;
using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.SceneGraph;

/// <summary>
///     A reusable clickable button node with hover feedback.
/// </summary>
public sealed class ButtonNode : Control {
	public string Text { get; set; } = string.Empty;

	public int FontSize { get; set; } = 14;

	public Color Fill { get; set; } = new(34, 42, 52, 255);

	public Color HoverFill { get; set; } = new(54, 67, 82, 255);

	public Color Outline { get; set; } = new(140, 160, 180, 200);

	public Color TextColor { get; set; } = Color.RayWhite;

	public bool HandleInput() {
		if (!Visible) {
			return false;
		}

		if (!Raylib.IsMouseButtonPressed(MouseButton.Left)) {
			return false;
		}

		return ContainsPoint(Raylib.GetMousePosition());
	}

	protected override void Draw() {
		if (Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		var fill = IsHovered() ? HoverFill : Fill;

		Raylib.DrawRectangleRec(bounds, fill);
		Raylib.DrawRectangleLinesEx(bounds, 1, Outline);

		if (string.IsNullOrEmpty(Text)) {
			return;
		}

		var textWidth = CombatUiAssets.MeasureText(Text, FontSize);
		var textX = (int)(bounds.X + ((bounds.Width - textWidth) / 2f));
		var textY = (int)(bounds.Y + ((bounds.Height - FontSize) / 2f)) - 1;
		CombatUiAssets.DrawText(Text, textX, textY, FontSize, TextColor);
	}

	private bool IsHovered() {
		return ContainsPoint(Raylib.GetMousePosition());
	}
}
