using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Immutable hardpoint data matching the durable native contract shape.
/// </summary>
public readonly record struct HardpointInfo3D(
	HardpointJointType Type,
	Vector3 Point,
	Quaternion Orientation,
	Vector3 Axis,
	float Min0,
	float Max0,
	float SpringConstant,
	float DampingConstant,
	float RestLength);
