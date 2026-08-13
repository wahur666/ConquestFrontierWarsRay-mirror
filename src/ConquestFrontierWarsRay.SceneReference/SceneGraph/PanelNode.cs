using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.SceneGraph;

/// <summary>
///     Draws a filled rectangle with an outline and supports screen-graph layout.
/// </summary>
public sealed class PanelNode : Control {
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
