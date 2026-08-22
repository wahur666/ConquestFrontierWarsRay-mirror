using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Screen-space 2D node with rectangular size and hit-testing.
/// </summary>
/// <remarks>
/// <see cref="Control"/> is the common base for lightweight framework UI nodes
/// such as <see cref="PanelNode"/>, <see cref="TextNode"/>, <see cref="ButtonNode"/>,
/// <see cref="DropdownNode"/>, <see cref="SliderNode"/>, <see cref="ListViewNode"/>,
/// and <see cref="VideoPlayer"/>.
/// </remarks>
public class Control : Node2D {
	public Control(string? name = null) : base(name) {
	}

	public Vector2 Size { get; set; }

	/// <summary>
	/// Normalized anchor used when converting position plus size into screen bounds.
	/// </summary>
	public Vector2 Pivot { get; set; } = Vector2.Zero;

	/// <summary>
	/// Final bounds after world position, scale, and pivot are applied.
	/// </summary>
	public Rectangle GlobalBounds {
		get {
			var globalScale = GlobalScale;
			var scaledSize = new Vector2(
				MathF.Abs(Size.X * globalScale.X),
				MathF.Abs(Size.Y * globalScale.Y));

			return new Rectangle(
				GlobalPosition.X - (scaledSize.X * Pivot.X),
				GlobalPosition.Y - (scaledSize.Y * Pivot.Y),
				scaledSize.X,
				scaledSize.Y);
		}
	}

	public bool ContainsPoint(Vector2 point) {
		return Raylib.CheckCollisionPointRec(point, GlobalBounds);
	}
}
