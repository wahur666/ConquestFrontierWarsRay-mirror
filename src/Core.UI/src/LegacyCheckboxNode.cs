using System.Numerics;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored checkbox control that keeps the base frame visible and overlays the authored checkmark frame when checked.
/// </summary>
public sealed class LegacyCheckboxNode : Control, IUiPointerEventHandler, ILegacyKeyboardFocusable {
	private const float PrimitiveCheckboxInset = 3f;
	private static readonly Color KeyboardFocusColor = new(181, 218, 240, 255);
	private AtlasFramesResource? _art;
	private GT_BUTTON_TYPE _buttonType;
	private bool _enabled = true;
	private bool _hovered;
	private bool _lastHovered;
	private bool _keyboardFocus;
	private bool _keyboardPressed;
	private bool _pointerPressed;
	private bool _visible = true;

	public LegacyCheckboxNode(string? name = null) : base(name) {
	}

	public event Action<LegacyCheckboxNode>? Activated;
	public event Action<LegacyCheckboxNode>? Entered;
	public event Action<LegacyCheckboxNode>? Exited;

	public bool IsChecked { get; set; }
	public bool IsPointerInputEnabled => Visible && _visible && _enabled && Size.X > 0f && Size.Y > 0f;
	public Vector4 PointerHitInsets { get; private set; }
	public Rectangle GlobalHitBounds => ApplyInsets(GlobalBounds, PointerHitInsets);
	public Color DisabledTint { get; private set; } = new(150, 150, 150, 255);
	public Color HoverTint { get; private set; } = new(255, 255, 255, 255);
	public Color NormalTint { get; private set; } = new(255, 255, 255, 255);

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
		DisabledTint = ToDisabledTint(archetype.DisabledText);

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
			_lastHovered = false;
			_keyboardFocus = false;
			_keyboardPressed = false;
			_pointerPressed = false;
		}
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		if (!visible) {
			_hovered = false;
			_lastHovered = false;
			_keyboardPressed = false;
			_pointerPressed = false;
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

	public bool HitTest(Vector2 screenPoint) {
		return Raylib.CheckCollisionPointRec(screenPoint, GlobalHitBounds);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
			case UiPointerEventKind.Move:
				_hovered = HitTest(pointerEvent.Position);
				if (!_lastHovered && _hovered) {
					Entered?.Invoke(this);
				}
				_lastHovered = _hovered;
				break;
			case UiPointerEventKind.Leave:
				_hovered = false;
				if (_lastHovered) {
					Exited?.Invoke(this);
				}
				_lastHovered = false;
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left && _enabled && HitTest(pointerEvent.Position)) {
					_hovered = true;
					_pointerPressed = true;
					_keyboardFocus = true;
					pointerEvent.RequestPointerCapture();
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Up:
				if (pointerEvent.Button == MouseButton.Left && _pointerPressed) {
					var releaseInside = HitTest(pointerEvent.Position);
					_pointerPressed = false;
					_hovered = releaseInside;
					if (releaseInside) {
						Activated?.Invoke(this);
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
		}

		if (_keyboardPressed && !acceptDown) {
			_keyboardPressed = false;
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
		} else {
			DrawPrimitive(bounds, visualState);
		}

		UiDebugBounds.DrawInput(bounds, Name);
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private void DrawArt(Rectangle bounds, VisualState state) {
		var slice = _art!.Texture.GetSlice();
		var baseSource = _art.Frames.GetFrameRegion(state switch {
			VisualState.Disabled => 0,
			VisualState.Depressed => 2,
			VisualState.Hovered => 2,
			_ => 1
		});
		var tint = state == VisualState.Disabled ? DisabledTint : Color.White;

		Raylib.DrawTexturePro(slice.Texture, baseSource, bounds, Vector2.Zero, 0f, tint);

		if (IsChecked && _art.Frames.Count >= 4) {
			var checkSource = _art.Frames.GetFrameRegion(3);
			var destination = CenterOverlay(bounds, checkSource);
			Raylib.DrawTexturePro(slice.Texture, checkSource, destination, Vector2.Zero, 0f, tint);
		}

		if (_keyboardFocus && HasUsableFocusOverlay(baseSource)) {
			var focusSource = _art.Frames.GetFrameRegion(Math.Min(4, _art.Frames.Count - 1));
			Raylib.DrawTexturePro(slice.Texture, focusSource, bounds, Vector2.Zero, 0f, KeyboardFocusColor);
		}
	}

	private void DrawPrimitive(Rectangle bounds, VisualState state) {
		var outline = state == VisualState.Disabled ? DisabledTint : KeyboardFocusColor;
		if (state is VisualState.Normal or VisualState.Hovered or VisualState.Depressed) {
			outline = Color.White;
		}

		Raylib.DrawRectangleRec(bounds, new Color(0, 0, 0, 255));
		Raylib.DrawRectangleLinesEx(bounds, 1f, outline);

		if (!IsChecked) {
			return;
		}

		var x1 = bounds.X + PrimitiveCheckboxInset;
		var y1 = bounds.Y + PrimitiveCheckboxInset;
		var x2 = bounds.X + bounds.Width - PrimitiveCheckboxInset;
		var y2 = bounds.Y + bounds.Height - PrimitiveCheckboxInset;
		Raylib.DrawLineEx(new Vector2(x1, y1), new Vector2(x2, y2), 1f, new Color(255, 255, 0, 255));
		Raylib.DrawLineEx(new Vector2(x1, y2), new Vector2(x2, y1), 1f, new Color(255, 255, 0, 255));
	}

	private VisualState ResolveVisualState() {
		if (!_enabled) {
			return VisualState.Disabled;
		}

		if ((_hovered && _pointerPressed) || _keyboardPressed) {
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

	private static Rectangle CenterOverlay(Rectangle bounds, Rectangle overlaySource) {
		var width = Math.Min(bounds.Width, overlaySource.Width);
		var height = Math.Min(bounds.Height, overlaySource.Height);
		return new Rectangle(
			bounds.X + ((bounds.Width - width) * 0.5f),
			bounds.Y + ((bounds.Height - height) * 0.5f),
			width,
			height);
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

	private static Color ToDisabledTint(GT_COLOR color) {
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
