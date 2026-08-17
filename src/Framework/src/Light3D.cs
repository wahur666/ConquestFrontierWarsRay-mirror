using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Reusable 3D light node with transform-derived world position and direction.
/// </summary>
public sealed class Light3D : Node3D {
	private const float Epsilon = 0.000001f;
	private static readonly Vector3 DefaultLocalDirection = -Vector3.UnitZ;

	/// <summary>
	/// Creates a light node.
	/// </summary>
	public Light3D(string? name = null) : base(name) {
	}

	/// <summary>
	/// True when the light should affect rendering.
	/// </summary>
	public bool Enabled { get; set; } = true;

	/// <summary>
	/// True when the light contributes to ambient scene lighting instead of a directional/point/spot pass.
	/// </summary>
	public bool Ambient { get; set; }

	/// <summary>
	/// Light behavior model.
	/// </summary>
	public Light3DType Type { get; set; } = Light3DType.Point;

	/// <summary>
	/// Light color in raylib color space.
	/// </summary>
	public Color Color { get; set; } = Color.White;

	/// <summary>
	/// Scalar multiplier applied to the light color.
	/// </summary>
	public float Energy { get; set; } = 1f;

	/// <summary>
	/// Effective light range for point and spot lights.
	/// </summary>
	public float Range { get; set; } = 10f;

	/// <summary>
	/// Attenuation factor consumed by custom lighting shaders.
	/// </summary>
	public float Attenuation { get; set; } = 1f;

	/// <summary>
	/// Inner spotlight cone angle in degrees.
	/// </summary>
	public float InnerSpotAngleDegrees { get; set; } = 20f;

	/// <summary>
	/// Outer spotlight cone angle in degrees.
	/// </summary>
	public float OuterSpotAngleDegrees { get; set; } = 35f;

	/// <summary>
	/// World-space light position.
	/// </summary>
	public Vector3 WorldPosition => GlobalPosition;

	/// <summary>
	/// World-space light direction derived from the node rotation.
	/// </summary>
	public Vector3 WorldDirection => NormalizeOrFallback(
		Raymath.Vector3RotateByQuaternion(DefaultLocalDirection, NormalizeOrIdentity(GlobalRotation)),
		DefaultLocalDirection);

	/// <summary>
	/// World-space target point in the style used by raylib's stock light helpers.
	/// </summary>
	public Vector3 TargetPosition => Raymath.Vector3Add(WorldPosition, WorldDirection);

	/// <summary>
	/// Normalized RGB color multiplied by <see cref="Energy"/>.
	/// </summary>
	public Vector3 ColorVector {
		get {
			var energy = Math.Max(Energy, 0f);
			return new Vector3(
				(Color.R / 255f) * energy,
				(Color.G / 255f) * energy,
				(Color.B / 255f) * energy);
		}
	}

	/// <summary>
	/// Spotlight inner cone angle in radians.
	/// </summary>
	public float InnerSpotAngleRadians => DegreesToRadians(SanitizedInnerSpotAngleDegrees);

	/// <summary>
	/// Spotlight outer cone angle in radians.
	/// </summary>
	public float OuterSpotAngleRadians => DegreesToRadians(SanitizedOuterSpotAngleDegrees);

	/// <summary>
	/// Spotlight inner cone cosine.
	/// </summary>
	public float InnerSpotCosine => MathF.Cos(InnerSpotAngleRadians * 0.5f);

	/// <summary>
	/// Spotlight outer cone cosine.
	/// </summary>
	public float OuterSpotCosine => MathF.Cos(OuterSpotAngleRadians * 0.5f);

	/// <summary>
	/// World-space snapshot of the current light state.
	/// </summary>
	public Light3DState ToLightState() {
		return new Light3DState(
			Type,
			Enabled,
			Ambient,
			WorldPosition,
			TargetPosition,
			WorldDirection,
			Color,
			ColorVector,
			Math.Max(Energy, 0f),
			Math.Max(Range, 0f),
			Math.Max(Attenuation, 0f),
			SanitizedInnerSpotAngleDegrees,
			SanitizedOuterSpotAngleDegrees);
	}

	private float SanitizedInnerSpotAngleDegrees =>
		Math.Clamp(InnerSpotAngleDegrees, 0f, SanitizedOuterSpotAngleDegrees);

	private float SanitizedOuterSpotAngleDegrees =>
		Math.Clamp(OuterSpotAngleDegrees, 0f, 179f);

	private static Quaternion NormalizeOrIdentity(Quaternion value) {
		return Raymath.QuaternionLength(value) > Epsilon
			? Raymath.QuaternionNormalize(value)
			: Quaternion.Identity;
	}

	private static Vector3 NormalizeOrFallback(Vector3 value, Vector3 fallback) {
		return Raymath.Vector3LengthSqr(value) > Epsilon
			? Raymath.Vector3Normalize(value)
			: fallback;
	}

	private static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);
}
