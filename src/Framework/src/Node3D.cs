using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// 3D transform node for scene-space hierarchy composition.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Node3D"/> adds local position, quaternion rotation, and scale on
/// top of <see cref="Node"/>, then exposes both local and parent-composed global
/// transforms through <see cref="Transform3D"/> and matrix views.
/// </para>
/// <para>
/// The current 3D stack is primarily transform-oriented. Rendering behavior is
/// provided by specialized descendants such as <see cref="Camera3DNode"/> and
/// <see cref="Light3D"/>.
/// </para>
/// </remarks>
public class Node3D : Node {
	/// <summary>
	/// Creates a 3D node.
	/// </summary>
	public Node3D(string? name = null) : base(name) {
	}

	/// <summary>
	/// Local position relative to the parent.
	/// </summary>
	public Vector3 Position { get; set; } = Vector3.Zero;

	/// <summary>
	/// Local rotation relative to the parent.
	/// </summary>
	public Quaternion Rotation { get; set; } = Quaternion.Identity;

	/// <summary>
	/// Local scale relative to the parent.
	/// </summary>
	public Vector3 Scale { get; set; } = Vector3.One;

	/// <summary>
	/// Combined local transform built from scale, rotation, and position.
	/// </summary>
	public Matrix4x4 LocalTransform => LocalTransform3D.Matrix;

	/// <summary>
	/// Local transform value built from scale, rotation, and position.
	/// </summary>
	public Transform3D LocalTransform3D => new(Position, Rotation, Scale);

	/// <summary>
	/// Full transform after parent transforms are applied.
	/// </summary>
	public Matrix4x4 GlobalTransform => GlobalTransform3D.Matrix;

	/// <summary>
	/// Full transform value after parent transforms are applied.
	/// </summary>
	public Transform3D GlobalTransform3D =>
		Parent is Node3D parentNode3D
			? LocalTransform3D.Compose(parentNode3D.GlobalTransform3D)
			: LocalTransform3D;

	/// <summary>
	/// World position of this node.
	/// </summary>
	public Vector3 GlobalPosition => GlobalTransform3D.Position;

	/// <summary>
	/// World rotation after parent rotation is applied.
	/// </summary>
	public Quaternion GlobalRotation => GlobalTransform3D.Rotation;

	/// <summary>
	/// World scale after parent scale is applied.
	/// </summary>
	public Vector3 GlobalScale => GlobalTransform3D.Scale;
}
