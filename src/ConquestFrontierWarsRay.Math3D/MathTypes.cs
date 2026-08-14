using System.Numerics;

namespace Math3D;

public enum RotationAxis : uint {
	X = 0,
	Y = 1,
	Z = 2,
	Pitch = X,
	Yaw = Y,
	Roll = Z
}

public readonly record struct PersistVector(float X, float Y, float Z) {
	public Vector3 ToVector3() => new(X, Y, Z);

	public static PersistVector FromVector3(Vector3 value) => new(value.X, value.Y, value.Z);
}

public readonly record struct PersistMatrix(
	float E00,
	float E01,
	float E02,
	float E10,
	float E11,
	float E12,
	float E20,
	float E21,
	float E22) {
	public Matrix3 ToMatrix3() => new(
		E00, E01, E02,
		E10, E11, E12,
		E20, E21, E22);

	public static PersistMatrix FromMatrix3(Matrix3 value) => new(
		value.M11, value.M12, value.M13,
		value.M21, value.M22, value.M23,
		value.M31, value.M32, value.M33);
}

public readonly record struct PersistTransform(PersistMatrix Matrix, PersistVector Translation) {
	public Transform3 ToTransform3() => new(Matrix.ToMatrix3(), Translation.ToVector3());

	public static PersistTransform FromTransform3(Transform3 value) =>
		new(PersistMatrix.FromMatrix3(value.Orientation), PersistVector.FromVector3(value.Translation));
}

public readonly record struct PersistQuaternion(float W, float X, float Y, float Z) {
	public Quaternion ToQuaternion() => new(X, Y, Z, W);

	public static PersistQuaternion FromQuaternion(Quaternion value) => new(value.W, value.X, value.Y, value.Z);
}

public readonly record struct Matrix3(
	float M11,
	float M12,
	float M13,
	float M21,
	float M22,
	float M23,
	float M31,
	float M32,
	float M33) {
	public static Matrix3 Identity { get; } = new(
		1f, 0f, 0f,
		0f, 1f, 0f,
		0f, 0f, 1f);

	public static Matrix3 Zero { get; } = default;

	public Matrix3 Transpose() => new(
		M11, M21, M31,
		M12, M22, M32,
		M13, M23, M33);

	public Vector3 Transform(Vector3 value) => Vector3.TransformNormal(value, ToNumericsMatrix4x4());

	public Vector3 TransposeTransform(Vector3 value) => Vector3.TransformNormal(value, Transpose().ToNumericsMatrix4x4());

	public Matrix3 Scale(float scalar) => new(
		M11 * scalar, M12 * scalar, M13 * scalar,
		M21 * scalar, M22 * scalar, M23 * scalar,
		M31 * scalar, M32 * scalar, M33 * scalar);

	public Matrix4x4 ToNumericsMatrix4x4() => new(
		M11, M21, M31, 0f,
		M12, M22, M32, 0f,
		M13, M23, M33, 0f,
		0f, 0f, 0f, 1f);

	public static Matrix3 FromNumericsMatrix4x4(Matrix4x4 value) => new(
		value.M11, value.M21, value.M31,
		value.M12, value.M22, value.M32,
		value.M13, value.M23, value.M33);

	public static Matrix3 CreateFromQuaternion(Quaternion quaternion) =>
		FromNumericsMatrix4x4(Matrix4x4.CreateFromQuaternion(quaternion));

	public static Matrix3 CreateRotationX(float radians) =>
		FromNumericsMatrix4x4(Matrix4x4.CreateRotationX(radians));

	public static Matrix3 CreateRotationY(float radians) =>
		FromNumericsMatrix4x4(Matrix4x4.CreateRotationY(radians));

	public static Matrix3 CreateRotationZ(float radians) =>
		FromNumericsMatrix4x4(Matrix4x4.CreateRotationZ(radians));
}

public readonly record struct Transform3(Matrix3 Orientation, Vector3 Translation) {
	public static Transform3 Identity { get; } = new(Matrix3.Identity, Vector3.Zero);

	public Vector3 Rotate(Vector3 value) => Orientation.Transform(value);

	public Vector3 Transform(Vector3 value) => Vector3.Transform(value, ToNumericsMatrix4x4());

	public Vector3 InverseRotate(Vector3 value) => Orientation.TransposeTransform(value);

	public Vector3 InverseTransform(Vector3 value) {
		var translated = value - Translation;
		return Orientation.TransposeTransform(translated);
	}

	public Matrix4x4 ToNumericsMatrix4x4() {
		var orientation = Orientation.ToNumericsMatrix4x4();
		orientation.M41 = Translation.X;
		orientation.M42 = Translation.Y;
		orientation.M43 = Translation.Z;
		return orientation;
	}

	public static Transform3 FromNumericsMatrix4x4(Matrix4x4 value) =>
		new(Matrix3.FromNumericsMatrix4x4(value), new Vector3(value.M41, value.M42, value.M43));
}
