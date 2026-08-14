using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Value type that owns 2D transform composition and decomposition.
/// </summary>
public readonly struct Transform2D {
	private readonly Matrix3x2 _matrix;

	/// <summary>
	/// Creates a transform from position, rotation, and scale.
	/// </summary>
	public Transform2D(Vector2 position, float rotation, Vector2 scale) {
		_matrix =
			Matrix3x2.CreateScale(scale) *
			Matrix3x2.CreateRotation(rotation) *
			Matrix3x2.CreateTranslation(position);
	}

	private Transform2D(Matrix3x2 matrix) {
		_matrix = matrix;
	}

	/// <summary>
	/// Identity 2D transform.
	/// </summary>
	public static Transform2D Identity { get; } = new(Matrix3x2.Identity);

	/// <summary>
	/// Combined matrix built from scale, rotation, and translation.
	/// </summary>
	public Matrix3x2 Matrix => _matrix;

	/// <summary>
	/// Position extracted from the matrix translation.
	/// </summary>
	public Vector2 Position => new(_matrix.M31, _matrix.M32);

	/// <summary>
	/// Rotation extracted from the transformed X basis.
	/// </summary>
	public float Rotation {
		get {
			var xBasis = XBasis;
			return MathF.Atan2(xBasis.Y, xBasis.X);
		}
	}

	/// <summary>
	/// Scale extracted from the transformed basis lengths.
	/// </summary>
	public Vector2 Scale => new(XBasis.Length(), YBasis.Length());

	/// <summary>
	/// X basis after scale and rotation are applied.
	/// </summary>
	public Vector2 XBasis => new(_matrix.M11, _matrix.M12);

	/// <summary>
	/// Y basis after scale and rotation are applied.
	/// </summary>
	public Vector2 YBasis => new(_matrix.M21, _matrix.M22);

	/// <summary>
	/// Composes this local transform under a parent transform.
	/// </summary>
	public Transform2D Compose(Transform2D parent) {
		return new Transform2D(_matrix * parent._matrix);
	}

	/// <summary>
	/// Transforms one local-space point into the transform space.
	/// </summary>
	public Vector2 TransformPoint(Vector2 point) {
		return Vector2.Transform(point, _matrix);
	}

	/// <summary>
	/// Rebuilds a transform from an existing matrix.
	/// </summary>
	public static Transform2D FromMatrix(Matrix3x2 matrix) {
		return new Transform2D(matrix);
	}
}
