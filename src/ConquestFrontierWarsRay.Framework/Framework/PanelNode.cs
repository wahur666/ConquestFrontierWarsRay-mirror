using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Draws a filled rectangle panel with an optional outline.
/// </summary>
internal sealed class PanelNode : Control {
	public PanelNode(string? name = null) : base(name) {
	}

	public Color Fill { get; set; } = new(0, 0, 0, 200);
	public Color Outline { get; set; } = Color.White;
	public float OutlineThickness { get; set; } = 1f;

	protected override void Draw() {
		if (Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var rect = GlobalBounds;
		Raylib.DrawRectangleRec(rect, Fill);

		if (OutlineThickness > 0f) {
			Raylib.DrawRectangleLinesEx(rect, OutlineThickness, Outline);
		}
	}
}
