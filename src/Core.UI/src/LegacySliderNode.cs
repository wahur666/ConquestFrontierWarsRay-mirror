using System.Numerics;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored discrete slider that mirrors the Trim-era slider behavior.
/// </summary>
/// <remarks>
/// <para>
/// The control moves in integer steps, can be horizontal or vertical, supports
/// art-driven or primitive rendering, and can defer change emission until
/// pointer release.
/// </para>
/// <para>
/// Configure it from legacy GT data through <see cref="ApplyLegacyDefinition(GT_SLIDER,SLIDER_DATA,VfxAnimationDataRepository?)"/>.
/// Pointer routing comes from <see cref="UiEventSource"/>. Keyboard movement is
/// handled during update when this node has keyboard focus.
/// </para>
/// </remarks>
public sealed class LegacySliderNode : Control, IUiPointerEventHandler {
	public enum LegacySliderVisualState {
		Automatic,
		Disabled,
		Normal,
		Highlight,
		Alert
	}

	private const float DefaultPrimitiveTabWidth = 10f;
	private AtlasFramesResource? _art;
	private bool _dragging;
	private bool _enabled = true;
	private bool _hasKeyboardFocus;
	private bool _hovered;
	private bool _pendingDeferredEmit;
	private float _pointerAnchor;
	private float _positionOffset;
	private int _rangeMax = 10;
	private int _rangeMin;
	private int _sliderPosition;
	private float _tabWidth = DefaultPrimitiveTabWidth;
	private bool _visible = true;

	public LegacySliderNode(string? name = null) : base(name) {
	}

	/// <summary>
	/// Raised when the legacy slider would have posted its CQE_SLIDER message.
	/// </summary>
	public event Action<LegacySliderNode>? ValueChanged;

	/// <summary>
	/// Current integer slider value.
	/// </summary>
	public int SliderPosition => _sliderPosition;

	/// <summary>
	/// Inclusive lower bound for <see cref="SliderPosition"/>.
	/// </summary>
	public int RangeMin => _rangeMin;

	/// <summary>
	/// Inclusive upper bound for <see cref="SliderPosition"/>.
	/// </summary>
	public int RangeMax => _rangeMax;

	/// <summary>
	/// True for vertical sliders and false for horizontal sliders.
	/// </summary>
	public bool IsVertical { get; private set; }

	/// <summary>
	/// Legacy art indent between the end of the track and the thumb center.
	/// </summary>
	public float Indent { get; private set; }

	/// <summary>
	/// When true, drag changes are emitted only on pointer release.
	/// </summary>
	public bool DeferValueChangedUntilRelease { get; set; }

	/// <summary>
	/// Optional visual-state override for preview scenes.
	/// </summary>
	public LegacySliderVisualState VisualStateOverride { get; set; }

	/// <summary>
	/// Enables or disables pointer and keyboard interaction.
	/// </summary>
	public bool Enabled {
		get => _enabled;
		set {
			_enabled = value;
			if (!_enabled) {
				_dragging = false;
				_hovered = false;
			}
		}
	}

	/// <summary>
	/// Optional control ID carried over from legacy menus.
	/// </summary>
	public uint ControlId { get; set; }

	/// <summary>
	/// True when the node can participate in routed pointer input.
	/// </summary>
	public bool IsPointerInputEnabled => Visible && _visible && _enabled && Size.X > 0f && Size.Y > 0f;

	/// <summary>
	/// Primitive disabled/normal/highlight/alert colors from the legacy archetype.
	/// </summary>
	public Color[] StateColors { get; } = [
		new Color(100, 100, 100, 255),
		new Color(255, 0, 0, 255),
		new Color(255, 255, 0, 255),
		new Color(0, 0, 255, 255)
	];

	/// <summary>
	/// Configures the slider from one GT archetype plus one authored placement record.
	/// </summary>
	public void ApplyLegacyDefinition(GT_SLIDER archetype, SLIDER_DATA data, VfxAnimationDataRepository? repository = null) {
		ArgumentNullException.ThrowIfNull(archetype);
		ArgumentNullException.ThrowIfNull(data);

		DisposeArt();

		StateColors[0] = ToColor(archetype.DisabledColor);
		StateColors[1] = ToColor(archetype.NormalColor);
		StateColors[2] = ToColor(archetype.HighlightColor);
		StateColors[3] = ToColor(archetype.AlertColor);
		IsVertical = archetype.Vertical;
		Indent = archetype.Indent;

		if (!string.IsNullOrWhiteSpace(archetype.ShapeFile) && repository is not null) {
			LoadArt(repository, archetype.ShapeFile);
		}

		var localWidth = GetConfiguredWidth(data.ScreenRect);
		var localHeight = GetConfiguredHeight(data.ScreenRect);

		Size = new Vector2(localWidth, localHeight);
		Position = new Vector2(
			data.ScreenRect.Left + data.XOrigin,
			data.ScreenRect.Top + data.YOrigin);

		_tabWidth = ResolveTabWidth();
		SetSliderPosition(_sliderPosition, emitEvent: false);
	}

	/// <summary>
	/// Changes the inclusive slider range.
	/// </summary>
	public void SetRange(int min, int max) {
		if (max < min) {
			throw new ArgumentOutOfRangeException(nameof(max), "Range max must be greater than or equal to range min.");
		}

		_rangeMin = min;
		_rangeMax = max;
		SetSliderPosition(_sliderPosition, emitEvent: false);
	}

	/// <summary>
	/// Sets the current value and optionally raises <see cref="ValueChanged"/>.
	/// </summary>
	public void SetSliderPosition(int value, bool emitEvent = true) {
		_pendingDeferredEmit = false;
		ApplySliderPosition(value, emitEvent);
	}

	/// <summary>
	/// Grants or clears keyboard focus for arrow-key movement.
	/// </summary>
	public void SetKeyboardFocus(bool enabled) {
		_hasKeyboardFocus = enabled && _enabled && _visible && Visible;
		if (!_hasKeyboardFocus) {
			_dragging = false;
		}
	}

	public bool HitTest(Vector2 screenPoint) {
		return ContainsPoint(screenPoint) || Raylib.CheckCollisionPointRec(screenPoint, GetThumbBounds());
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
				_hovered = true;
				break;
			case UiPointerEventKind.Leave:
				_hovered = false;
				if (!_dragging) {
					_positionOffset = 0f;
				}
				break;
			case UiPointerEventKind.Move:
				_hovered = HitTest(pointerEvent.Position);
				if (_dragging && pointerEvent.Button is null) {
					ApplyPointerMotion(pointerEvent.Position);
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left && HitTest(pointerEvent.Position) && _enabled) {
					_hovered = true;
					_dragging = true;
					_hasKeyboardFocus = true;
					_positionOffset = 0f;
					_pendingDeferredEmit = false;
					SnapPointerToNearestStep(pointerEvent.Position, emitEvent: !DeferValueChangedUntilRelease);
					_pointerAnchor = GetPointerAxis(pointerEvent.Position);
					pointerEvent.RequestPointerCapture();
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Up:
				if (pointerEvent.Button == MouseButton.Left && _dragging) {
					_dragging = false;
					_hovered = HitTest(pointerEvent.Position);
					_positionOffset = 0f;

					if (DeferValueChangedUntilRelease && _pendingDeferredEmit) {
						_pendingDeferredEmit = false;
						ValueChanged?.Invoke(this);
					}

					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Click:
				_hovered = HitTest(pointerEvent.Position);
				break;
			case UiPointerEventKind.Wheel:
				_hovered = HitTest(pointerEvent.Position);
				break;
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (!_enabled || !_visible || !Visible || !_hasKeyboardFocus) {
			return;
		}

		var snapshot = Input.CaptureUiSnapshot();

		if (!IsVertical) {
			if (snapshot.NavigateLeft) {
				SetSliderPosition(_sliderPosition - 1);
			}

			if (snapshot.NavigateRight) {
				SetSliderPosition(_sliderPosition + 1);
			}

			return;
		}

		if (snapshot.NavigateUp) {
			SetSliderPosition(_sliderPosition - 1);
		}

		if (snapshot.NavigateDown) {
			SetSliderPosition(_sliderPosition + 1);
		}
	}

	protected override void Draw() {
		if (!_visible || !Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		var thumb = GetThumbBounds();
		var stateIndex = ResolveStateIndex();

		if (_art is not null) {
			DrawArt(bounds, thumb, stateIndex);
			return;
		}

		DrawPrimitive(bounds, thumb, StateColors[stateIndex]);
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private void ApplyPointerMotion(Vector2 pointerPosition) {
		if (!_dragging || _rangeMax <= _rangeMin) {
			return;
		}

		var pointerAxis = GetPointerAxis(pointerPosition);
		_positionOffset += pointerAxis - _pointerAnchor;
		_pointerAnchor = pointerAxis;

		var stepSize = GetStepSize();
		if (MathF.Abs(_positionOffset) < stepSize) {
			return;
		}

		var delta = _positionOffset > 0f ? 1 : -1;
		ApplySliderPosition(_sliderPosition + delta, emitEvent: !DeferValueChangedUntilRelease);
		_positionOffset = 0f;
		_pointerAnchor = GetCurrentThumbCenter();
	}

	private void ApplySliderPosition(int value, bool emitEvent) {
		var clamped = ClampToRange(value);
		if (clamped == _sliderPosition) {
			return;
		}

		_sliderPosition = clamped;
		if (emitEvent) {
			ValueChanged?.Invoke(this);
			return;
		}

		_pendingDeferredEmit = true;
	}

	private int ClampToRange(int value) {
		if (_rangeMax < _rangeMin) {
			return value;
		}

		return Math.Clamp(value, _rangeMin, _rangeMax);
	}

	private void DisposeArt() {
		_art?.Dispose();
		_art = null;
	}

	private void DrawArt(Rectangle bounds, Rectangle thumb, int stateIndex) {
		var frames = _art!.Frames;
		DrawFrame(frames.GetFrameRegion(stateIndex), bounds);
		DrawFrame(frames.GetFrameRegion(stateIndex + 4), thumb);
	}

	private void DrawFrame(Rectangle source, Rectangle destination) {
		var slice = _art!.Texture.GetSlice();
		Raylib.DrawTexturePro(
			slice.Texture,
			source,
			destination,
			Vector2.Zero,
			0f,
			Color.White);
	}

	private void DrawPrimitive(Rectangle bounds, Rectangle thumb, Color color) {
		Raylib.DrawRectangleRec(bounds, Color.Black);
		Raylib.DrawRectangleLinesEx(bounds, 1f, color);
		DrawHatchFill(thumb, color);
		Raylib.DrawRectangleLinesEx(thumb, 1f, color);
	}

	private static void DrawHatchFill(Rectangle bounds, Color color) {
		var x0 = (int)MathF.Round(bounds.X);
		var y0 = (int)MathF.Round(bounds.Y);
		var x1 = (int)MathF.Round(bounds.X + bounds.Width);
		var y1 = (int)MathF.Round(bounds.Y + bounds.Height);

		for (var x = x0 - (y1 - y0); x < x1; x += 4) {
			Raylib.DrawLine(x, y1, x + (y1 - y0), y0, color);
		}
	}

	private float GetConfiguredHeight(RECT screenRect) {
		if (_art is not null) {
			return _art.Frames.GetFrameRegion(0).Height;
		}

		return Math.Max(0f, screenRect.Bottom - screenRect.Top);
	}

	private float GetConfiguredWidth(RECT screenRect) {
		if (_art is not null) {
			return _art.Frames.GetFrameRegion(0).Width;
		}

		return Math.Max(0f, screenRect.Right - screenRect.Left);
	}

	private float GetCurrentThumbCenter() {
		var thumb = GetThumbBounds();
		return IsVertical
			? thumb.Y + (thumb.Height * 0.5f)
			: thumb.X + (thumb.Width * 0.5f);
	}

	private float GetLogicalMaximum() {
		var bounds = GlobalBounds;
		var halfTab = GetThumbHalfExtent();

		return IsVertical
			? bounds.Y + halfTab + Indent
			: bounds.X + bounds.Width - halfTab - Indent;
	}

	private float GetLogicalMinimum() {
		var bounds = GlobalBounds;
		var halfTab = GetThumbHalfExtent();

		return IsVertical
			? bounds.Y + bounds.Height - halfTab - Indent
			: bounds.X + halfTab + Indent;
	}

	private float GetPointerAxis(Vector2 pointerPosition) {
		return IsVertical ? pointerPosition.Y : pointerPosition.X;
	}

	private float GetStepSize() {
		var span = MathF.Abs(GetLogicalMaximum() - GetLogicalMinimum());
		var denominator = _rangeMax - _rangeMin;
		if (denominator <= 0) {
			return 1f;
		}

		return Math.Max(1f, span / denominator);
	}

	private void SnapPointerToNearestStep(Vector2 pointerPosition, bool emitEvent) {
		if (_rangeMax <= _rangeMin) {
			return;
		}

		var pointerAxis = GetPointerAxis(pointerPosition);
		var logicalMin = GetLogicalMinimum();
		var logicalMax = GetLogicalMaximum();
		var span = logicalMax - logicalMin;
		if (MathF.Abs(span) <= float.Epsilon) {
			return;
		}

		var ratio = (pointerAxis - logicalMin) / span;
		ratio = Math.Clamp(ratio, 0f, 1f);

		var range = _rangeMax - _rangeMin;
		var snappedOffset = (int)MathF.Round(ratio * range, MidpointRounding.AwayFromZero);
		ApplySliderPosition(_rangeMin + snappedOffset, emitEvent);
	}

	private Rectangle GetThumbBounds() {
		var bounds = GlobalBounds;
		var center = GetThumbCenterFromValue();

		if (IsVertical) {
			var height = _tabWidth;
			return new Rectangle(bounds.X, center - (height * 0.5f), bounds.Width, height);
		}

		var width = _tabWidth;
		return new Rectangle(center - (width * 0.5f), bounds.Y, width, bounds.Height);
	}

	private float GetThumbCenterFromValue() {
		var logicalMin = GetLogicalMinimum();
		var logicalMax = GetLogicalMaximum();
		var denominator = _rangeMax - _rangeMin;
		if (denominator <= 0) {
			return logicalMin;
		}

		var ratio = (_sliderPosition - _rangeMin) / (float)denominator;
		ratio = Math.Clamp(ratio, 0f, 1f);
		return logicalMin + (ratio * (logicalMax - logicalMin));
	}

	private float GetThumbHalfExtent() {
		return _tabWidth * 0.5f;
	}

	private void LoadArt(VfxAnimationDataRepository repository, string legacyShapeFile) {
		if (!repository.TryGetAtlasByShapeFile(legacyShapeFile, out var atlasEntry)) {
			return;
		}

		_art = new AtlasFramesResource(
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: false),
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: true),
			TextureFilter.Point);
		_ = _art.Frames.Count;
	}

	private int ResolveStateIndex() {
		switch (VisualStateOverride) {
			case LegacySliderVisualState.Disabled:
				return 0;
			case LegacySliderVisualState.Normal:
				return 1;
			case LegacySliderVisualState.Highlight:
				return 2;
			case LegacySliderVisualState.Alert:
				return 3;
		}

		if (!_enabled) {
			return 0;
		}

		if (_hasKeyboardFocus) {
			return 2;
		}

		if (_hovered || HitTest(Raylib.GetMousePosition())) {
			return 3;
		}

		return 1;
	}

	private float ResolveTabWidth() {
		if (_art is null) {
			return DefaultPrimitiveTabWidth;
		}

		var thumbFrame = _art.Frames.GetFrameRegion(4);
		return IsVertical ? thumbFrame.Height : thumbFrame.Width;
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color((int)color.Red, (int)color.Green, (int)color.Blue, 255);
	}
}
