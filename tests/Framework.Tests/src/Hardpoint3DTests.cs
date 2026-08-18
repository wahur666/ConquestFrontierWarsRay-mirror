using System.Numerics;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class Hardpoint3DTests {
	[Fact]
	public void ToInfo_ExportsNativeStyleFields() {
		var hardpoint = new Hardpoint3D("DockLatch", "dock") {
			Position = new Vector3(1f, 2f, 3f),
			Rotation = Quaternion.CreateFromYawPitchRoll(0.2f, -0.3f, 0.1f),
			JointType = HardpointJointType.Prismatic,
			Axis = Vector3.Normalize(new Vector3(0f, 0f, 2f)),
			Min0 = -2f,
			Max0 = 4f,
			SpringConstant = 8f,
			DampingConstant = 1.5f,
			RestLength = 0.75f
		};

		var info = hardpoint.ToInfo();

		Assert.Equal(HardpointJointType.Prismatic, info.Type);
		AssertVectorEqual(new Vector3(1f, 2f, 3f), info.Point);
		AssertQuaternionEquivalent(hardpoint.Rotation, info.Orientation);
		AssertVectorEqual(Vector3.UnitZ, info.Axis);
		Assert.Equal(-2f, info.Min0);
		Assert.Equal(4f, info.Max0);
		Assert.Equal(8f, info.SpringConstant);
		Assert.Equal(1.5f, info.DampingConstant);
		Assert.Equal(0.75f, info.RestLength);
	}

	[Fact]
	public void BuildConnectionTo_FixedHardpointMatchesNativeRelativeAlignment() {
		var parent = new Hardpoint3D("Parent", "mount") {
			Position = new Vector3(3f, 0f, 0f),
			Rotation = Quaternion.CreateFromYawPitchRoll(0.25f, 0f, 0f),
			JointType = HardpointJointType.Fixed
		};
		var child = new Hardpoint3D("Child", "mount") {
			Position = new Vector3(1f, 0f, 0f),
			Rotation = Quaternion.CreateFromYawPitchRoll(-0.15f, 0f, 0f),
			JointType = HardpointJointType.Fixed
		};

		var descriptor = parent.BuildConnectionTo(child);
		var parentMatrix = Matrix4x4.CreateFromQuaternion(parent.Rotation);
		var childMatrix = Matrix4x4.CreateFromQuaternion(child.Rotation);
		var expectedOrientation = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(parentMatrix * Matrix4x4.Transpose(childMatrix)));
		var expectedPosition = parent.Position - Vector3.Transform(child.Position, parentMatrix * Matrix4x4.Transpose(childMatrix));

		Assert.Equal(HardpointJointType.Fixed, descriptor.Type);
		AssertVectorEqual(expectedPosition, descriptor.RelativePosition);
		AssertQuaternionEquivalent(expectedOrientation, descriptor.RelativeOrientation);
		AssertVectorEqual(parent.Position, descriptor.ParentPoint);
		AssertVectorEqual(child.Position, descriptor.ChildPoint);
	}

	[Fact]
	public void BuildConnectionTo_RevoluteUsesChildTypeAxisAndLimits() {
		var parent = new Hardpoint3D("TurretMount", "weapon") {
			Rotation = Quaternion.CreateFromYawPitchRoll(0.4f, 0.2f, 0f)
		};
		var child = new Hardpoint3D("BarrelBreech", "weapon") {
			JointType = HardpointJointType.Revolute,
			Axis = Vector3.UnitX,
			Min0 = -0.5f,
			Max0 = 0.75f,
			Rotation = Quaternion.CreateFromYawPitchRoll(-0.1f, 0.3f, 0f)
		};

		var descriptor = parent.BuildConnectionTo(child);
		var expectedMatrix = Matrix4x4.CreateFromQuaternion(parent.Rotation) * Matrix4x4.CreateFromQuaternion(child.Rotation);
		var expectedAxis = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitX, expectedMatrix));

		Assert.Equal(HardpointJointType.Revolute, descriptor.Type);
		AssertVectorEqual(expectedAxis, descriptor.Axis);
		Assert.Equal(-0.5f, descriptor.Min0);
		Assert.Equal(0.75f, descriptor.Max0);
	}

	[Fact]
	public void Socket3D_FindSocketAndWorldTransformWorkAcrossHierarchy() {
		var hull = new Node3D("Hull") {
			Position = new Vector3(10f, 0f, 0f)
		};
		var rootSocket = hull.AddChild(new Socket3D("RootSocket", "mount") {
			Position = new Vector3(1f, 2f, 0f)
		});
		var nestedSocket = rootSocket.AddChild(new Socket3D("NestedSocket", "weapon") {
			Position = new Vector3(0f, 0f, 3f)
		});
		var grandChildSocket = nestedSocket.AddChild(new Socket3D("GrandChildSocket", "dock") {
			Position = new Vector3(0f, 1f, 0f)
		});

		Assert.Same(nestedSocket, rootSocket.FindSocket("NestedSocket"));
		Assert.Equal(2, rootSocket.EnumerateSockets().Count());
		AssertVectorEqual(new Vector3(11f, 2f, 3f), nestedSocket.GlobalPosition);
		AssertVectorEqual(new Vector3(11f, 3f, 3f), grandChildSocket.GlobalPosition);
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
