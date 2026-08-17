using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Native-style hardpoint node with both socket transform and joint metadata.
/// </summary>
public sealed class Hardpoint3D : Socket3D {
	/// <summary>
	/// Creates a hardpoint node.
	/// </summary>
	public Hardpoint3D(string? name = null, string? groupName = null) : base(name, groupName) {
	}

	/// <summary>
	/// Connection behavior selected by the child hardpoint during attachment.
	/// </summary>
	public HardpointJointType JointType { get; set; } = HardpointJointType.Fixed;

	/// <summary>
	/// Unit axis of rotation or translation in local hardpoint orientation space.
	/// </summary>
	public Vector3 Axis { get; set; } = Vector3.UnitY;

	/// <summary>
	/// Minimum primary constraint value.
	/// </summary>
	public float Min0 { get; set; }

	/// <summary>
	/// Maximum primary constraint value.
	/// </summary>
	public float Max0 { get; set; }

	/// <summary>
	/// Optional spring constant from the native hardpoint model.
	/// </summary>
	public float SpringConstant { get; set; }

	/// <summary>
	/// Optional damping constant from the native hardpoint model.
	/// </summary>
	public float DampingConstant { get; set; }

	/// <summary>
	/// Optional rest length from the native hardpoint model.
	/// </summary>
	public float RestLength { get; set; }

	/// <summary>
	/// Converts this node into an immutable hardpoint data value.
	/// </summary>
	public HardpointInfo3D ToInfo() {
		return new HardpointInfo3D(
			JointType,
			Position,
			NormalizeOrIdentity(Rotation),
			NormalizeOrDefault(Axis, Vector3.Zero),
			Min0,
			Max0,
			SpringConstant,
			DampingConstant,
			RestLength);
	}

	/// <summary>
	/// Builds the native-style connection descriptor between this parent hardpoint and a child hardpoint.
	/// </summary>
	public HardpointConnectionDescriptor BuildConnectionTo(Hardpoint3D child) {
		ArgumentNullException.ThrowIfNull(child);

		var parentInfo = ToInfo();
		var childInfo = child.ToInfo();
		var parentOrientation = RotationMatrix(parentInfo.Orientation);
		var childOrientation = RotationMatrix(childInfo.Orientation);

		return childInfo.Type switch {
			HardpointJointType.Fixed => BuildFixed(parentInfo, childInfo, parentOrientation, childOrientation),
			HardpointJointType.Revolute or HardpointJointType.Prismatic => BuildAxisJoint(parentInfo, childInfo, parentOrientation, childOrientation),
			_ => new HardpointConnectionDescriptor(
				childInfo.Type,
				Vector3.Zero,
				Quaternion.Identity,
				parentInfo.Point,
				childInfo.Point,
				Vector3.Zero,
				childInfo.Min0,
				childInfo.Max0)
		};
	}

	private static HardpointConnectionDescriptor BuildFixed(
		HardpointInfo3D parent,
		HardpointInfo3D child,
		Matrix4x4 parentOrientation,
		Matrix4x4 childOrientation) {
		var relativeOrientationMatrix = parentOrientation * Matrix4x4.Transpose(childOrientation);
		var relativeOrientation = QuaternionFromRotationMatrix(relativeOrientationMatrix);
		var relativePosition = parent.Point - Vector3.Transform(child.Point, relativeOrientationMatrix);
		return new HardpointConnectionDescriptor(
			HardpointJointType.Fixed,
			relativePosition,
			relativeOrientation,
			parent.Point,
			child.Point,
			Vector3.Zero,
			child.Min0,
			child.Max0);
	}

	private static HardpointConnectionDescriptor BuildAxisJoint(
		HardpointInfo3D parent,
		HardpointInfo3D child,
		Matrix4x4 parentOrientation,
		Matrix4x4 childOrientation) {
		var relativeOrientationMatrix = parentOrientation * childOrientation;
		var axis = Vector3.TransformNormal(child.Axis, relativeOrientationMatrix);
		return new HardpointConnectionDescriptor(
			child.Type,
			Vector3.Zero,
			QuaternionFromRotationMatrix(relativeOrientationMatrix),
			parent.Point,
			child.Point,
			NormalizeOrDefault(axis, Vector3.Zero),
			child.Min0,
			child.Max0);
	}

	private static Matrix4x4 RotationMatrix(Quaternion rotation) {
		return Matrix4x4.CreateFromQuaternion(NormalizeOrIdentity(rotation));
	}

	private static Quaternion QuaternionFromRotationMatrix(Matrix4x4 matrix) {
		return NormalizeOrIdentity(Quaternion.CreateFromRotationMatrix(matrix));
	}

	private static Quaternion NormalizeOrIdentity(Quaternion value) {
		return value.LengthSquared() > 0.000001f
			? Quaternion.Normalize(value)
			: Quaternion.Identity;
	}

	private static Vector3 NormalizeOrDefault(Vector3 value, Vector3 fallback) {
		return value.LengthSquared() > 0.000001f
			? Vector3.Normalize(value)
			: fallback;
	}
}
