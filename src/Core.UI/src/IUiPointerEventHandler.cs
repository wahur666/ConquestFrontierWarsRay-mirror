using System.Numerics;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Pointer target contract used by <see cref="UiEventSource"/>.
/// </summary>
public interface IUiPointerEventHandler {
	/// <summary>
	/// True when the node is eligible for pointer routing.
	/// </summary>
	bool IsPointerInputEnabled { get; }

	/// <summary>
	/// Hit-tests a screen-space point.
	/// </summary>
	bool HitTest(Vector2 screenPoint);

	/// <summary>
	/// Handles one routed pointer event.
	/// </summary>
	void OnPointerEvent(UiPointerEvent pointerEvent);
}
