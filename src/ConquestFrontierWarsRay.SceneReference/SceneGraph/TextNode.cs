using ConquestFrontierWarsRay.SceneReference.Core;
using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.SceneGraph;

/// <summary>
///     Draws a text label that participates in the scenegraph transform hierarchy.
/// </summary>
public sealed class TextNode : Control {
	public string Text { get; set; } = string.Empty;

	public int FontSize { get; set; } = 20;

	public Color Tint { get; set; } = Color.RayWhite;

	protected override void Draw() {
		if (string.IsNullOrEmpty(Text)) {
			return;
		}

		var position = GlobalPosition;
		CombatUiAssets.DrawText(Text, position.X, position.Y, FontSize, Tint);
	}
}
