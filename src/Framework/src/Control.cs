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

	public Rectangle GlobalBounds => new(GlobalPosition.X, GlobalPosition.Y, Size.X, Size.Y);

	public bool ContainsPoint(Vector2 point) {
		return Raylib.CheckCollisionPointRec(point, GlobalBounds);
	}
}
