using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Attachment data derived from a parent and child hardpoint pair.
/// </summary>
public readonly record struct HardpointConnectionDescriptor(
	HardpointJointType Type,
	Vector3 RelativePosition,
	Quaternion RelativeOrientation,
	Vector3 ParentPoint,
	Vector3 ChildPoint,
	Vector3 Axis,
	float Min0,
	float Max0);
