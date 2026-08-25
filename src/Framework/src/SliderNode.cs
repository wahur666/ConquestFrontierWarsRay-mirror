using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Mouse-driven horizontal slider with normalized values.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Value"/> is clamped to the range [0, 1].
/// </para>
/// <para>
/// Input is intentionally explicit. Call <see cref="HandleInput"/> during the
/// owning scene or UI controller update step to apply drag behavior.
/// </para>
/// </remarks>
public sealed class SliderNode : Control, IUiPointerEventHandler {
	private bool _dragging;
	private bool _isHovered;
	private bool _pendingValueChanged;
	private float _value;

	public SliderNode(string? name = null) : base(name) {
	}

	public float Value {
		get => _value;
		set => _value = Math.Clamp(value, 0f, 1f);
	}

	public bool IsDragging => _dragging;

	public float Step { get; set; } = 0.01f;
	public Color TrackFill { get; set; } = new(42, 52, 68, 255);
	public Color ActiveTrackFill { get; set; } = new(96, 138, 208, 255);
	public Color Outline { get; set; } = new(132, 150, 174, 255);
	public Color KnobFill { get; set; } = new(245, 247, 252, 255);
	public Color KnobOutline { get; set; } = new(24, 30, 42, 255);
	public float TrackHeight { get; set; } = 8f;
	public float KnobRadius { get; set; } = 9f;
	public bool IsPointerInputEnabled => Visible && Size.X > 0f && Size.Y > 0f;

	public event Action<SliderNode>? ValueChanged;

	public bool HandleInput() {
		if (_pendingValueChanged) {
			_pendingValueChanged = false;
			return true;
		}

		if (!Visible || Size.X <= 0f || Size.Y <= 0f) {
			_dragging = false;
			return false;
		}

		var mouse = Raylib.GetMousePosition();
		var knobBounds = GetKnobBounds();
		var isHovering = ContainsPoint(mouse) || Raylib.CheckCollisionPointRec(mouse, knobBounds);

		if (Raylib.IsMouseButtonPressed(MouseButton.Left) && isHovering) {
			_dragging = true;
		}

		var changed = false;
		if (_dragging) {
			Value = NormalizeFromMouse(mouse.X);
			changed = true;
		}

		if (Raylib.IsMouseButtonReleased(MouseButton.Left)) {
			_dragging = false;
		}

		return changed;
	}

	public bool HitTest(System.Numerics.Vector2 screenPoint) {
		return ContainsPoint(screenPoint) || Raylib.CheckCollisionPointRec(screenPoint, GetKnobBounds());
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
				_isHovered = true;
				break;
			case UiPointerEventKind.Leave:
				_isHovered = false;
				break;
			case UiPointerEventKind.Move:
				_isHovered = HitTest(pointerEvent.Position);
				if (_dragging && pointerEvent.Button is null) {
					ApplyPointerValue(pointerEvent.Position.X);
				}
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left && HitTest(pointerEvent.Position)) {
					_isHovered = true;
					_dragging = true;
					ApplyPointerValue(pointerEvent.Position.X);
					pointerEvent.RequestPointerCapture();
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Up:
				if (pointerEvent.Button == MouseButton.Left && _dragging) {
					_dragging = false;
					_isHovered = HitTest(pointerEvent.Position);
					ApplyPointerValue(pointerEvent.Position.X);
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Click:
				break;
			case UiPointerEventKind.Wheel:
				_isHovered = HitTest(pointerEvent.Position);
				break;
		}
	}

	public bool Nudge(float direction) {
		if (direction == 0f) {
			return false;
		}

		var previous = Value;
		Value += MathF.Sign(direction) * Step;
		return Value != previous;
	}

	protected override void Draw() {
		if (Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var track = GetTrackBounds();
		var activeTrack = track;
		activeTrack.Width *= Value;
		var knob = GetKnobBounds();
		var mouse = Raylib.GetMousePosition();
		var hovered = _isHovered || ContainsPoint(mouse) || Raylib.CheckCollisionPointRec(mouse, knob);

		Raylib.DrawRectangleRounded(track, 0.5f, 8, TrackFill);
		if (activeTrack.Width > 0f) {
			Raylib.DrawRectangleRounded(activeTrack, 0.5f, 8, ActiveTrackFill);
		}

		Raylib.DrawRectangleRoundedLinesEx(track, 0.5f, 8, 1.25f, Outline);
		Raylib.DrawCircleV(new System.Numerics.Vector2(knob.X + (knob.Width * 0.5f), knob.Y + (knob.Height * 0.5f)), KnobRadius, hovered || _dragging ? Color.Gold : KnobFill);
		Raylib.DrawCircleLinesV(new System.Numerics.Vector2(knob.X + (knob.Width * 0.5f), knob.Y + (knob.Height * 0.5f)), KnobRadius, KnobOutline);

		UiDebugBounds.DrawInput(UiDebugBounds.Union(GlobalBounds, knob), Name);
	}

	private Rectangle GetTrackBounds() {
		var bounds = GlobalBounds;
		return new Rectangle(
			bounds.X,
			bounds.Y + ((bounds.Height - TrackHeight) * 0.5f),
			bounds.Width,
			TrackHeight);
	}

	private Rectangle GetKnobBounds() {
		var track = GetTrackBounds();
		var knobCenterX = track.X + (track.Width * Value);
		var knobDiameter = KnobRadius * 2f;
		return new Rectangle(
			knobCenterX - KnobRadius,
			track.Y + (track.Height * 0.5f) - KnobRadius,
			knobDiameter,
			knobDiameter);
	}

	private float NormalizeFromMouse(float mouseX) {
		var bounds = GlobalBounds;
		if (bounds.Width <= 0f) {
			return Value;
		}

		return Math.Clamp((mouseX - bounds.X) / bounds.Width, 0f, 1f);
	}

	private void ApplyPointerValue(float mouseX) {
		var previous = Value;
		Value = NormalizeFromMouse(mouseX);
		if (Value == previous) {
			return;
		}

		_pendingValueChanged = true;
		ValueChanged?.Invoke(this);
	}
}
