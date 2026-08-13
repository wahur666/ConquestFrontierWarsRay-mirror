using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.SceneGraph;

/// <summary>
///     A drawable texture node. Position, rotation, scale, and origin are inherited from Node2D.
/// </summary>
public sealed class Sprite : Node2D, IDisposable {
	private const float Rad2Deg = 180f / MathF.PI;

	private readonly Texture2D _texture;
	private bool _disposed;

	public Sprite(string texturePath) {
		_texture = Raylib.LoadTexture(texturePath);
		SourceRectangle = new Rectangle(0, 0, _texture.Width, _texture.Height);
	}

	public Rectangle SourceRectangle { get; set; }

	public Color Tint { get; set; } = Color.White;

	public Vector2 Size {
		get => new(SourceRectangle.Width * Scale.X, SourceRectangle.Height * Scale.Y);
		set =>
			Scale = new Vector2(
				SourceRectangle.Width == 0 ? 0 : value.X / SourceRectangle.Width,
				SourceRectangle.Height == 0 ? 0 : value.Y / SourceRectangle.Height);
	}

	public void Dispose() {
		if (_disposed) {
			return;
		}

		Raylib.UnloadTexture(_texture);
		_disposed = true;
	}

	/// <summary>
	///     Sets the sprite origin in source texture pixels.
	///     Position will be anchored to this point when the sprite is drawn.
	/// </summary>
	public void SetOrigin(float x, float y) {
		Origin = new Vector2(x, y);
	}

	/// <summary>
	///     Sets the sprite origin in source texture pixels.
	///     Position will be anchored to this point when the sprite is drawn.
	/// </summary>
	public void SetOrigin(Vector2 origin) {
		Origin = origin;
	}

	/// <summary>
	///     Sets the origin using normalized coordinates inside the current source rectangle.
	///     (0, 0) is top-left, (0.5, 0.5) is center, (1, 1) is bottom-right.
	/// </summary>
	public void SetOriginNormalized(float x, float y) {
		Origin = new Vector2(
			SourceRectangle.Width * x,
			SourceRectangle.Height * y);
	}

	public void SetOriginTopLeft() {
		Origin = Vector2.Zero;
	}

	public void SetOriginTopCenter() {
		SetOriginNormalized(0.5f, 0f);
	}

	public void SetOriginTopRight() {
		SetOriginNormalized(1f, 0f);
	}

	public void SetOriginCenter() {
		SetOriginNormalized(0.5f, 0.5f);
	}

	public void SetOriginBottomLeft() {
		SetOriginNormalized(0f, 1f);
	}

	public void SetOriginBottomCenter() {
		SetOriginNormalized(0.5f, 1f);
	}

	public void SetOriginBottomRight() {
		SetOriginNormalized(1f, 1f);
	}

	protected override void Draw() {
		if (_disposed) {
			return;
		}

		DecomposeTransform(GlobalTransform, out var scale, out var rotation, out var translation);
		var pivot = Vector2.Transform(Origin, GlobalTransform);

		Raylib.DrawTexturePro(
			_texture,
			SourceRectangle,
			new Rectangle(pivot.X, pivot.Y, SourceRectangle.Width * scale.X, SourceRectangle.Height * scale.Y),
			Origin * scale,
			rotation * Rad2Deg,
			Tint);
	}

	private static void DecomposeTransform(
		Matrix3x2 matrix,
		out Vector2 scale,
		out float rotation,
		out Vector2 translation) {
		translation = new Vector2(matrix.M31, matrix.M32);
		scale = new Vector2(
			MathF.Sqrt((matrix.M11 * matrix.M11) + (matrix.M12 * matrix.M12)),
			MathF.Sqrt((matrix.M21 * matrix.M21) + (matrix.M22 * matrix.M22)));

		if (matrix.GetDeterminant() < 0) {
			scale.X = -scale.X;
		}

		rotation = MathF.Atan2(matrix.M12, matrix.M11);
	}
}
