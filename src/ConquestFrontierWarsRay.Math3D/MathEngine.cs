using System.Numerics;

namespace Math3D;

public static class MathEngine {
	public static bool TryInverse(Matrix3 matrix, out Matrix3 inverse) {
		if (!Matrix4x4.Invert(matrix.ToNumericsMatrix4x4(), out var numericsInverse)) {
			inverse = default;
			return false;
		}

		inverse = Matrix3.FromNumericsMatrix4x4(numericsInverse);
		return true;
	}

	public static Matrix3 Scale(Matrix3 matrix, float scalar) => matrix.Scale(scalar);

	public static float Determinant(Matrix3 matrix) => matrix.ToNumericsMatrix4x4().GetDeterminant();

	public static Matrix3 Multiply(Matrix3 left, Matrix3 right) =>
		Matrix3.FromNumericsMatrix4x4(right.ToNumericsMatrix4x4() * left.ToNumericsMatrix4x4());

	public static Transform3 Multiply(Transform3 left, Transform3 right) =>
		Transform3.FromNumericsMatrix4x4(right.ToNumericsMatrix4x4() * left.ToNumericsMatrix4x4());

	public static Vector3 Transform(Matrix3 matrix, Vector3 vector) => matrix.Transform(vector);

	public static Vector3 Transform(Transform3 transform, Vector3 vector) => transform.Transform(vector);

	public static Vector3 Rotate(Transform3 transform, Vector3 vector) => transform.Rotate(vector);

	public static Vector3 InverseRotate(Transform3 transform, Vector3 vector) => transform.InverseRotate(vector);

	public static Vector3 TransposeTransform(Matrix3 matrix, Vector3 vector) => matrix.TransposeTransform(vector);

	public static Vector3 InverseTransform(Transform3 transform, Vector3 vector) => transform.InverseTransform(vector);

	public static void TransformList(Span<Vector3> destination, Matrix3 matrix, ReadOnlySpan<Vector3> source) {
		TransformList(destination, source, matrix.Transform);
	}

	public static void TransformList(Span<Vector3> destination, Transform3 transform, ReadOnlySpan<Vector3> source) {
		TransformList(destination, source, transform.Transform);
	}

	public static void TransposeTransformList(Span<Vector3> destination, Matrix3 matrix, ReadOnlySpan<Vector3> source) {
		TransformList(destination, source, matrix.TransposeTransform);
	}

	public static void InverseTransformList(Span<Vector3> destination, Transform3 transform, ReadOnlySpan<Vector3> source) {
		TransformList(destination, source, transform.InverseTransform);
	}

	public static Quaternion MatrixToQuaternion(Matrix3 matrix) {
		var quaternion = Quaternion.CreateFromRotationMatrix(matrix.ToNumericsMatrix4x4());
		return Quaternion.Normalize(quaternion);
	}

	public static Matrix3 QuaternionToMatrix(Quaternion quaternion) => Matrix3.CreateFromQuaternion(Quaternion.Normalize(quaternion));

	public static Vector3 Transform(Quaternion quaternion, Vector3 vector) =>
		Vector3.Transform(vector, Quaternion.Normalize(quaternion));

	public static Quaternion Multiply(Quaternion left, Quaternion right) =>
		Quaternion.Normalize(Quaternion.Multiply(left, right));

	public static Quaternion Slerp(Quaternion start, Quaternion end, float amount) =>
		Quaternion.Normalize(Quaternion.Slerp(start, end, amount));

	public static bool TryGeneralInverse(Transform3 transform, out Transform3 inverse, out float w) {
		var numericsTransform = transform.ToNumericsMatrix4x4();
		if (!Matrix4x4.Invert(numericsTransform, out var numericsInverse)) {
			inverse = default;
			w = numericsTransform.GetDeterminant();
			return false;
		}

		inverse = Transform3.FromNumericsMatrix4x4(numericsInverse);
		w = numericsInverse.M44;
		return true;
	}

	public static float InvSqrt(float x) => 1f / MathF.Sqrt(x);

	public static float Sqrt(float x) => MathF.Sqrt(x);

	private static void TransformList(Span<Vector3> destination, ReadOnlySpan<Vector3> source, Func<Vector3, Vector3> transform) {
		if (destination.Length < source.Length) {
			throw new ArgumentException("Destination span is shorter than the source span.", nameof(destination));
		}

		for (var index = 0; index < source.Length; index++) {
			destination[index] = transform(source[index]);
		}
	}
}
