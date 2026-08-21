using System.Numerics;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Pointer target contract used by a UI input dispatcher.
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
