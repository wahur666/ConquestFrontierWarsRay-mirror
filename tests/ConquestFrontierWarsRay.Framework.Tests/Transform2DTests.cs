using System.Numerics;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class Transform2DTests {
	[Fact]
	public void Matrix_CombinesScaleRotationAndTranslation() {
		var transform = new Transform2D(
			new Vector2(10f, 20f),
			MathF.PI / 2f,
			new Vector2(2f, 3f));

		var expected =
			Matrix3x2.CreateScale(transform.Scale) *
			Matrix3x2.CreateRotation(transform.Rotation) *
			Matrix3x2.CreateTranslation(transform.Position);

		AssertMatrixEqual(expected, transform.Matrix);
	}

	[Fact]
	public void Compose_UsesParentTransformRules() {
		var parent = new Transform2D(
			new Vector2(10f, 0f),
			MathF.PI / 2f,
			new Vector2(2f, 3f));
		var child = new Transform2D(
			new Vector2(5f, 0f),
			MathF.PI / 4f,
			new Vector2(4f, 5f));

		var actual = child.Compose(parent);
		var expectedMatrix = child.Matrix * parent.Matrix;

		AssertMatrixEqual(expectedMatrix, actual.Matrix);
		Assert.Equal(MathF.Atan2(expectedMatrix.M12, expectedMatrix.M11), actual.Rotation, 4);
		AssertVectorEqual(Vector2.Transform(Vector2.Zero, expectedMatrix), actual.Position);
		AssertVectorEqual(
			new Vector2(
				new Vector2(expectedMatrix.M11, expectedMatrix.M12).Length(),
				new Vector2(expectedMatrix.M21, expectedMatrix.M22).Length()),
			actual.Scale);
	}

	[Fact]
	public void FromMatrix_DecomposesTransformComponents() {
		var expected = new Transform2D(
			new Vector2(-4f, 12f),
			0.35f,
			new Vector2(3f, 5f));

		var actual = Transform2D.FromMatrix(expected.Matrix);

		AssertVectorEqual(expected.Position, actual.Position);
		Assert.Equal(expected.Rotation, actual.Rotation, 4);
		AssertVectorEqual(expected.Scale, actual.Scale);
	}

	private static void AssertMatrixEqual(Matrix3x2 expected, Matrix3x2 actual, int precision = 4) {
		Assert.Equal(expected.M11, actual.M11, precision);
		Assert.Equal(expected.M12, actual.M12, precision);
		Assert.Equal(expected.M21, actual.M21, precision);
		Assert.Equal(expected.M22, actual.M22, precision);
		Assert.Equal(expected.M31, actual.M31, precision);
		Assert.Equal(expected.M32, actual.M32, precision);
	}

	private static void AssertVectorEqual(Vector2 expected, Vector2 actual, int precision = 4) {
		Assert.Equal(expected.X, actual.X, precision);
		Assert.Equal(expected.Y, actual.Y, precision);
	}
}
