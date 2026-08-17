using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class Camera3DNodeTests {
	[Fact]
	public void ToRaylibCamera_UsesRaylibStylePositionTargetAndUp() {
		var camera = new Camera3DNode("Camera") {
			Position = new Vector3(4f, 5f, 6f),
			Target = new Vector3(0f, 0f, 0f),
			Up = Vector3.UnitY,
			FovY = 52f,
			Projection = CameraProjection.Perspective
		};

		var raylibCamera = camera.ToRaylibCamera();

		AssertVectorEqual(camera.GlobalPosition, raylibCamera.Position);
		AssertVectorEqual(camera.Target, raylibCamera.Target);
		AssertVectorEqual(camera.Up, raylibCamera.Up);
		Assert.Equal(52f, raylibCamera.FovY, 4);
		Assert.Equal(CameraProjection.Perspective, raylibCamera.Projection);
	}

	[Fact]
	public void TryPointToScreen_ProjectsCenterPointToViewportCenter() {
		var camera = CreateDefaultCamera();

		var visible = camera.TryPointToScreen(new Vector3(0f, 0f, -10f), out var screenX, out var screenY, out var depth);

		Assert.True(visible);
		Assert.Equal(400f, screenX, 3);
		Assert.Equal(300f, screenY, 3);
		Assert.Equal(10f, depth, 3);
	}

	[Fact]
	public void ScreenToPoint_ReturnsNearPlanePointInWorldSpace() {
		var camera = CreateDefaultCamera();

		var worldPoint = camera.ScreenToPoint(400f, 300f);

		AssertVectorEqual(new Vector3(0f, 0f, -1f), worldPoint);
	}

	[Fact]
	public void ObjectVisibility_MatchesEngineStyleFrustumChecks() {
		var camera = CreateDefaultCamera();

		Assert.Equal(CameraVisibilityState.FullyVisible, camera.ObjectVisibility(new Vector3(0f, 0f, -10f), 1f));
		Assert.Equal(CameraVisibilityState.SubPixel, camera.ObjectVisibility(new Vector3(0f, 0f, -100f), 0.01f));
		Assert.Equal(CameraVisibilityState.NotVisible, camera.ObjectVisibility(new Vector3(0f, 0f, 1f), 0.25f));
	}

	[Fact]
	public void ObjectVisibility_Box_UsesFrustumPlaneClassification() {
		var camera = CreateDefaultCamera();

		Assert.Equal(
			CameraVisibilityState.FullyVisible,
			camera.ObjectVisibility(new BoundingBox(new Vector3(-1f, -1f, -12f), new Vector3(1f, 1f, -8f))));
		Assert.Equal(
			CameraVisibilityState.PartiallyVisible,
			camera.ObjectVisibility(new BoundingBox(new Vector3(-2f, -2f, -2f), new Vector3(2f, 2f, 0.5f))));
		Assert.Equal(
			CameraVisibilityState.NotVisible,
			camera.ObjectVisibility(new BoundingBox(new Vector3(50f, -1f, -10f), new Vector3(52f, 1f, -8f))));
	}

	private static Camera3DNode CreateDefaultCamera() {
		return new Camera3DNode("Camera") {
			Target = new Vector3(0f, 0f, -1f),
			FovY = 60f,
			ZNear = 1f,
			ZFar = 500f,
			ViewportOverride = new CameraViewport(0f, 0f, 800f, 600f)
		};
	}

	private static void AssertVectorEqual(Vector3 expected, Vector3 actual, int precision = 4) {
		Assert.Equal(expected.X, actual.X, precision);
		Assert.Equal(expected.Y, actual.Y, precision);
		Assert.Equal(expected.Z, actual.Z, precision);
	}
}
