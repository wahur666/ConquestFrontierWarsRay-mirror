using System.Numerics;
using Math3D;
using Xunit;

namespace ConquestFrontierWarsRay.Math3D.Tests;

public sealed class MathEngineTests {
	[Fact]
	public void MatrixInverse_RoundTripsVector() {
		var matrix = Matrix3.CreateRotationY(MathF.PI / 4f);

		Assert.True(MathEngine.TryInverse(matrix, out var inverse));

		var vector = new Vector3(3f, -2f, 5f);
		var transformed = MathEngine.Transform(matrix, vector);
		var roundTripped = MathEngine.Transform(inverse, transformed);

		AssertClose(vector, roundTripped);
	}

	[Fact]
	public void TransformMultiply_MatchesSequentialApplication() {
		var left = new Transform3(Matrix3.CreateRotationZ(MathF.PI / 6f), new Vector3(2f, 0f, 0f));
		var right = new Transform3(Matrix3.CreateRotationX(MathF.PI / 4f), new Vector3(0f, 3f, 1f));
		var combined = MathEngine.Multiply(left, right);
		var vector = new Vector3(4f, -1f, 2f);

		var sequential = MathEngine.Transform(left, MathEngine.Transform(right, vector));
		var single = MathEngine.Transform(combined, vector);

		AssertClose(sequential, single);
	}

	[Fact]
	public void TryGeneralInverse_InvertsAffineTransform() {
		var transform = new Transform3(Matrix3.CreateRotationY(MathF.PI / 3f), new Vector3(10f, -4f, 1f));
		var vector = new Vector3(-7f, 2f, 9f);

		Assert.True(MathEngine.TryGeneralInverse(transform, out var inverse, out var w));

		var transformed = MathEngine.Transform(transform, vector);
		var restored = MathEngine.Transform(inverse, transformed);

		AssertClose(vector, restored);
		Assert.Equal(1f, w, 5);
	}

	[Fact]
	public void QuaternionConversions_StayConsistent() {
		var quaternion = Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(0.7f, -0.2f, 0.1f));

		var matrix = MathEngine.QuaternionToMatrix(quaternion);
		var reconstructed = MathEngine.MatrixToQuaternion(matrix);
		var vector = Vector3.Normalize(new Vector3(1f, 2f, -3f));

		var viaQuaternion = MathEngine.Transform(quaternion, vector);
		var viaReconstructed = MathEngine.Transform(reconstructed, vector);

		AssertClose(viaQuaternion, viaReconstructed);
	}

	[Fact]
	public void TransformList_ProcessesWholeSpan() {
		var transform = new Transform3(Matrix3.CreateRotationX(MathF.PI / 2f), new Vector3(1f, 2f, 3f));
		var source = new[] {
			new Vector3(1f, 0f, 0f),
			new Vector3(0f, 1f, 0f),
			new Vector3(0f, 0f, 1f)
		};
		var destination = new Vector3[source.Length];

		MathEngine.TransformList(destination, transform, source);

		for (var index = 0; index < source.Length; index++) {
			AssertClose(MathEngine.Transform(transform, source[index]), destination[index]);
		}
	}

	[Fact]
	public void PersistTypes_RoundTripNumericsValues() {
		var transform = new Transform3(Matrix3.CreateRotationZ(MathF.PI / 5f), new Vector3(8f, 9f, -2f));
		var persisted = PersistTransform.FromTransform3(transform);
		var restored = persisted.ToTransform3();
		var vector = new Vector3(2f, 3f, 4f);

		AssertClose(MathEngine.Transform(transform, vector), MathEngine.Transform(restored, vector));
	}

	private static void AssertClose(Vector3 expected, Vector3 actual, float tolerance = 1e-5f) {
		Assert.InRange(Vector3.Distance(expected, actual), 0f, tolerance);
	}
}
