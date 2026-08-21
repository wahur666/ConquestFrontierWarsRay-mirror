using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Routed pointer event shared across a bubbling dispatch chain.
/// </summary>
public sealed class UiPointerEvent {
	internal UiPointerEvent(UiPointerEventKind kind, Vector2 position, MouseButton? button, float wheelDelta, Node originalTarget) {
		Kind = kind;
		Position = position;
		Button = button;
		WheelDelta = wheelDelta;
		OriginalTarget = originalTarget;
		CurrentTarget = originalTarget;
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

	/// <summary>
	/// First hit node that received this event.
	/// </summary>
	public Node OriginalTarget { get; }

	/// <summary>
	/// Node currently handling the event during bubbling.
	/// </summary>
	public Node CurrentTarget { get; internal set; }

	/// <summary>
	/// True once a handler stops propagation.
	/// </summary>
	public bool Handled { get; private set; }

	/// <summary>
	/// Stops further bubbling.
	/// </summary>
	public void MarkHandled() {
		Handled = true;
	}
}
