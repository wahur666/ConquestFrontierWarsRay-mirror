using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class Light3DTests {
	[Fact]
	public void TypeOrdering_MatchesRaylibHelpersAndAddsSpot() {
		Assert.Equal(0, (int)Light3DType.Directional);
		Assert.Equal(1, (int)Light3DType.Point);
		Assert.Equal(2, (int)Light3DType.Spot);
	}

	[Fact]
	public void ToLightState_UsesGlobalTransformForWorldValues() {
		var parent = new Node3D("Parent") {
			Position = new Vector3(10f, 2f, -4f),
			Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI * 0.5f)
		};
		var light = parent.AddChild(new Light3D("Light") {
			Type = Light3DType.Directional,
			Position = new Vector3(3f, 0f, 0f),
			Color = new Color(128, 64, 32, 255),
			Energy = 2f
		});

		var state = light.ToLightState();

		Assert.Equal(Light3DType.Directional, state.Type);
		AssertVectorEqual(new Vector3(10f, 2f, -7f), state.Position);
		AssertVectorEqual(new Vector3(-1f, 0f, 0f), state.Direction);
		AssertVectorEqual(new Vector3(9f, 2f, -7f), state.Target);
		AssertVectorEqual(new Vector3(128f / 255f * 2f, 64f / 255f * 2f, 32f / 255f * 2f), state.ColorVector);
		Assert.False(state.Ambient);
	}

	[Fact]
	public void SpotAngles_AreClampedIntoValidConeRange() {
		var light = new Light3D("Spot") {
			Type = Light3DType.Spot,
			InnerSpotAngleDegrees = 60f,
			OuterSpotAngleDegrees = 40f
		};

		var state = light.ToLightState();

		Assert.Equal(40f, state.InnerSpotAngleDegrees, 4);
		Assert.Equal(40f, state.OuterSpotAngleDegrees, 4);
		Assert.Equal(MathF.Cos((40f * MathF.PI / 180f) * 0.5f), light.InnerSpotCosine, 4);
		Assert.Equal(MathF.Cos((40f * MathF.PI / 180f) * 0.5f), light.OuterSpotCosine, 4);
	}

	[Fact]
	public void ToLightState_PreservesAmbientFlag() {
		var light = new Light3D("Ambient") {
			Ambient = true,
			Color = new Color(10, 20, 30, 255),
			Energy = 0.5f
		};

		var state = light.ToLightState();

		Assert.True(state.Ambient);
		AssertVectorEqual(new Vector3(10f / 255f * 0.5f, 20f / 255f * 0.5f, 30f / 255f * 0.5f), state.ColorVector);
	}

	[Fact]
	public void ResolveAmbientColor_AddsEnabledAmbientLightsAndClamps() {
		var shader = new LightShader3D();
		shader.AmbientColor = new Vector3(0.2f, 0.3f, 0.4f);
		var lights = new[] {
			new Light3DState(Light3DType.Directional, true, true, Vector3.Zero, Vector3.Zero, -Vector3.UnitZ, new Color(255, 128, 0, 255), new Vector3(0.9f, 0.4f, 0f), 1f, 0f, 0f, 0f, 0f),
			new Light3DState(Light3DType.Point, true, false, Vector3.Zero, Vector3.Zero, -Vector3.UnitZ, Color.White, new Vector3(0.5f, 0.5f, 0.5f), 1f, 1f, 1f, 0f, 0f),
			new Light3DState(Light3DType.Directional, false, true, Vector3.Zero, Vector3.Zero, -Vector3.UnitZ, Color.White, new Vector3(0.4f, 0.4f, 0.4f), 1f, 0f, 0f, 0f, 0f)
		};

		var ambient = shader.ResolveAmbientColor(lights);

		AssertVectorEqual(new Vector3(1f, 0.7f, 0.4f), ambient);
	}

	[Fact]
	public void TryGetDynamicLight_SkipsAmbientLights() {
		var lights = new[] {
			new Light3DState(Light3DType.Directional, true, true, Vector3.Zero, Vector3.Zero, -Vector3.UnitZ, Color.White, Vector3.One, 1f, 0f, 0f, 0f, 0f),
			new Light3DState(Light3DType.Point, true, false, new Vector3(1f, 2f, 3f), Vector3.Zero, -Vector3.UnitZ, Color.White, Vector3.One, 1f, 4f, 0.3f, 0f, 0f)
		};
		var index = 0;

		var found = LightShader3D.TryGetDynamicLight(lights, ref index, out var light);

		Assert.True(found);
		Assert.Equal(Light3DType.Point, light.Type);
		Assert.Equal(2, index);
	}

	private static void AssertVectorEqual(Vector3 expected, Vector3 actual, int precision = 4) {
		Assert.Equal(expected.X, actual.X, precision);
		Assert.Equal(expected.Y, actual.Y, precision);
		Assert.Equal(expected.Z, actual.Z, precision);
	}
}
