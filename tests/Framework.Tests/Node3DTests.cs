using System.Numerics;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class Node3DTests {
	[Fact]
	public void LocalTransform_CombinesScaleRotationAndTranslation() {
		var node = new Node3D("Node") {
			Position = new Vector3(10f, 20f, 30f),
			Rotation = Quaternion.CreateFromYawPitchRoll(0.2f, 0.4f, -0.3f),
			Scale = new Vector3(2f, 3f, 4f)
		};

		var expected =
			Matrix4x4.CreateScale(node.Scale) *
			Matrix4x4.CreateFromQuaternion(node.Rotation) *
			Matrix4x4.CreateTranslation(node.Position);

		AssertMatrixEqual(expected, node.LocalTransform);
	}

	[Fact]
	public void GlobalProperties_ComposeAcrossParents() {
		var parent = new Node3D("Parent") {
			Position = new Vector3(10f, 0f, -5f),
			Rotation = Quaternion.CreateFromYawPitchRoll(0.6f, -0.1f, 0.25f),
			Scale = new Vector3(3f, 3f, 3f)
		};
		var child = parent.AddChild(new Node3D("Child") {
			Position = new Vector3(5f, 1f, 2f),
			Rotation = Quaternion.CreateFromYawPitchRoll(-0.3f, 0.4f, 0.1f),
			Scale = new Vector3(2f, 2f, 2f)
		});

		var expectedGlobalTransform = child.LocalTransform * parent.GlobalTransform;

		AssertMatrixEqual(expectedGlobalTransform, child.GlobalTransform);
		AssertVectorEqual(Vector3.Transform(Vector3.Zero, expectedGlobalTransform), child.GlobalPosition);
		AssertVectorEqual(
			new Vector3(
				new Vector3(expectedGlobalTransform.M11, expectedGlobalTransform.M12, expectedGlobalTransform.M13).Length(),
				new Vector3(expectedGlobalTransform.M21, expectedGlobalTransform.M22, expectedGlobalTransform.M23).Length(),
				new Vector3(expectedGlobalTransform.M31, expectedGlobalTransform.M32, expectedGlobalTransform.M33).Length()),
			child.GlobalScale);
		AssertQuaternionEquivalent(DecomposeRotation(expectedGlobalTransform), child.GlobalRotation);
	}

	[Fact]
	public void Light3D_DerivesDirectionFromNodeRotation() {
		var light = new Light3D("Light") {
			Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.5f)
		};

		var expectedDirection = new Vector3(-1f, 0f, 0f);

		AssertVectorEqual(expectedDirection, light.WorldDirection);
		AssertVectorEqual(expectedDirection, light.TargetPosition - light.WorldPosition);
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
