using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Value type that owns 3D transform composition and decomposition.
/// </summary>
public readonly struct Transform3D {
	private readonly Matrix4x4 _matrix;

	/// <summary>
	/// Creates a transform from position, rotation, and scale.
	/// </summary>
	public Transform3D(Vector3 position, Quaternion rotation, Vector3 scale) {
		_matrix =
			Matrix4x4.CreateScale(scale) *
			Matrix4x4.CreateFromQuaternion(NormalizeOrIdentity(rotation)) *
			Matrix4x4.CreateTranslation(position);
	}

	private Transform3D(Matrix4x4 matrix) {
		_matrix = matrix;
	}

	/// <summary>
	/// Identity 3D transform.
	/// </summary>
	public static Transform3D Identity { get; } = new(Matrix4x4.Identity);

	/// <summary>
	/// Combined matrix built from scale, rotation, and translation.
	/// </summary>
	public Matrix4x4 Matrix => _matrix;

	/// <summary>
	/// Position extracted from the matrix translation.
	/// </summary>
	public Vector3 Position => new(_matrix.M41, _matrix.M42, _matrix.M43);

	/// <summary>
	/// Rotation extracted from the matrix decomposition.
	/// </summary>
	public Quaternion Rotation => ExtractRotation();

	/// <summary>
	/// Scale extracted from the matrix decomposition.
	/// </summary>
	public Vector3 Scale => new(XBasis.Length(), YBasis.Length(), ZBasis.Length());

	/// <summary>
	/// X basis after scale and rotation are applied.
	/// </summary>
	public Vector3 XBasis => new(_matrix.M11, _matrix.M12, _matrix.M13);

	/// <summary>
	/// Y basis after scale and rotation are applied.
	/// </summary>
	public Vector3 YBasis => new(_matrix.M21, _matrix.M22, _matrix.M23);

	/// <summary>
	/// Z basis after scale and rotation are applied.
	/// </summary>
	public Vector3 ZBasis => new(_matrix.M31, _matrix.M32, _matrix.M33);

	/// <summary>
	/// Composes this local transform under a parent transform.
	/// </summary>
	public Transform3D Compose(Transform3D parent) {
		return new Transform3D(_matrix * parent._matrix);
	}

	/// <summary>
	/// Transforms one local-space point into the transform space.
	/// </summary>
	public Vector3 TransformPoint(Vector3 point) {
		return Vector3.Transform(point, _matrix);
	}

	/// <summary>
	/// Rebuilds a transform from an existing matrix.
	/// </summary>
	public static Transform3D FromMatrix(Matrix4x4 matrix) {
		return new Transform3D(matrix);
	}

	private Quaternion ExtractRotation() {
		const float epsilon = 0.000001f;

		var x = NormalizeOrFallback(XBasis, Vector3.UnitX, epsilon);
		var y = YBasis - Vector3.Dot(YBasis, x) * x;
		y = NormalizeOrFallback(y, GetPerpendicularUnit(x), epsilon);
		var z = Vector3.Cross(x, y);
		z = NormalizeOrFallback(z, Vector3.Cross(x, GetPerpendicularUnit(x)), epsilon);
		y = NormalizeOrFallback(Vector3.Cross(z, x), y, epsilon);

		var rotationMatrix = new Matrix4x4(
			x.X, x.Y, x.Z, 0f,
			y.X, y.Y, y.Z, 0f,
			z.X, z.Y, z.Z, 0f,
			0f, 0f, 0f, 1f);

		return NormalizeOrIdentity(Quaternion.CreateFromRotationMatrix(rotationMatrix));
	}

	private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback, float epsilon) {
		if (value.LengthSquared() > epsilon) {
			return Vector3.Normalize(value);
		}

		return Vector3.Normalize(fallback);
	}

	private static Vector3 GetPerpendicularUnit(Vector3 value) {
		var axis = MathF.Abs(Vector3.Dot(value, Vector3.UnitY)) < 0.999f
			? Vector3.UnitY
			: Vector3.UnitX;
		return Vector3.Normalize(Vector3.Cross(axis, value));
	}

	private static Quaternion NormalizeOrIdentity(Quaternion value) {
		return value.LengthSquared() > 0.000001f
			? Quaternion.Normalize(value)
			: Quaternion.Identity;
	}
}
