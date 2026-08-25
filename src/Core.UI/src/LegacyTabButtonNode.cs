using System.Numerics;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored tab header plus page-local focus scope.
/// </summary>
public sealed class LegacyTabButtonNode : Control, IUiPointerEventHandler {
	private const float DefaultFontSize = 15f;
	private static readonly Color DefaultNormalColor = new(63, 125, 180, 255);
	private static readonly Color DefaultHighlightColor = new(0, 192, 255, 255);
	private static readonly Color DefaultSelectedColor = new(181, 218, 240, 255);
	private readonly List<ILegacyKeyboardFocusable> _focusables = [];
	private readonly Node2D _pageRoot;
	private AtlasFramesResource? _art;
	private bool _enabled = true;
	private int _focusedIndex = -1;
	private int _frameStartIndex;
	private bool _headerHovered;
	private bool _hasKeyboardFocus;
	private bool _headerPressed;
	private bool _keyboardFocusEnabled;
	private bool _visible = true;
	private float _fontSize = DefaultFontSize;

	public LegacyTabButtonNode(string? name = null) : base(name) {
		_pageRoot = AddChild(new Node2D($"{Name}Page"));
		_pageRoot.Visible = false;
	}

	public event Action<LegacyTabButtonNode>? Activated;

	public int TabIndex { get; internal set; }

	public bool IsSelected { get; private set; }

	public string Text { get; set; } = string.Empty;

	public Vector2 PageOffset {
		get => _pageRoot.Position;
		set => _pageRoot.Position = value;
	}

	public Color NormalTextColor { get; private set; } = DefaultNormalColor;

	public Color HighlightTextColor { get; private set; } = DefaultHighlightColor;

	public Color SelectedTextColor { get; private set; } = DefaultSelectedColor;

	public bool IsPointerInputEnabled => Visible && _visible && _enabled && Size.X > 0f && Size.Y > 0f;

	public void ApplyLegacyDefinition(
		GT_HOTBUTTON hotButtonArchetype,
		GT_VFXSHAPE shapeArchetype,
		HOTBUTTON_DATA data,
		int tabFrameStart,
		VfxAnimationDataRepository? repository = null) {
		ArgumentNullException.ThrowIfNull(hotButtonArchetype);
		ArgumentNullException.ThrowIfNull(shapeArchetype);
		ArgumentNullException.ThrowIfNull(data);

		DisposeArt();

		FontSizeFromHotButton(hotButtonArchetype.FontType);
		_frameStartIndex = tabFrameStart;
		if (!string.IsNullOrWhiteSpace(shapeArchetype.Filename) && repository is not null) {
			_art = LoadArt(repository, shapeArchetype.Filename);
		}

		Position = new Vector2(data.XOrigin, data.YOrigin);
		if (_art is not null && tabFrameStart >= 0 && tabFrameStart < _art.Frames.Count) {
			var frame = _art.Frames.GetFrameRegion(tabFrameStart);
			Size = new Vector2(frame.Width, frame.Height);
		}
	}

	public T AddPageChild<T>(T child, bool registerForFocus = true) where T : Node {
		ArgumentNullException.ThrowIfNull(child);
		var added = _pageRoot.AddChild(child);
		if (registerForFocus && child is ILegacyKeyboardFocusable focusable) {
			RegisterFocusable(focusable);
		}

		return added;
	}

	public void RegisterFocusable(ILegacyKeyboardFocusable focusable) {
		ArgumentNullException.ThrowIfNull(focusable);
		if (!_focusables.Contains(focusable)) {
			_focusables.Add(focusable);
		}
	}

	public void SetTextColors(Color normal, Color highlight, Color selected) {
		NormalTextColor = normal;
		HighlightTextColor = highlight;
		SelectedTextColor = selected;
	}

	public void SetDefaultFocusControl(ILegacyKeyboardFocusable? focusable) {
		if (focusable is null) {
			_focusedIndex = -1;
			return;
		}

		RegisterFocusable(focusable);
		_focusedIndex = _focusables.IndexOf(focusable);
	}

	public void EnableKeyboardFocusing() {
		_keyboardFocusEnabled = true;
	}

	public void EnableButton(bool enabled) {
		_enabled = enabled;
		if (!enabled) {
			ClearFocus();
			_headerHovered = false;
			_headerPressed = false;
		}
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		if (!visible) {
			ClearFocus();
			_headerHovered = false;
			_headerPressed = false;
		}

		_pageRoot.Visible = visible && IsSelected;
	}

	public void SetSelected(bool selected) {
		if (IsSelected == selected) {
			if (selected && _keyboardFocusEnabled && _hasKeyboardFocus && _focusedIndex < 0) {
				FocusFirstAvailable();
			}

			return;
		}

		IsSelected = selected;
		_pageRoot.Visible = _visible && selected;
		if (!selected) {
			ClearFocus();
			return;
		}

		if (_keyboardFocusEnabled && _hasKeyboardFocus) {
			FocusFirstAvailable();
		}
	}

	public void SetKeyboardFocusActive(bool enabled) {
		_hasKeyboardFocus = enabled && _enabled && _visible && Visible;
		if (!_hasKeyboardFocus) {
			ClearFocus();
			return;
		}

		if (IsSelected && _keyboardFocusEnabled) {
			FocusFirstAvailable();
		}
	}

	public bool HitTest(Vector2 screenPoint) {
		return ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
			case UiPointerEventKind.Move:
				_headerHovered = HitTest(pointerEvent.Position);
				break;
			case UiPointerEventKind.Leave:
				_headerHovered = false;
				if (!_headerPressed) {
					SyncFocusedChildFromPointer();
				}
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left && _enabled && HitTest(pointerEvent.Position)) {
					_headerPressed = true;
					pointerEvent.RequestPointerCapture();
					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Up:
				if (pointerEvent.Button == MouseButton.Left && _headerPressed) {
					_headerPressed = false;
					_headerHovered = HitTest(pointerEvent.Position);
					if (_headerHovered && _enabled) {
						Activated?.Invoke(this);
					}

					pointerEvent.MarkHandled();
				}
				break;
			case UiPointerEventKind.Click:
				_headerHovered = HitTest(pointerEvent.Position);
				break;
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (!_visible || !_enabled || !Visible) {
			return;
		}

		if (IsSelected && Raylib.IsMouseButtonPressed(MouseButton.Left)) {
			SyncFocusedChildFromPointer();
		}

		if (!IsSelected || !_keyboardFocusEnabled || !_hasKeyboardFocus || ShouldReserveDirectionalInput()) {
			return;
		}

		var snapshot = Input.CaptureUiSnapshot();
		if (snapshot.NavigateLeft || snapshot.NavigateUp) {
			FocusPrevious();
		}

		if (snapshot.NavigateRight || snapshot.NavigateDown) {
			FocusNext();
		}
	}

	protected override void Draw() {
		if (!_visible || !Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		if (_art is not null) {
			DrawArt(bounds);
		} else {
			DrawPrimitive(bounds);
		}

		if (!string.IsNullOrWhiteSpace(Text)) {
			DrawText(bounds);
		}

		UiDebugBounds.DrawInput(bounds, Name);
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private void ClearFocus() {
		for (var i = 0; i < _focusables.Count; i++) {
			_focusables[i].SetKeyboardFocus(false);
		}
	}

	private void DrawArt(Rectangle bounds) {
		var baseIndex = ResolveStateFrameIndex();
		DrawFrame(baseIndex, bounds);
		if (IsSelected) {
			DrawFrame(Math.Min(baseIndex + 2, _art!.Frames.Count - 1), bounds);
		}
	}

	private void DrawPrimitive(Rectangle bounds) {
		var outline = IsSelected
			? SelectedTextColor
			: _headerHovered
				? HighlightTextColor
				: NormalTextColor;
		var fill = IsSelected
			? new Color(25, 36, 54, 255)
			: new Color(12, 18, 28, 255);
		Raylib.DrawRectangleRec(bounds, fill);
		Raylib.DrawRectangleLinesEx(bounds, 1f, outline);
	}

	private void DrawText(Rectangle bounds) {
		var tint = IsSelected
			? SelectedTextColor
			: _headerHovered
				? HighlightTextColor
				: NormalTextColor;
		const float horizontalPadding = 4f;
		var usableWidth = Math.Max(0f, bounds.Width - (horizontalPadding * 2f));
		if (usableWidth < 12f) {
			return;
		}

		var measured = UiText.MeasureWidth(Text, _fontSize, UiTextStyle.Body);
		var x = bounds.X + Math.Max(horizontalPadding, (bounds.Width - measured) * 0.5f);
		var y = bounds.Y + Math.Max(0f, (bounds.Height - _fontSize) * 0.5f) - 1f;

		Raylib.BeginScissorMode(
			(int)MathF.Floor(bounds.X + horizontalPadding),
			(int)MathF.Floor(bounds.Y),
			(int)MathF.Ceiling(usableWidth),
			(int)MathF.Ceiling(bounds.Height));
		try {
			UiText.Draw(Text, x, y, _fontSize, tint, UiTextStyle.Body);
		} finally {
			Raylib.EndScissorMode();
		}
	}

	private void DrawFrame(int frameIndex, Rectangle destination) {
		if (_art is null || frameIndex < 0 || frameIndex >= _art.Frames.Count) {
			return;
		}

		var slice = _art.Texture.GetSlice();
		var source = _art.Frames.GetFrameRegion(frameIndex);
		Raylib.DrawTexturePro(slice.Texture, source, destination, Vector2.Zero, 0f, Color.White);
	}

	private void FocusFirstAvailable() {
		if (_focusables.Count == 0) {
			_focusedIndex = -1;
			return;
		}

		var startIndex = _focusedIndex >= 0 ? _focusedIndex : 0;
		if (TryAssignFocusFrom(startIndex, 1)) {
			return;
		}

		TryAssignFocusFrom(_focusables.Count - 1, -1);
	}

	private void FocusNext() {
		if (_focusables.Count == 0) {
			return;
		}

		var start = _focusedIndex < 0 ? 0 : (_focusedIndex + 1) % _focusables.Count;
		TryAssignFocusFrom(start, 1);
	}

	private void FocusPrevious() {
		if (_focusables.Count == 0) {
			return;
		}

		var start = _focusedIndex < 0 ? _focusables.Count - 1 : (_focusedIndex - 1 + _focusables.Count) % _focusables.Count;
		TryAssignFocusFrom(start, -1);
	}

	private void FontSizeFromHotButton(string fontType) {
		_fontSize = fontType switch {
			"Font!!TabsBig" => 18f,
			"Font!!Tabs" => 15f,
			_ => DefaultFontSize
		};
	}

	private bool ShouldReserveDirectionalInput() {
		if (_focusedIndex < 0 || _focusedIndex >= _focusables.Count) {
			return false;
		}

		return _focusables[_focusedIndex] is LegacyListBoxNode or LegacySliderNode;
	}

	private int ResolveStateFrameIndex() {
		if (_art is null || _art.Frames.Count == 0) {
			return 0;
		}

		var baseIndex = Math.Clamp(_frameStartIndex, 0, _art.Frames.Count - 1);
		if (!_enabled) {
			return Math.Min(baseIndex + 3, _art.Frames.Count - 1);
		}

		if (_headerHovered || _headerPressed) {
			return Math.Min(baseIndex + 1, _art.Frames.Count - 1);
		}

		return baseIndex;
	}

	private void SyncFocusedChildFromPointer() {
		var pointer = Raylib.GetMousePosition();
		for (var i = 0; i < _focusables.Count; i++) {
			if (_focusables[i] is Control control && control.ContainsPoint(pointer)) {
				SetFocusedIndex(i);
				return;
			}
		}
	}

	private void SetFocusedIndex(int index) {
		if (index < 0 || index >= _focusables.Count) {
			return;
		}

		for (var i = 0; i < _focusables.Count; i++) {
			_focusables[i].SetKeyboardFocus(i == index);
		}

		_focusedIndex = index;
	}

	private bool TryAssignFocusFrom(int startIndex, int step) {
		if (_focusables.Count == 0) {
			return false;
		}

		for (var attempts = 0; attempts < _focusables.Count; attempts++) {
			var index = (startIndex + (attempts * step) + _focusables.Count) % _focusables.Count;
			if (!_focusables[index].SetKeyboardFocus(true)) {
				continue;
			}

			for (var i = 0; i < _focusables.Count; i++) {
				if (i != index) {
					_focusables[i].SetKeyboardFocus(false);
				}
			}

			_focusedIndex = index;
			return true;
		}

		return false;
	}

	private void DisposeArt() {
		_art?.Dispose();
		_art = null;
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
}
