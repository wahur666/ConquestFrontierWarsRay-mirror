using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Routed pointer event shared across a bubbling dispatch chain.
/// </summary>
public sealed class UiPointerEvent : UiEvent {
	public UiPointerEvent(UiPointerEventKind kind, Vector2 position, MouseButton? button, float wheelDelta, Node originalTarget)
		: base(originalTarget) {
		Kind = kind;
		Position = position;
		Button = button;
		WheelDelta = wheelDelta;
	}

	/// <summary>
	/// Routed event kind.
	/// </summary>
	public UiPointerEventKind Kind { get; }

	/// <summary>
	/// Pointer position in screen space.
	/// </summary>
	public Vector2 Position { get; }

	/// <summary>
	/// Mouse button associated with the event when applicable.
	/// </summary>
	public MouseButton? Button { get; }

	/// <summary>
	/// Mouse wheel delta for wheel events.
	/// </summary>
	public float WheelDelta { get; }
}
