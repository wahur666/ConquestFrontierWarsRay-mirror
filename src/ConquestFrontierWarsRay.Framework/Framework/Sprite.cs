using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Draws a texture as a 2D node.
/// </summary>
internal sealed class Sprite : Node2D {
	private Texture2D _texture;

	/// <summary>
	/// Creates a sprite with one texture source.
	/// </summary>
	public Sprite(Texture2D texture, string? name = null) : base(name) {
		_texture = texture ?? throw new ArgumentNullException(nameof(texture));
	}

	/// <summary>
	/// Normalized pivot point used for rotation and placement.
	/// </summary>
	public Vector2 Pivot { get; set; } = new(0.5f, 0.5f);

	/// <summary>
	/// Tint color applied when drawing.
	/// </summary>
	public Color Modulate { get; set; } = Color.White;

	/// <summary>
	/// Replaces the current texture source.
	/// </summary>
	public void SetTexture(Texture2D texture) {
		_texture = texture ?? throw new ArgumentNullException(nameof(texture));
	}

	protected override void OnDraw() {
		var slice = _texture.GetSlice();

		var globalScale = GlobalScale;
		var destinationSize = new Vector2(slice.Source.Width * globalScale.X, slice.Source.Height * globalScale.Y);
		var destination = new Rectangle(
			GlobalPosition.X,
			GlobalPosition.Y,
			destinationSize.X,
			destinationSize.Y);
		var origin = new Vector2(destinationSize.X * Pivot.X, destinationSize.Y * Pivot.Y);
		var rotationDegrees = GlobalRotation * (180f / MathF.PI);

		Raylib.DrawTexturePro(slice.Texture, slice.Source, destination, origin, rotationDegrees, Modulate);
	}
}
