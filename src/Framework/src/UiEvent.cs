namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Base routed UI event shared across a bubbling dispatch chain.
/// </summary>
public abstract class UiEvent {
	protected UiEvent(Node originalTarget) {
		OriginalTarget = originalTarget;
		CurrentTarget = originalTarget;
	}

	/// <summary>
	/// First node that received this event.
	/// </summary>
	public Node OriginalTarget { get; }

	/// <summary>
	/// Node currently handling the event during bubbling.
	/// </summary>
	public Node CurrentTarget { get; private set; }

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

	/// <summary>
	/// Updates the current bubbling target before invoking the next handler.
	/// </summary>
	public void RouteTo(Node currentTarget) {
		ArgumentNullException.ThrowIfNull(currentTarget);
		CurrentTarget = currentTarget;
	}
}
