using System.Numerics;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class Transform3DTests {
	[Fact]
	public void Matrix_CombinesScaleRotationAndTranslation() {
		var transform = new Transform3D(
			new Vector3(10f, 20f, 30f),
			Quaternion.CreateFromYawPitchRoll(0.4f, 0.2f, -0.3f),
			new Vector3(2f, 3f, 4f));

		var expected =
			Matrix4x4.CreateScale(transform.Scale) *
			Matrix4x4.CreateFromQuaternion(transform.Rotation) *
			Matrix4x4.CreateTranslation(transform.Position);

		AssertMatrixEqual(expected, transform.Matrix);
	}

	[Fact]
	public void Compose_UsesParentTransformRules() {
		var parent = new Transform3D(
			new Vector3(10f, 0f, -5f),
			Quaternion.CreateFromYawPitchRoll(0.6f, -0.1f, 0.25f),
			new Vector3(3f, 3f, 3f));
		var child = new Transform3D(
			new Vector3(5f, 1f, 2f),
			Quaternion.CreateFromYawPitchRoll(-0.3f, 0.4f, 0.1f),
			new Vector3(2f, 2f, 2f));

		var actual = child.Compose(parent);
		var expectedMatrix = child.Matrix * parent.Matrix;

		AssertMatrixEqual(expectedMatrix, actual.Matrix);
		AssertVectorEqual(Vector3.Transform(Vector3.Zero, expectedMatrix), actual.Position);
		AssertVectorEqual(
			new Vector3(
				new Vector3(expectedMatrix.M11, expectedMatrix.M12, expectedMatrix.M13).Length(),
				new Vector3(expectedMatrix.M21, expectedMatrix.M22, expectedMatrix.M23).Length(),
				new Vector3(expectedMatrix.M31, expectedMatrix.M32, expectedMatrix.M33).Length()),
			actual.Scale);
		AssertQuaternionEquivalent(DecomposeRotation(expectedMatrix), actual.Rotation);
	}

	[Fact]
	public void FromMatrix_DecomposesTransformComponents() {
		var expected = new Transform3D(
			new Vector3(-4f, 12f, 7f),
			Quaternion.CreateFromYawPitchRoll(0.35f, -0.15f, 0.55f),
			new Vector3(3f, 5f, 7f));

		var actual = Transform3D.FromMatrix(expected.Matrix);

		AssertVectorEqual(expected.Position, actual.Position);
		AssertVectorEqual(expected.Scale, actual.Scale);
		AssertQuaternionEquivalent(expected.Rotation, actual.Rotation);
	}

	private static Quaternion DecomposeRotation(Matrix4x4 matrix) {
		Matrix4x4.Decompose(matrix, out _, out var rotation, out _);
		return Quaternion.Normalize(rotation);
	}

	private static void AssertMatrixEqual(Matrix4x4 expected, Matrix4x4 actual, int precision = 4) {
		Assert.Equal(expected.M11, actual.M11, precision);
		Assert.Equal(expected.M12, actual.M12, precision);
		Assert.Equal(expected.M13, actual.M13, precision);
		Assert.Equal(expected.M14, actual.M14, precision);
		Assert.Equal(expected.M21, actual.M21, precision);
		Assert.Equal(expected.M22, actual.M22, precision);
		Assert.Equal(expected.M23, actual.M23, precision);
		Assert.Equal(expected.M24, actual.M24, precision);
		Assert.Equal(expected.M31, actual.M31, precision);
		Assert.Equal(expected.M32, actual.M32, precision);
		Assert.Equal(expected.M33, actual.M33, precision);
		Assert.Equal(expected.M34, actual.M34, precision);
		Assert.Equal(expected.M41, actual.M41, precision);
		Assert.Equal(expected.M42, actual.M42, precision);
		Assert.Equal(expected.M43, actual.M43, precision);
		Assert.Equal(expected.M44, actual.M44, precision);
	}

	private static void AssertVectorEqual(Vector3 expected, Vector3 actual, int precision = 4) {
		Assert.Equal(expected.X, actual.X, precision);
		Assert.Equal(expected.Y, actual.Y, precision);
		Assert.Equal(expected.Z, actual.Z, precision);
	}

	private static void AssertQuaternionEquivalent(Quaternion expected, Quaternion actual, float epsilon = 0.0001f) {
		var distance = MathF.Abs(Quaternion.Dot(Quaternion.Normalize(expected), Quaternion.Normalize(actual)));
		Assert.True(1f - distance <= epsilon, $"Expected quaternion {expected} to match {actual}.");
	}
}
