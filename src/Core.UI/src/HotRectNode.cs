using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Minimal rectangular pointer target with debug drawing and bubbling hooks.
/// </summary>
public class HotRectNode : Control, IUiPointerEventHandler {
	private bool _isHovered;
	private bool _isPressed;

	public HotRectNode(string? name = null) : base(name) {
	}

	/// <summary>
	/// User-facing label drawn inside the hot rect.
	/// </summary>
	public string Label { get; set; } = "Hot rect";

	/// <summary>
	/// Raised whenever this node receives a routed pointer event.
	/// </summary>
	public event Action<HotRectNode, UiPointerEvent>? PointerEvent;

	/// <summary>
	/// Optional normal fill color.
	/// </summary>
	public Color Fill { get; set; } = new(32, 49, 78, 104);

	/// <summary>
	/// Outline color while idle.
	/// </summary>
	public Color Outline { get; set; } = new(120, 148, 198, 255);

	/// <summary>
	/// Fill color while hovered.
	/// </summary>
	public Color HoverFill { get; set; } = new(52, 88, 140, 130);

	/// <summary>
	/// Outline color while hovered.
	/// </summary>
	public Color HoverOutline { get; set; } = new(255, 208, 96, 255);

	/// <summary>
	/// Fill color while pressed.
	/// </summary>
	public Color PressedFill { get; set; } = new(76, 108, 170, 180);

	/// <summary>
	/// Stops bubbling when this node receives an event.
	/// </summary>
	public bool StopsPointerPropagation { get; set; }

	/// <summary>
	/// Last event kind received by this node.
	/// </summary>
	public UiPointerEventKind? LastEventKind { get; private set; }

	/// <summary>
	/// Last route summary seen by this node.
	/// </summary>
	public string LastRouteSummary { get; private set; } = "none";

	public bool IsPointerInputEnabled => Visible && Size.X > 0f && Size.Y > 0f;

	public bool HitTest(Vector2 screenPoint) {
		return ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		LastEventKind = pointerEvent.Kind;
		LastRouteSummary = $"{pointerEvent.OriginalTarget.Name} -> {pointerEvent.CurrentTarget.Name}";

		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
				_isHovered = true;
				break;
			case UiPointerEventKind.Leave:
				_isHovered = false;
				_isPressed = false;
				break;
			case UiPointerEventKind.Move:
				_isHovered = HitTest(pointerEvent.Position);
				break;
			case UiPointerEventKind.Down:
				_isHovered = true;
				_isPressed = true;
				break;
			case UiPointerEventKind.Up:
				_isPressed = false;
				_isHovered = HitTest(pointerEvent.Position);
				break;
			case UiPointerEventKind.Click:
				_isHovered = HitTest(pointerEvent.Position);
				_isPressed = false;
				break;
			case UiPointerEventKind.Wheel:
				_isHovered = HitTest(pointerEvent.Position);
				break;
		}

		PointerEvent?.Invoke(this, pointerEvent);

		if (StopsPointerPropagation) {
			pointerEvent.MarkHandled();
		}
	}

	protected override void Draw() {
		var bounds = GlobalBounds;
		var fill = _isPressed ? PressedFill : _isHovered ? HoverFill : Fill;
		var outline = _isHovered || _isPressed ? HoverOutline : Outline;

		Raylib.DrawRectangleRec(bounds, fill);
		Raylib.DrawRectangleLinesEx(bounds, 2f, outline);
		UiText.Draw(Label, bounds.X + 12f, bounds.Y + 10f, 18f, Color.RayWhite);

		var eventText = LastEventKind is null ? "Last event: none" : $"Last event: {LastEventKind}";
		UiText.Draw(eventText, bounds.X + 12f, bounds.Y + 36f, 15f, new Color(220, 228, 240, 255));
		UiText.Draw(StopsPointerPropagation ? "Consumes: yes" : "Consumes: no", bounds.X + 12f, bounds.Y + 58f, 15f, new Color(220, 228, 240, 255));
	}
}
