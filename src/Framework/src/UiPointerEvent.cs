using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Routed pointer event shared across a bubbling dispatch chain.
/// </summary>
public sealed class UiPointerEvent : UiEvent {
	private Node? _captureTarget;

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

	/// <summary>
	/// True when a handler requested pointer capture during this dispatch.
	/// </summary>
	public bool CaptureRequested => _captureTarget is not null;

	/// <summary>
	/// Capture target requested by the current handler chain.
	/// </summary>
	public Node? CaptureTarget => _captureTarget;

	/// <summary>
	/// Requests pointer capture for the current handler.
	/// </summary>
	public void RequestPointerCapture() {
		_captureTarget = CurrentTarget;
	}
}
