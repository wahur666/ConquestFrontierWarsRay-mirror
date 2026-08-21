using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Mouse-clickable button with hover feedback and centered text.
/// </summary>
/// <remarks>
/// Input is intentionally explicit. Call <see cref="HandleInput"/> during the
/// owning scene or UI controller update step to turn the current frame's mouse
/// state into a button press decision.
/// </remarks>
public sealed class ButtonNode : Control, IUiPointerEventHandler {
	private bool _isHovered;
	private bool _isPressed;
	private bool _pendingClick;

	public ButtonNode(string? name = null) : base(name) {
	}

	public string Text { get; set; } = string.Empty;
	public float FontSize { get; set; } = 14f;
	public Color Fill { get; set; } = new(34, 42, 52, 255);
	public Color HoverFill { get; set; } = new(54, 67, 82, 255);
	public Color Outline { get; set; } = new(140, 160, 180, 200);
	public Color TextColor { get; set; } = Color.RayWhite;
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;
	public bool IsPointerInputEnabled => Visible && Size.X > 0f && Size.Y > 0f;

	public event Action<ButtonNode>? Clicked;

	public bool HandleInput() {
		if (_pendingClick) {
			_pendingClick = false;
			return true;
		}

		if (!Visible || !Raylib.IsMouseButtonPressed(MouseButton.Left)) {
			return false;
		}

		return ContainsPoint(Raylib.GetMousePosition());
	}

	public bool HitTest(System.Numerics.Vector2 screenPoint) {
		return ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
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
				pointerEvent.MarkHandled();
				break;
			case UiPointerEventKind.Up:
				_isPressed = false;
				_isHovered = HitTest(pointerEvent.Position);
				pointerEvent.MarkHandled();
				break;
			case UiPointerEventKind.Click:
				_isHovered = HitTest(pointerEvent.Position);
				_isPressed = false;
				_pendingClick = true;
				Clicked?.Invoke(this);
				pointerEvent.MarkHandled();
				break;
			case UiPointerEventKind.Wheel:
				_isHovered = HitTest(pointerEvent.Position);
				break;
		}
	}

	protected override void Draw() {
		if (Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		var hovered = _isHovered || (!_isPressed && ContainsPoint(Raylib.GetMousePosition()));
		var fill = _isPressed ? HoverFill : hovered ? HoverFill : Fill;

		Raylib.DrawRectangleRec(bounds, fill);
		Raylib.DrawRectangleLinesEx(bounds, 1f, Outline);

		if (string.IsNullOrEmpty(Text)) {
			return;
		}

		var textWidth = UiText.MeasureWidth(Text, FontSize, TextStyle);
		var textX = bounds.X + ((bounds.Width - textWidth) * 0.5f);
		var textY = bounds.Y + ((bounds.Height - FontSize) * 0.5f) - 1f;
		UiText.Draw(Text, textX, textY, FontSize, TextColor, TextStyle);
	}
}
