using System.Numerics;

namespace ConquestFrontierWarsRay.SceneReference.SceneGraph;

/// <summary>
///     A simple 2D transform node for the lightweight screen graph.
/// </summary>
public class Node2D : Node {
	public Vector2 Position { get; set; }

	public float Rotation { get; set; }

	public Vector2 Scale { get; set; } = Vector2.One;

	public Vector2 Origin { get; set; }

	public Matrix3x2 LocalTransform =>
		Matrix3x2.CreateTranslation(-Origin) *
		Matrix3x2.CreateScale(Scale) *
		Matrix3x2.CreateRotation(Rotation) *
		Matrix3x2.CreateTranslation(Position);

	public Matrix3x2 GlobalTransform => Parent is null
		? LocalTransform
		: LocalTransform * GetParentGlobalTransform();

	public Vector2 GlobalPosition => Vector2.Transform(Vector2.Zero, GlobalTransform);

	private Matrix3x2 GetParentGlobalTransform() {
		for (var ancestor = Parent; ancestor is not null; ancestor = ancestor.Parent) {
			if (ancestor is Node2D node2D) {
				return node2D.GlobalTransform;
			}
		}

		return Matrix3x2.Identity;
	}
}
