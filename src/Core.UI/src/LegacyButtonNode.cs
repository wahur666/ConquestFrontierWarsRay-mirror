using System.Numerics;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored button control that mirrors the Trim-era Button2 baseline closely enough for menu composition.
/// </summary>
public sealed class LegacyButtonNode : Control, IUiPointerEventHandler, ILegacyKeyboardFocusable {
	public enum LegacyButtonVisualState {
		Automatic,
		Disabled,
		Normal,
		Hovered,
		Depressed,
		KeyboardFocus
	}

	private const float DefaultFontSize = 16f;
	private const float DefaultTextInset = 6f;
	private const float PrimitiveDropdownInset = 16f;
	private const float PrimitiveCheckboxInset = 3f;
	private const float RepeatIntervalSeconds = 0.1f;
	private static readonly Color KeyboardFocusColor = new(181, 218, 240, 255);
	private AtlasFramesResource? _art;
	private GT_BUTTON_TYPE _buttonType;
	private bool _enabled = true;
	private bool _hovered;
	private bool _lastHovered;
	private bool _keyboardFocus;
	private bool _keyboardPressed;
	private bool _pointerPressed;
	private float _repeatAccumulator;
	private bool _visible = true;
	private float _resolvedFontSize = DefaultFontSize;

	public LegacyButtonNode(string? name = null) : base(name) {
	}

	public event Action<LegacyButtonNode>? Activated;
	public event Action<LegacyButtonNode>? RepeatActivated;

	public event Action<LegacyButtonNode>? Entered;
	public event Action<LegacyButtonNode>? Exited;

	public uint ControlId { get; set; }
	public string Text { get; set; } = string.Empty;
	public Color DisabledTextColor { get; private set; } = new(100, 100, 100, 255);
	public Color NormalTextColor { get; private set; } = new(180, 160, 120, 255);
	public Color HighlightTextColor { get; private set; } = new(200, 180, 140, 255);
	public bool Transparent { get; set; }
	public bool ColorLocked { get; private set; }
	public bool ForceDropdownIndicator { get; set; }
	public bool PushState { get; set; }
	public LegacyButtonVisualState VisualStateOverride { get; set; }
	public bool IsPointerInputEnabled => Visible && _visible && _enabled && Size.X > 0f && Size.Y > 0f;
	public Vector4 PointerHitInsets { get; private set; }
	public int LeftMargin { get; private set; }
	public int RightMargin { get; private set; }
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;

	public Rectangle GlobalHitBounds => ApplyInsets(GlobalBounds, PointerHitInsets);

	public void SetPointerHitInsets(float left = 0f, float top = 0f, float right = 0f, float bottom = 0f) {
		PointerHitInsets = new Vector4(
			Math.Max(0f, left),
			Math.Max(0f, top),
			Math.Max(0f, right),
			Math.Max(0f, bottom));
	}

	public void ApplyLegacyDefinition(GT_BUTTON archetype, BUTTON_DATA? data = null, VfxAnimationDataRepository? repository = null) {
		ArgumentNullException.ThrowIfNull(archetype);

		DisposeArt();

		_buttonType = archetype.ButtonType;
		DisabledTextColor = ToColor(archetype.DisabledText);
		NormalTextColor = ToColor(archetype.NormalText);
		HighlightTextColor = ToColor(archetype.HighlightText);
		LeftMargin = Math.Max(0, (int)archetype.LeftMargin);
		RightMargin = Math.Max(0, (int)archetype.RightMargin);
		_resolvedFontSize = ResolveFontSize(archetype.FontName);

		if (!string.IsNullOrWhiteSpace(archetype.ShapeFile) && repository is not null) {
			_art = LoadArt(repository, archetype.ShapeFile);
		}

		if (_art is not null) {
			var frame = _art.Frames.GetFrameRegion(1);
			Size = new Vector2(frame.Width, frame.Height);
		}

		if (data is null) {
			return;
		}

		Position = new Vector2(data.XOrigin, data.YOrigin);

		if (_art is null) {
			var width = Math.Max(0f, data.ButtonArea.Right - data.ButtonArea.Left);
			var height = Math.Max(0f, data.ButtonArea.Bottom - data.ButtonArea.Top);
			Size = new Vector2(width, height);
		}
	}

	public void EnableButton(bool enabled) {
		_enabled = enabled;
		if (!_enabled) {
			_hovered = false;
			_lastHovered = _hovered;
			_keyboardFocus = false;
			_keyboardPressed = false;
			_pointerPressed = false;
			_repeatAccumulator = 0f;
		}
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		if (!visible) {
			_hovered = false;
			_lastHovered = _hovered;
			_keyboardPressed = false;
			_pointerPressed = false;
			_repeatAccumulator = 0f;
		}
	}

	public bool SetKeyboardFocus(bool enabled) {
		if (enabled && (!_enabled || !_visible || !Visible)) {
			return false;
		}

		_keyboardFocus = enabled;
		if (!enabled) {
			_keyboardPressed = false;
		}

		return true;
	}

	public void SetDefaultColor(Color color) {
		NormalTextColor = color;
		ColorLocked = true;
	}

	public bool HitTest(Vector2 screenPoint) {
		return Raylib.CheckCollisionPointRec(screenPoint, GlobalHitBounds);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
			case UiPointerEventKind.Move:
				_hovered = HitTest(pointerEvent.Position);
				if (!_lastHovered && _hovered) {
					EmitEntered();
				}
				_lastHovered = _hovered;
				break;
			case UiPointerEventKind.Leave:
				_hovered = false;
				if (_lastHovered) {
					EmitExited();
				}

				_lastHovered = _hovered;
				if (!_pointerPressed) {
					_repeatAccumulator = 0f;
				}
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left && _enabled && HitTest(pointerEvent.Position)) {
					_hovered = true;
					_pointerPressed = true;
					_keyboardFocus = true;
					_repeatAccumulator = 0f;
					if (TriggersOnPress()) {
						EmitActivated();
					}

					pointerEvent.RequestPointerCapture();
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Up:
				if (pointerEvent.Button == MouseButton.Left && _pointerPressed) {
					var releaseInside = HitTest(pointerEvent.Position);
					_pointerPressed = false;
					_hovered = releaseInside;
					_repeatAccumulator = 0f;
					if (releaseInside && TriggersOnRelease()) {
						EmitActivated();
					}

					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Click:
				_hovered = HitTest(pointerEvent.Position);
				break;
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (!_enabled || !_visible || !Visible) {
			return;
		}

		var acceptDown = Input.IsActionPressed(InputManager.UiAcceptAction) || Raylib.IsKeyDown(KeyboardKey.Space);
		var acceptPressed = Input.IsActionJustPressed(InputManager.UiAcceptAction) || Raylib.IsKeyPressed(KeyboardKey.Space);

		if (_keyboardFocus && !_keyboardPressed && acceptPressed) {
			_keyboardPressed = true;
			_repeatAccumulator = 0f;
			if (TriggersOnPress()) {
				EmitActivated();
			}
		}

		if (_keyboardPressed && !acceptDown) {
			_keyboardPressed = false;
			_repeatAccumulator = 0f;
			if (TriggersOnRelease()) {
				EmitActivated();
			}
		}

		if (!IsRepeatingTriggerHeld()) {
			return;
		}

		_repeatAccumulator += deltaTime;
		while (_repeatAccumulator >= RepeatIntervalSeconds) {
			_repeatAccumulator -= RepeatIntervalSeconds;
			RepeatActivated?.Invoke(this);
			Activated?.Invoke(this);
		}
	}

	protected override void Draw() {
		if (!_visible || !Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		var visualState = ResolveVisualState();

		if (_art is not null) {
			DrawArt(bounds, visualState);
		} else if (!Transparent) {
			DrawPrimitive(bounds, visualState);
		}

		if (!string.IsNullOrEmpty(Text)) {
			DrawText(bounds, visualState);
		}

		UiDebugBounds.DrawInput(bounds, Name);
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private bool TriggersOnPress() {
		return _buttonType is GT_BUTTON_TYPE.PUSHPIN or GT_BUTTON_TYPE.REPEATER;
	}

	private bool TriggersOnRelease() {
		return _buttonType is GT_BUTTON_TYPE.DEFAULT or GT_BUTTON_TYPE.CHECKBOX;
	}

	private bool IsRepeatingTriggerHeld() {
		return _buttonType == GT_BUTTON_TYPE.REPEATER && (_pointerPressed || _keyboardPressed || PushState);
	}

	private void EmitActivated() {
		Activated?.Invoke(this);
	}

	private void EmitEntered() {
		Entered?.Invoke(this);
	}

	private void EmitExited() {
		Exited?.Invoke(this);
	}

	private void DrawArt(Rectangle bounds, VisualState state) {
		var slice = _art!.Texture.GetSlice();
		var normalSource = _art.Frames.GetFrameRegion(1);
		var source = _art.Frames.GetFrameRegion(state switch {
			VisualState.Disabled => 0,
			VisualState.Depressed => 3,
			VisualState.Hovered => 2,
			_ => 1
		});

		Raylib.DrawTexturePro(slice.Texture, source, bounds, Vector2.Zero, 0f, Color.White);

		if (_buttonType == GT_BUTTON_TYPE.CHECKBOX && PushState) {
			var depressedSource = _art.Frames.GetFrameRegion(3);
			Raylib.DrawTexturePro(slice.Texture, depressedSource, bounds, Vector2.Zero, 0f, Color.White);
		}

		if (_keyboardFocus && HasUsableFocusOverlay(normalSource)) {
			var focusSource = _art.Frames.GetFrameRegion(Math.Min(4, _art.Frames.Count - 1));
			Raylib.DrawTexturePro(slice.Texture, focusSource, bounds, Vector2.Zero, 0f, Color.White);
		}
	}

	private void DrawPrimitive(Rectangle bounds, VisualState state) {
		var textColor = ResolveTextColor(state);
		var outline = textColor;
		var fill = new Color(0, 0, 0, 255);

		Raylib.DrawRectangleRec(bounds, fill);
		Raylib.DrawRectangleLinesEx(bounds, 1f, outline);

		if (_buttonType == GT_BUTTON_TYPE.CHECKBOX && PushState) {
			var x1 = bounds.X + PrimitiveCheckboxInset;
			var y1 = bounds.Y + PrimitiveCheckboxInset;
			var x2 = bounds.X + bounds.Width - PrimitiveCheckboxInset;
			var y2 = bounds.Y + bounds.Height - PrimitiveCheckboxInset;
			Raylib.DrawLineEx(new Vector2(x1, y1), new Vector2(x2, y2), 1f, new Color(255, 255, 0, 255));
			Raylib.DrawLineEx(new Vector2(x1, y2), new Vector2(x2, y1), 1f, new Color(255, 255, 0, 255));
		}
	}

	private void DrawText(Rectangle bounds, VisualState state) {
		var color = ResolveTextColor(state);
		var safeText = TrimTextToFit(Text, bounds.Width);
		var textWidth = UiText.MeasureWidth(safeText, _resolvedFontSize, TextStyle);
		var availableWidth = Math.Max(0f, bounds.Width - LeftMargin - RightMargin - (ShowsDropdownIndicator() ? PrimitiveDropdownInset : 0f));
		var textX = bounds.X + LeftMargin + Math.Max(0f, (availableWidth - textWidth) * 0.5f);
		var textY = bounds.Y + ((bounds.Height - _resolvedFontSize) * 0.5f) - 1f;

		if (state == VisualState.Depressed && _buttonType != GT_BUTTON_TYPE.CHECKBOX) {
			textX += 1f;
			textY += 1f;
		}

		UiText.Draw(safeText, textX, textY, _resolvedFontSize, color, TextStyle);

		if (ShowsDropdownIndicator()) {
			var arrowX = bounds.X + bounds.Width - Math.Max(PrimitiveDropdownInset, RightMargin + 6f);
			UiText.Draw("v", arrowX, textY, _resolvedFontSize, color, TextStyle);
		}
	}

	private bool ShowsDropdownIndicator() {
		return ForceDropdownIndicator && _art is null;
	}

	private string TrimTextToFit(string text, float width) {
		if (string.IsNullOrEmpty(text)) {
			return string.Empty;
		}

		var maxWidth = Math.Max(0f, width - LeftMargin - RightMargin - DefaultTextInset - (ShowsDropdownIndicator() ? PrimitiveDropdownInset : 0f));
		if (UiText.MeasureWidth(text, _resolvedFontSize, TextStyle) <= maxWidth) {
			return text;
		}

		const string ellipsis = "...";
		for (var length = text.Length - 1; length > 0; length--) {
			var candidate = text[..length] + ellipsis;
			if (UiText.MeasureWidth(candidate, _resolvedFontSize, TextStyle) <= maxWidth) {
				return candidate;
			}
		}

		return ellipsis;
	}

	private VisualState ResolveVisualState() {
		switch (VisualStateOverride) {
			case LegacyButtonVisualState.Disabled:
				return VisualState.Disabled;
			case LegacyButtonVisualState.Normal:
				return VisualState.Normal;
			case LegacyButtonVisualState.Hovered:
				return VisualState.Hovered;
			case LegacyButtonVisualState.Depressed:
				return VisualState.Depressed;
			case LegacyButtonVisualState.KeyboardFocus:
				return VisualState.KeyboardFocus;
		}

		if (!_enabled) {
			return VisualState.Disabled;
		}

		var depressed = (_hovered && _pointerPressed) || _keyboardPressed || (_buttonType != GT_BUTTON_TYPE.DEFAULT && PushState);
		if (depressed && (PushState || _buttonType != GT_BUTTON_TYPE.CHECKBOX)) {
			return VisualState.Depressed;
		}

		if (_hovered) {
			return VisualState.Hovered;
		}

		if (_keyboardFocus) {
			return VisualState.KeyboardFocus;
		}

		return VisualState.Normal;
	}

	private Color ResolveTextColor(VisualState state) {
		if (ColorLocked) {
			return NormalTextColor;
		}

		return state switch {
			VisualState.Disabled => DisabledTextColor,
			VisualState.Hovered => HighlightTextColor,
			VisualState.Depressed => _hovered ? HighlightTextColor : NormalTextColor,
			VisualState.KeyboardFocus => KeyboardFocusColor,
			_ => NormalTextColor
		};
	}

	private void DisposeArt() {
		_art?.Dispose();
		_art = null;
	}

	private bool HasUsableFocusOverlay(Rectangle normalSource) {
		if (_art is null || _art.Frames.Count < 5) {
			return false;
		}

		var focusSource = _art.Frames.GetFrameRegion(4);
		return focusSource.Width >= (normalSource.Width * 0.5f)
			&& focusSource.Height >= (normalSource.Height * 0.5f);
	}

	private static AtlasFramesResource? LoadArt(VfxAnimationDataRepository repository, string shapeId) {
		if (!repository.TryGetAtlasByShapeId(shapeId, out var atlasEntry)) {
			return null;
		}

		return new AtlasFramesResource(
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: false),
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: true),
			TextureFilter.Point);
	}

	private static float ResolveFontSize(string fontName) {
		return fontName switch {
			"Font!!Button" => 16f,
			"Font!!Button3D" => 16f,
			"Font!!MessageFutureReserved" => 16f,
			_ => DefaultFontSize
		};
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color(color.Red, color.Green, color.Blue, (byte)255);
	}

	private static Rectangle ApplyInsets(Rectangle bounds, Vector4 insets) {
		var x = bounds.X + insets.X;
		var y = bounds.Y + insets.Y;
		var width = Math.Max(0f, bounds.Width - insets.X - insets.Z);
		var height = Math.Max(0f, bounds.Height - insets.Y - insets.W);
		return new Rectangle(x, y, width, height);
	}

	private enum VisualState {
		Disabled,
		Normal,
		Hovered,
		Depressed,
		KeyboardFocus
	}
}
