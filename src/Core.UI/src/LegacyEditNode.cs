using System.Numerics;
using System.Text;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored single-line text input that follows the original Edit2 contract closely enough for menu and HUD ports.
/// </summary>
public sealed class LegacyEditNode : Control, IUiPointerEventHandler, ILegacyKeyboardFocusable {
	private const float DefaultFontSize = 14f;
	private const float DefaultPaddingX = 6f;
	private const float DefaultPaddingY = 2f;
	private const float DoubleClickSeconds = 0.35f;
	private static string s_scratchText = string.Empty;
	private AtlasFramesResource? _art;
	private bool _enabled = true;
	private bool _hasKeyboardFocus;
	private bool _mousePressed;
	private bool _toolbarBehavior;
	private bool _chatboxBehavior;
	private bool _lockedTextBehavior;
	private bool _transparent;
	private bool _disableInput;
	private bool _overwriteMode;
	private bool _visible = true;
	private float _caretBlinkElapsed;
	private float _fontSize = DefaultFontSize;
	private int _selectionAnchor = -1;
	private int _selectionCaret = -1;
	private int _firstDisplayChar;
	private int _maxChars = 256;
	private string _text = string.Empty;
	private string _ignoreChars = string.Empty;
	private double _lastClickTime;
	private Vector2 _lastClickPosition;

	public LegacyEditNode(string? name = null) : base(name) {
	}

	public event Action<LegacyEditNode>? Activated;
	public event Action<LegacyEditNode>? TextChanged;

	public uint ControlId { get; set; }

	public bool IsPointerInputEnabled => Visible && _visible && Size.X > 0f && Size.Y > 0f;

	public string Text => _text;

	public int CaretIndex { get; private set; }

	public bool OverwriteMode => _overwriteMode;

	public bool HasSelection => TryGetSelectionRange(out _, out _);

	public Color DisabledTextColor { get; private set; } = new(100, 100, 100, 255);
	public Color NormalTextColor { get; private set; } = new(0, 138, 191, 255);
	public Color HighlightTextColor { get; private set; } = new(40, 178, 231, 255);
	public Color SelectedTextColor { get; private set; } = new(61, 17, 123, 255);
	public Color CaretColor { get; private set; } = Color.White;
	public Color PrimitiveBackgroundFill { get; set; } = Color.Black;
	public Color PrimitiveOutline { get; set; } = new(100, 100, 100, 255);
	public Color PrimitiveFocusOutline { get; set; } = new(255, 100, 100, 255);
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;

	public void ApplyLegacyDefinition(GT_EDIT archetype, EDIT_DATA? data = null, VfxAnimationDataRepository? repository = null) {
		ArgumentNullException.ThrowIfNull(archetype);

		DisposeArt();
		if (!string.IsNullOrWhiteSpace(archetype.ShapeFile) && repository is not null) {
			_art = LoadArt(repository, archetype.ShapeFile);
		}

		DisabledTextColor = ToColor(archetype.DisabledText);
		NormalTextColor = ToColor(archetype.NormalText);
		HighlightTextColor = ToColor(archetype.HighlightText);
		SelectedTextColor = ToColor(archetype.SelectedText);
		CaretColor = ToColor(archetype.Caret);
		_fontSize = ResolveFontSize(archetype.FontName);

		var width = archetype.Width > 0 ? archetype.Width : (int)(_art?.Frames.GetFrameRegion(0).Width ?? 0);
		var height = archetype.Height > 0 ? archetype.Height : (int)(_art?.Frames.GetFrameRegion(0).Height ?? 0);

		if (data is not null) {
			Position = new Vector2(data.XOrigin, data.YOrigin);
		}

		Size = new Vector2(Math.Max(0, width), Math.Max(0, height));
		_firstDisplayChar = 0;
	}

	public void EnableEdit(bool enabled) {
		_enabled = enabled;
		if (!enabled) {
			_mousePressed = false;
			_hasKeyboardFocus = false;
			ClearSelection();
		}
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		if (!visible) {
			_mousePressed = false;
			_hasKeyboardFocus = false;
			ClearSelection();
		}
	}

	public bool SetKeyboardFocus(bool enabled) {
		if (enabled && (!_enabled || !_visible || !Visible || _disableInput)) {
			return false;
		}

		var changed = _hasKeyboardFocus != enabled;
		_hasKeyboardFocus = enabled;
		_mousePressed = false;
		if (!_hasKeyboardFocus) {
			ClearSelection();
			return true;
		}

		if (changed) {
			HighlightAll();
			_caretBlinkElapsed = 0f;
		}

		return true;
	}

	public bool RequestKeyboardFocus() {
		LegacyKeyboardFocusScope.Acquire(this);
		return SetKeyboardFocus(true);
	}

	public void SetText(string text, int firstHighlightChar = -1) {
		_text = SanitizeText(text);
		if (_text.Length > _maxChars) {
			_text = _text[.._maxChars];
		}

		CaretIndex = firstHighlightChar >= 0 && firstHighlightChar <= _text.Length
			? firstHighlightChar
			: _text.Length;

		if (_text.Length > 0 && firstHighlightChar >= 0 && firstHighlightChar < _text.Length) {
			_selectionAnchor = firstHighlightChar;
			_selectionCaret = _text.Length;
		} else {
			ClearSelection();
		}

		EnsureCaretVisible();
	}

	public int GetText(Span<char> buffer) {
		_text.AsSpan().CopyTo(buffer);
		return _text.Length;
	}

	public int GetTextLength() {
		return _text.Length;
	}

	public void SetMaxChars(int maxChars) {
		_maxChars = maxChars <= 0 ? int.MaxValue : maxChars;
		if (_text.Length > _maxChars) {
			_text = _text[.._maxChars];
			CaretIndex = Math.Min(CaretIndex, _text.Length);
			ClearSelection();
			EnsureCaretVisible();
		}
	}

	public void EnableToolbarBehavior() {
		_toolbarBehavior = true;
	}

	public void EnableChatboxBehavior() {
		_chatboxBehavior = true;
	}

	public void EnableLockedTextBehavior() {
		_lockedTextBehavior = true;
	}

	public void DisableInput(bool disableInput) {
		_disableInput = disableInput;
		if (_disableInput) {
			_mousePressed = false;
		}
	}

	public void SetTransparentBehavior(bool transparent) {
		_transparent = transparent;
	}

	public void SetIgnoreChars(string? ignoreChars) {
		_ignoreChars = ignoreChars ?? string.Empty;
	}

	public bool IsTextAllVisible() {
		return MeasureTextWidth(_text) <= GetVisibleTextWidth();
	}

	public bool HitTest(Vector2 screenPoint) {
		return ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		if (!_visible || !Visible || _disableInput) {
			return;
		}

		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Down when pointerEvent.Button == MouseButton.Left:
				HandleLeftPointerDown(pointerEvent);
				break;
			case UiPointerEventKind.Move:
				HandlePointerMove(pointerEvent);
				break;
			case UiPointerEventKind.Up when pointerEvent.Button == MouseButton.Left:
				_mousePressed = false;
				break;
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		_caretBlinkElapsed += deltaTime;
		if (_caretBlinkElapsed >= 1f) {
			_caretBlinkElapsed -= 1f;
		}

		if (!_enabled || !_visible || !Visible || !_hasKeyboardFocus || _disableInput) {
			return;
		}

		if (HandleSpecialKeys()) {
			return;
		}

		ConsumeTypedCharacters();
	}

	protected override void Draw() {
		if (!_visible || !Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		DrawSurface(bounds);
		DrawSelection(bounds);
		DrawText(bounds);
		DrawCaret(bounds);
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private void HandleLeftPointerDown(UiPointerEvent pointerEvent) {
		if (!_enabled) {
			return;
		}

		RequestKeyboardFocus();
		_mousePressed = true;
		pointerEvent.RequestPointerCapture();
		pointerEvent.MarkHandled();

		var now = Raylib.GetTime();
		var caret = ResolveCaretIndex(pointerEvent.Position.X);
		if (now - _lastClickTime <= DoubleClickSeconds &&
		    Vector2.DistanceSquared(pointerEvent.Position, _lastClickPosition) <= 36f) {
			SelectWordAt(caret);
		} else {
			CaretIndex = caret;
			_selectionAnchor = caret;
			_selectionCaret = caret;
			if (!Raylib.IsKeyDown(KeyboardKey.LeftShift) && !Raylib.IsKeyDown(KeyboardKey.RightShift)) {
				ClearSelection();
			}
		}

		_lastClickTime = now;
		_lastClickPosition = pointerEvent.Position;
		_caretBlinkElapsed = 0f;
		EnsureCaretVisible();
	}

	private void HandlePointerMove(UiPointerEvent pointerEvent) {
		if (!_mousePressed || !_hasKeyboardFocus) {
			return;
		}

		var caret = ResolveCaretIndex(pointerEvent.Position.X);
		if (_selectionAnchor < 0) {
			_selectionAnchor = CaretIndex;
		}

		CaretIndex = caret;
		_selectionCaret = caret;
		EnsureCaretVisible();
	}

	private bool HandleSpecialKeys() {
		if (Raylib.IsKeyPressed(KeyboardKey.Enter)) {
			Activated?.Invoke(this);
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Escape) && _toolbarBehavior) {
			ClearSelection();
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Escape) && _chatboxBehavior) {
			ClearSelection();
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.A) && IsCtrlDown()) {
			HighlightAll();
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Insert)) {
			if (IsShiftDown()) {
				PasteScratch();
			} else if (IsCtrlDown()) {
				CopySelectionToScratch();
			} else {
				_overwriteMode = !_overwriteMode;
			}
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Delete)) {
			if (IsShiftDown()) {
				CopySelectionToScratch();
			}
			DeleteForward();
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Backspace)) {
			DeleteBackward();
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Home)) {
			MoveCaret(0, extendSelection: IsShiftDown());
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.End)) {
			MoveCaret(_text.Length, extendSelection: IsShiftDown());
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Left)) {
			MoveCaret(Math.Max(0, CaretIndex - 1), extendSelection: IsShiftDown());
			return true;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Right)) {
			MoveCaret(Math.Min(_text.Length, CaretIndex + 1), extendSelection: IsShiftDown());
			return true;
		}

		return false;
	}

	private void ConsumeTypedCharacters() {
		var changed = false;

		for (var codepoint = Raylib.GetCharPressed(); codepoint > 0; codepoint = Raylib.GetCharPressed()) {
			if (!Rune.IsValid(codepoint) || char.IsControl((char)codepoint)) {
				continue;
			}

			var incoming = char.ConvertFromUtf32(codepoint);
			if (string.IsNullOrEmpty(incoming) || ContainsIgnorableChar(incoming)) {
				continue;
			}

			changed |= InsertText(incoming);
		}

		if (changed) {
			TextChanged?.Invoke(this);
		}
	}

	private bool InsertText(string incoming) {
		var currentLength = _text.Length - GetSelectionLength();
		if (!_overwriteMode && currentLength + incoming.Length > _maxChars) {
			return false;
		}

		if (!DeleteSelectionInternal(notify: false) && _overwriteMode && CaretIndex < _text.Length) {
			var overwriteLength = Math.Min(incoming.Length, _text.Length - CaretIndex);
			_text = _text.Remove(CaretIndex, overwriteLength);
		}

		if (_overwriteMode && _text.Length + incoming.Length > _maxChars) {
			return false;
		}

		_text = _text.Insert(CaretIndex, incoming);
		CaretIndex += incoming.Length;
		ClearSelection();

		if (_lockedTextBehavior && !IsTextAllVisible()) {
			_text = _text.Remove(CaretIndex - incoming.Length, incoming.Length);
			CaretIndex -= incoming.Length;
			return false;
		}

		EnsureCaretVisible();
		_caretBlinkElapsed = 0f;
		return true;
	}

	private void DeleteBackward() {
		if (DeleteSelectionInternal()) {
			return;
		}

		if (CaretIndex <= 0 || _text.Length == 0) {
			return;
		}

		_text = _text.Remove(CaretIndex - 1, 1);
		CaretIndex--;
		EnsureCaretVisible();
		_caretBlinkElapsed = 0f;
		TextChanged?.Invoke(this);
	}

	private void DeleteForward() {
		if (DeleteSelectionInternal()) {
			return;
		}

		if (CaretIndex < 0 || CaretIndex >= _text.Length) {
			return;
		}

		_text = _text.Remove(CaretIndex, 1);
		EnsureCaretVisible();
		_caretBlinkElapsed = 0f;
		TextChanged?.Invoke(this);
	}

	private bool DeleteSelectionInternal(bool notify = true) {
		if (!TryGetSelectionRange(out var start, out var end)) {
			return false;
		}

		_text = _text.Remove(start, end - start);
		CaretIndex = start;
		ClearSelection();
		EnsureCaretVisible();
		_caretBlinkElapsed = 0f;
		if (notify) {
			TextChanged?.Invoke(this);
		}
		return true;
	}

	private void CopySelectionToScratch() {
		if (!TryGetSelectionRange(out var start, out var end)) {
			return;
		}

		s_scratchText = _text[start..end];
	}

	private void PasteScratch() {
		if (string.IsNullOrEmpty(s_scratchText)) {
			return;
		}

		var changed = InsertText(s_scratchText);
		if (changed) {
			TextChanged?.Invoke(this);
		}
	}

	private void MoveCaret(int newCaret, bool extendSelection) {
		newCaret = Math.Clamp(newCaret, 0, _text.Length);
		if (extendSelection) {
			if (_selectionAnchor < 0) {
				_selectionAnchor = CaretIndex;
			}

			CaretIndex = newCaret;
			_selectionCaret = newCaret;
		} else {
			CaretIndex = newCaret;
			ClearSelection();
		}

		EnsureCaretVisible();
		_caretBlinkElapsed = 0f;
	}

	private void HighlightAll() {
		if (_text.Length == 0) {
			CaretIndex = 0;
			ClearSelection();
			return;
		}

		_selectionAnchor = 0;
		_selectionCaret = _text.Length;
		CaretIndex = _text.Length;
		EnsureCaretVisible();
	}

	private void SelectWordAt(int caret) {
		if (string.IsNullOrEmpty(_text)) {
			CaretIndex = 0;
			ClearSelection();
			return;
		}

		caret = Math.Clamp(caret, 0, _text.Length - 1);
		if (char.IsWhiteSpace(_text[caret])) {
			CaretIndex = caret;
			ClearSelection();
			return;
		}

		var start = caret;
		while (start > 0 && !char.IsWhiteSpace(_text[start - 1])) {
			start--;
		}

		var end = caret + 1;
		while (end < _text.Length && !char.IsWhiteSpace(_text[end])) {
			end++;
		}

		_selectionAnchor = start;
		_selectionCaret = end;
		CaretIndex = end;
		EnsureCaretVisible();
	}

	private void EnsureCaretVisible() {
		if (CaretIndex <= _firstDisplayChar) {
			_firstDisplayChar = CaretIndex;
			return;
		}

		var visibleWidth = GetVisibleTextWidth();
		while (MeasureTextWidth(GetVisiblePrefix(CaretIndex)) > visibleWidth && _firstDisplayChar < CaretIndex) {
			_firstDisplayChar++;
		}
	}

	private Rectangle GetTextBounds(Rectangle bounds) {
		return new Rectangle(
			bounds.X + DefaultPaddingX,
			bounds.Y + Math.Max(DefaultPaddingY, (bounds.Height - _fontSize) * 0.5f - 1f),
			Math.Max(0f, bounds.Width - (DefaultPaddingX * 2f)),
			Math.Max(0f, _fontSize + 2f));
	}

	private float GetVisibleTextWidth() {
		return Math.Max(1f, Size.X - (DefaultPaddingX * 2f));
	}

	private string GetVisiblePrefix(int endExclusive) {
		if (endExclusive <= _firstDisplayChar) {
			return string.Empty;
		}

		return _text[_firstDisplayChar..Math.Min(endExclusive, _text.Length)];
	}

	private string GetVisibleText() {
		if (_firstDisplayChar <= 0) {
			return _text;
		}

		return _text[_firstDisplayChar..];
	}

	private void DrawSurface(Rectangle bounds) {
		if (_art is not null) {
			var slice = _art.Texture.GetSlice();
			var source = _art.Frames.GetFrameRegion(0);
			Raylib.DrawTexturePro(slice.Texture, source, bounds, Vector2.Zero, 0f, Color.White);
		} else if (!_transparent) {
			Raylib.DrawRectangleRec(bounds, PrimitiveBackgroundFill);
			Raylib.DrawRectangleLinesEx(bounds, 1f, PrimitiveOutline);
		}

		if (_hasKeyboardFocus && _art is null && !_transparent) {
			Raylib.DrawRectangleLinesEx(bounds, 1f, PrimitiveFocusOutline);
		}
	}

	private void DrawSelection(Rectangle bounds) {
		if (!_hasKeyboardFocus) {
			return;
		}

		var textBounds = GetTextBounds(bounds);
		var selectionColor = SelectedTextColor;
		if (TryGetSelectionRange(out var start, out var end)) {
			var startX = textBounds.X + MeasureTextWidth(GetVisiblePrefix(start));
			var endX = textBounds.X + MeasureTextWidth(GetVisiblePrefix(end));
			var rect = new Rectangle(startX, textBounds.Y, Math.Max(1f, endX - startX), _fontSize + 2f);
			Raylib.DrawRectangleRec(rect, selectionColor);
			return;
		}

		if (_text.Length == 0) {
			var width = Math.Min(GetVisibleTextWidth(), UiText.MeasureWidth("   ", _fontSize, TextStyle));
			Raylib.DrawRectangleRec(new Rectangle(textBounds.X, textBounds.Y, width, _fontSize + 2f), selectionColor);
		}
	}

	private void DrawText(Rectangle bounds) {
		var textBounds = GetTextBounds(bounds);
		Raylib.BeginScissorMode(
			(int)MathF.Floor(textBounds.X),
			(int)MathF.Floor(textBounds.Y),
			(int)MathF.Ceiling(textBounds.Width),
			(int)MathF.Ceiling(Math.Max(textBounds.Height, bounds.Height)));

		try {
			UiText.Draw(
				GetVisibleText(),
				textBounds.X,
				textBounds.Y,
				_fontSize,
				_enabled ? NormalTextColor : DisabledTextColor,
				TextStyle);
		} finally {
			Raylib.EndScissorMode();
		}
	}

	private void DrawCaret(Rectangle bounds) {
		if (!_hasKeyboardFocus || _caretBlinkElapsed >= 0.5f) {
			return;
		}

		var textBounds = GetTextBounds(bounds);
		var caretX = textBounds.X + MeasureTextWidth(GetVisiblePrefix(CaretIndex));
		Raylib.DrawLineEx(
			new Vector2(caretX, textBounds.Y),
			new Vector2(caretX, textBounds.Y + _fontSize + 1f),
			1f,
			CaretColor);
	}

	private int ResolveCaretIndex(float screenX) {
		var textBounds = GetTextBounds(GlobalBounds);
		var localX = screenX - textBounds.X;
		if (localX <= 0f) {
			return _firstDisplayChar;
		}

		var visible = GetVisibleText();
		var index = _firstDisplayChar;
		for (var i = 0; i <= visible.Length; i++) {
			var width = UiText.MeasureWidth(visible[..i], _fontSize, TextStyle);
			if (localX < width) {
				return index + Math.Max(0, i - 1);
			}
			index = _firstDisplayChar + i;
		}

		return _text.Length;
	}

	private float MeasureTextWidth(string text) {
		return string.IsNullOrEmpty(text) ? 0f : UiText.MeasureWidth(text, _fontSize, TextStyle);
	}

	private int GetSelectionLength() {
		return TryGetSelectionRange(out var start, out var end) ? end - start : 0;
	}

	private bool TryGetSelectionRange(out int start, out int end) {
		start = end = -1;
		if (_selectionAnchor < 0 || _selectionCaret < 0 || _selectionAnchor == _selectionCaret) {
			return false;
		}

		start = Math.Clamp(Math.Min(_selectionAnchor, _selectionCaret), 0, _text.Length);
		end = Math.Clamp(Math.Max(_selectionAnchor, _selectionCaret), 0, _text.Length);
		return end > start;
	}

	private void ClearSelection() {
		_selectionAnchor = -1;
		_selectionCaret = -1;
	}

	private void DisposeArt() {
		_art?.Dispose();
		_art = null;
	}

	private bool IsIgnorable(char letter) {
		if (_ignoreChars.Length > 0) {
			if (_ignoreChars.IndexOf(letter) >= 0) {
				return true;
			}

			if (CaretIndex == 0 && letter == ' ') {
				return true;
			}
		}

		return false;
	}

	private bool ContainsIgnorableChar(string text) {
		for (var i = 0; i < text.Length; i++) {
			if (IsIgnorable(text[i])) {
				return true;
			}
		}

		return false;
	}

	private static string SanitizeText(string? text) {
		return string.IsNullOrEmpty(text) ? string.Empty : text;
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

	private static bool IsCtrlDown() {
		return Raylib.IsKeyDown(KeyboardKey.LeftControl) || Raylib.IsKeyDown(KeyboardKey.RightControl);
	}

	private static bool IsShiftDown() {
		return Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift);
	}

	private static float ResolveFontSize(string fontName) {
		return fontName switch {
			"Font!!Button" => 16f,
			"Font!!Button3D" => 16f,
			"Font!!DropCustom" => 14f,
			"Font!!DefaultListBox" => 14f,
			"Font!!Chat3D" => 14f,
			"Font!!FutureMessage" => 16f,
			_ => DefaultFontSize
		};
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color(color.Red, color.Green, color.Blue, byte.MaxValue);
	}
}
