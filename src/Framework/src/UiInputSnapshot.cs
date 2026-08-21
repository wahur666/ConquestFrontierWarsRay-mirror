using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Per-frame UI input snapshot sampled once by the framework.
/// </summary>
public readonly record struct UiInputSnapshot(
	Vector2 PointerPosition,
	bool LeftPressed,
	bool LeftDown,
	bool LeftReleased,
	bool MiddlePressed,
	bool MiddleDown,
	bool MiddleReleased,
	bool RightPressed,
	bool RightDown,
	bool RightReleased,
	float WheelDelta,
	bool NavigateUp,
	bool NavigateDown,
	bool NavigateLeft,
	bool NavigateRight,
	bool AcceptPressed,
	bool BackPressed,
	bool EscapePressed);
