using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.SceneGraph;

/// <summary>
///     Base node for screen-space UI elements.
///     Supports position inheritance plus a local size for layout and hit testing.
/// </summary>
public class Control : Node2D {
	public Vector2 Size { get; set; }

	public Rectangle GlobalBounds
		=> new(GlobalPosition.X, GlobalPosition.Y, Size.X, Size.Y);

	public bool ContainsPoint(Vector2 point) {
		return Raylib.CheckCollisionPointRec(point, GlobalBounds);
	}
}
