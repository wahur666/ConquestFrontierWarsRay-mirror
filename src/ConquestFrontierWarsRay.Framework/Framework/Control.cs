using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Base node for simple screen-space UI elements with bounds.
/// </summary>
internal class Control : Node2D {
	public Control(string? name = null) : base(name) {
	}

	public Vector2 Size { get; set; }

	public Rectangle GlobalBounds => new(GlobalPosition.X, GlobalPosition.Y, Size.X, Size.Y);

	public bool ContainsPoint(Vector2 point) {
		return Raylib.CheckCollisionPointRec(point, GlobalBounds);
	}
}
