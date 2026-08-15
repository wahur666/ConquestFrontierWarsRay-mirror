using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Draws a text label inside the 2D node hierarchy.
/// </summary>
public sealed class TextNode : Control {
	public TextNode(string? name = null) : base(name) {
	}

	public string Text { get; set; } = string.Empty;
	public float FontSize { get; set; } = 20f;
	public Color Tint { get; set; } = Color.RayWhite;
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;

	protected override void Draw() {
		if (string.IsNullOrEmpty(Text)) {
			return;
		}

		var position = GlobalPosition;
		UiText.Draw(Text, position.X, position.Y, FontSize, Tint, TextStyle);
	}
}
