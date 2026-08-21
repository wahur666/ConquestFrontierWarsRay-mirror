using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Texture-backed 2D node that renders through the framework texture abstraction.
/// </summary>
public sealed class Sprite : Node2D {
	private Texture2D _texture;

	/// <summary>
	/// Creates a sprite with one texture source.
	/// </summary>
	public Sprite(Texture2D texture, string? name = null) : base(name) {
		_texture = texture ?? throw new ArgumentNullException(nameof(texture));
	}

	/// <summary>
	/// Creates a sprite from one indexed frame in a shared frame collection.
	/// </summary>
	public Sprite(SpriteFrames frames, int frameIndex = 0, string? name = null) : base(name) {
		ArgumentNullException.ThrowIfNull(frames);
		_texture = frames.GetFrameTexture(frameIndex);
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
	/// Mirrors the sprite horizontally when drawing.
	/// </summary>
	public bool FlipH { get; set; }

	/// <summary>
	/// Mirrors the sprite vertically when drawing.
	/// </summary>
	public bool FlipV { get; set; }

	/// <summary>
	/// Replaces the current texture source.
	/// </summary>
	public void SetTexture(Texture2D texture) {
		_texture = texture ?? throw new ArgumentNullException(nameof(texture));
	}

	/// <summary>
	/// Replaces the current texture source with one indexed frame from a shared frame collection.
	/// </summary>
	public void SetFrame(SpriteFrames frames, int frameIndex) {
		ArgumentNullException.ThrowIfNull(frames);
		_texture = frames.GetFrameTexture(frameIndex);
	}

	protected override void OnDraw() {
		var slice = _texture.GetSlice();
		var source = slice.Source;

		if (FlipH) {
			source.X += source.Width;
			source.Width = -source.Width;
		}

		if (FlipV) {
			source.Y += source.Height;
			source.Height = -source.Height;
		}

		var globalScale = GlobalScale;
		var destinationSize = new Vector2(slice.Source.Width * globalScale.X, slice.Source.Height * globalScale.Y);
		var destination = new Rectangle(
			GlobalPosition.X,
			GlobalPosition.Y,
			destinationSize.X,
			destinationSize.Y);
		var origin = new Vector2(destinationSize.X * Pivot.X, destinationSize.Y * Pivot.Y);
		var rotationDegrees = GlobalRotation * (180f / MathF.PI);

		Raylib.DrawTexturePro(slice.Texture, source, destination, origin, rotationDegrees, Modulate);
	}
}
