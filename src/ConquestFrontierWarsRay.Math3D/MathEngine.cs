using System.Numerics;

namespace Math3D;

public interface I3DMathEngine {
	bool TryInverse(Matrix3 matrix, out Matrix3 inverse);
	Matrix3 Scale(Matrix3 matrix, float scalar);
	float Determinant(Matrix3 matrix);
	Matrix3 Multiply(Matrix3 left, Matrix3 right);
	Transform3 Multiply(Transform3 left, Transform3 right);
	Vector3 Transform(Matrix3 matrix, Vector3 vector);
	Vector3 Transform(Transform3 transform, Vector3 vector);
	Vector3 Rotate(Transform3 transform, Vector3 vector);
	Vector3 InverseRotate(Transform3 transform, Vector3 vector);
	Vector3 TransposeTransform(Matrix3 matrix, Vector3 vector);
	Vector3 InverseTransform(Transform3 transform, Vector3 vector);
	void TransformList(Span<Vector3> destination, Matrix3 matrix, ReadOnlySpan<Vector3> source);
	void TransformList(Span<Vector3> destination, Transform3 transform, ReadOnlySpan<Vector3> source);
	void TransposeTransformList(Span<Vector3> destination, Matrix3 matrix, ReadOnlySpan<Vector3> source);
	void InverseTransformList(Span<Vector3> destination, Transform3 transform, ReadOnlySpan<Vector3> source);
	Quaternion MatrixToQuaternion(Matrix3 matrix);
	Matrix3 QuaternionToMatrix(Quaternion quaternion);
	Vector3 Transform(Quaternion quaternion, Vector3 vector);
	Quaternion Multiply(Quaternion left, Quaternion right);
	Quaternion Slerp(Quaternion start, Quaternion end, float amount);
	bool TryGeneralInverse(Transform3 transform, out Transform3 inverse, out float w);
	float InvSqrt(float x);
	float Sqrt(float x);
}

public sealed class NumericsMathEngine : I3DMathEngine {
	public bool TryInverse(Matrix3 matrix, out Matrix3 inverse) {
		if (!Matrix4x4.Invert(matrix.ToNumericsMatrix4x4(), out var numericsInverse)) {
			inverse = default;
			return false;
		}

		inverse = Matrix3.FromNumericsMatrix4x4(numericsInverse);
		return true;
	}

	public Matrix3 Scale(Matrix3 matrix, float scalar) => matrix.Scale(scalar);

	public float Determinant(Matrix3 matrix) => matrix.ToNumericsMatrix4x4().GetDeterminant();

	public Matrix3 Multiply(Matrix3 left, Matrix3 right) =>
		Matrix3.FromNumericsMatrix4x4(right.ToNumericsMatrix4x4() * left.ToNumericsMatrix4x4());

	public Transform3 Multiply(Transform3 left, Transform3 right) =>
		Transform3.FromNumericsMatrix4x4(right.ToNumericsMatrix4x4() * left.ToNumericsMatrix4x4());

	public Vector3 Transform(Matrix3 matrix, Vector3 vector) => matrix.Transform(vector);

	public Vector3 Transform(Transform3 transform, Vector3 vector) => transform.Transform(vector);

	public Vector3 Rotate(Transform3 transform, Vector3 vector) => transform.Rotate(vector);

	public Vector3 InverseRotate(Transform3 transform, Vector3 vector) => transform.InverseRotate(vector);

	public Vector3 TransposeTransform(Matrix3 matrix, Vector3 vector) => matrix.TransposeTransform(vector);

	public Vector3 InverseTransform(Transform3 transform, Vector3 vector) => transform.InverseTransform(vector);

	public void TransformList(Span<Vector3> destination, Matrix3 matrix, ReadOnlySpan<Vector3> source) {
		TransformList(destination, source, matrix.Transform);
	}

	public void TransformList(Span<Vector3> destination, Transform3 transform, ReadOnlySpan<Vector3> source) {
		TransformList(destination, source, transform.Transform);
	}

	public void TransposeTransformList(Span<Vector3> destination, Matrix3 matrix, ReadOnlySpan<Vector3> source) {
		TransformList(destination, source, matrix.TransposeTransform);
	}

	public void InverseTransformList(Span<Vector3> destination, Transform3 transform, ReadOnlySpan<Vector3> source) {
		TransformList(destination, source, transform.InverseTransform);
	}

	public Quaternion MatrixToQuaternion(Matrix3 matrix) {
		var quaternion = Quaternion.CreateFromRotationMatrix(matrix.ToNumericsMatrix4x4());
		return Quaternion.Normalize(quaternion);
	}

	public Matrix3 QuaternionToMatrix(Quaternion quaternion) => Matrix3.CreateFromQuaternion(Quaternion.Normalize(quaternion));

	public Vector3 Transform(Quaternion quaternion, Vector3 vector) =>
		Vector3.Transform(vector, Quaternion.Normalize(quaternion));

	public Quaternion Multiply(Quaternion left, Quaternion right) =>
		Quaternion.Normalize(Quaternion.Multiply(left, right));

	public Quaternion Slerp(Quaternion start, Quaternion end, float amount) =>
		Quaternion.Normalize(Quaternion.Slerp(start, end, amount));

	public bool TryGeneralInverse(Transform3 transform, out Transform3 inverse, out float w) {
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

	public float InvSqrt(float x) => 1f / MathF.Sqrt(x);

	public float Sqrt(float x) => MathF.Sqrt(x);

	private static void TransformList(Span<Vector3> destination, ReadOnlySpan<Vector3> source, Func<Vector3, Vector3> transform) {
		if (destination.Length < source.Length) {
			throw new ArgumentException("Destination span is shorter than the source span.", nameof(destination));
		}

		for (var index = 0; index < source.Length; index++) {
			destination[index] = transform(source[index]);
		}
	}
}
