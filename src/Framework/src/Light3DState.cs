using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Immutable world-space light snapshot suitable for shader upload or renderer use.
/// </summary>
public readonly record struct Light3DState(
	Light3DType Type,
	bool Enabled,
	bool Ambient,
	Vector3 Position,
	Vector3 Target,
	Vector3 Direction,
	Color Color,
	Vector3 ColorVector,
	float Energy,
	float Range,
	float Attenuation,
	float InnerSpotAngleDegrees,
	float OuterSpotAngleDegrees);
