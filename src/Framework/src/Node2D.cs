using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// 2D transform node for the canvas hierarchy.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Node2D"/> adds local position, rotation, and scale on top of
/// <see cref="CanvasItem"/>, then exposes both local and parent-composed global
/// transforms through <see cref="Transform2D"/> and matrix views.
/// </para>
/// <para>
/// Derived nodes override <see cref="Draw"/> to render in local space while the
/// framework applies the composed canvas transform automatically.
/// </para>
/// </remarks>
public class Node2D : CanvasItem {
	/// <summary>
	/// Creates a 2D node.
	/// </summary>
	public Node2D(string? name = null) : base(name) {
	}

	/// <summary>
	/// Local position relative to the parent.
	/// </summary>
	public Vector2 Position { get; set; } = Vector2.Zero;

	/// <summary>
	/// Local rotation in radians.
	/// </summary>
	public float Rotation { get; set; }

	/// <summary>
	/// Local scale relative to the parent.
	/// </summary>
	public Vector2 Scale { get; set; } = Vector2.One;

	/// <summary>
	/// Combined local transform built from scale, rotation, and position.
	/// </summary>
	public Matrix3x2 LocalTransform => LocalTransform2D.Matrix;

	/// <summary>
	/// Local transform value built from scale, rotation, and position.
	/// </summary>
	public Transform2D LocalTransform2D => new(Position, Rotation, Scale);

	/// <summary>
	/// Full transform after parent transforms are applied.
	/// </summary>
	public Matrix3x2 GlobalTransform => GlobalTransform2D.Matrix;

	/// <summary>
	/// Full transform value after parent transforms are applied.
	/// </summary>
	public Transform2D GlobalTransform2D =>
		Parent is Node2D parentNode2D
			? LocalTransform2D.Compose(parentNode2D.GlobalTransform2D)
			: LocalTransform2D;

	/// <summary>
	/// World position of this node.
	/// </summary>
	public Vector2 GlobalPosition => GlobalTransform2D.Position;

	/// <summary>
	/// World rotation in radians.
	/// </summary>
	public float GlobalRotation => GlobalTransform2D.Rotation;

	/// <summary>
	/// World scale after parent scale is applied.
	/// </summary>
	public Vector2 GlobalScale => GlobalTransform2D.Scale;

	protected override Matrix3x2 CanvasTransform => GlobalTransform;

	/// <summary>
	/// Draw hook for derived 2D nodes.
	/// </summary>
	protected virtual void Draw() {
	}

	protected override void OnDraw() {
		Draw();
	}
}
