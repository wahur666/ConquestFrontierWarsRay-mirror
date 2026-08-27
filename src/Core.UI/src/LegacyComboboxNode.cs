using System.Numerics;
using System.Text;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored editable combobox built from the original edit, button, and listbox records.
/// </summary>
public sealed class LegacyComboboxNode : Control, IUiPointerEventHandler, ILegacyKeyboardFocusable {
	private const float DefaultFontSize = 14f;
	private const float DefaultPaddingX = 6f;
	private const float DefaultPaddingY = 3f;
	private const float CaretBlinkPeriodSeconds = 0.5f;
	private readonly LegacyButtonNode _button;
	private readonly LegacyListBoxNode _listBox;
	private AtlasFramesResource? _editArt;
	private bool _enabled = true;
	private bool _hasKeyboardFocus;
	private int _highlightStart = -1;
	private bool _syncingSelectionFromText;
	private RECT _editArea = new();
	private float _fontSize = DefaultFontSize;
	private bool _visible = true;
	private int _maxChars = int.MaxValue;
	private float _caretBlinkElapsed;
	private string _text = string.Empty;

	public LegacyComboboxNode(string? name = null) : base(name) {
		_button = AddChild(new LegacyButtonNode($"{Name ?? "LegacyCombobox"}Button"));
		_button.Activated += _ => ToggleExpanded();
		_listBox = AddChild(new LegacyListBoxNode($"{Name ?? "LegacyCombobox"}ListBox"));
		_listBox.CaretMoved += HandleListBoxCaretMoved;
		_listBox.SelectionCommitted += HandleListBoxSelectionCommitted;
		_listBox.SetVisible(false);
	}

	public event Action<LegacyComboboxNode>? SelectionCommitted;
	public event Action<LegacyComboboxNode>? TextChanged;

	public uint ControlId { get; set; }

	public bool IsExpanded { get; private set; }

	public bool IsPointerInputEnabled => Visible && _visible && Size.X > 0f && Size.Y > 0f;

	public string Text => _text;

	public int SelectedIndex => _listBox.GetCurrentSelection();

	public string SelectedLabel => _listBox.SelectedLabel;

	public int CaretIndex { get; private set; }

	public IReadOnlyList<string> Items => _listBox.Items;

	public void ApplyLegacyDefinition(
		GT_COMBOBOX comboboxArchetype,
		GT_EDIT editArchetype,
		GT_BUTTON buttonArchetype,
		GT_LISTBOX listboxArchetype,
		COMBOBOX_DATA data,
		VfxAnimationDataRepository? repository = null,
		XmlDbRepository? xmlDbRepository = null) {
		ArgumentNullException.ThrowIfNull(comboboxArchetype);
		ArgumentNullException.ThrowIfNull(editArchetype);
		ArgumentNullException.ThrowIfNull(buttonArchetype);
		ArgumentNullException.ThrowIfNull(listboxArchetype);
		ArgumentNullException.ThrowIfNull(data);

		DisposeEditArt();

		_editArea = ResolveEditArea(data, editArchetype);
		Position = new Vector2(data.ScreenRect.Left, data.ScreenRect.Top);
		Size = new Vector2(
			Math.Max(0f, data.ScreenRect.Right - data.ScreenRect.Left),
			Math.Max(0f, _editArea.Bottom - _editArea.Top));

		if (!string.IsNullOrWhiteSpace(editArchetype.ShapeFile) && repository is not null) {
			_editArt = LoadArt(repository, editArchetype.ShapeFile);
		}

		_fontSize = ResolveFontSize(editArchetype.FontName);
		DisabledTextColor = ToColor(editArchetype.DisabledText);
		NormalTextColor = ToColor(editArchetype.NormalText);
		HighlightTextColor = ToColor(editArchetype.HighlightText);
		SelectedTextColor = ToColor(editArchetype.SelectedText);
		CaretColor = ToColor(editArchetype.Caret);

		_button.ApplyLegacyDefinition(buttonArchetype, repository: repository);
		_button.Position = new Vector2(data.ButtonData.XOrigin, data.ButtonData.YOrigin);
		_button.ControlId = 1;
		_button.SetVisible(_visible);
		_button.EnableButton(_enabled);
		Size = new Vector2(
			Math.Max(Size.X, _button.Position.X + _button.Size.X),
			Math.Max(Size.Y, _button.Position.Y + _button.Size.Y));

		_listBox.ApplyLegacyDefinition(listboxArchetype, data.ListboxData, repository, xmlDbRepository);
		_listBox.Position = new Vector2(data.ListboxData.XOrigin, data.ListboxData.YOrigin);
		_listBox.ControlId = 2;
		_listBox.CommitOnSingleClickPointerDown = true;
		_listBox.EnableContentMeasuredHeight(Math.Max(4, _listBox.TextLines));
		_listBox.SetVisible(false);
		_listBox.EnableListbox(_enabled);

		_text = string.Empty;
		CaretIndex = 0;
		_highlightStart = -1;
		_caretBlinkElapsed = 0f;
	}

	public Color DisabledTextColor { get; private set; } = new(100, 100, 100, 255);
	public Color NormalTextColor { get; private set; } = new(0, 138, 191, 255);
	public Color HighlightTextColor { get; private set; } = new(40, 178, 231, 255);
	public Color SelectedTextColor { get; private set; } = new(61, 17, 123, 255);
	public Color CaretColor { get; private set; } = Color.White;
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;
	public Color PrimitiveBackgroundFill { get; set; } = Color.Black;
	public Color PrimitiveOutline { get; set; } = new(140, 140, 160, 255);

	public void EnableCombobox(bool enabled) {
		_enabled = enabled;
		_button.EnableButton(enabled);
		_listBox.EnableListbox(enabled);
		if (!enabled) {
			_hasKeyboardFocus = false;
			SetExpanded(false);
		}
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		_button.SetVisible(visible);
		if (!visible) {
			SetExpanded(false);
		} else {
			_listBox.SetVisible(IsExpanded);
		}
	}

	public bool SetKeyboardFocus(bool enabled) {
		_hasKeyboardFocus = enabled && _enabled && _visible && Visible;
		if (!_hasKeyboardFocus) {
			_highlightStart = -1;
			SetExpanded(false);
		}

		UpdateChildFocus();
		return _hasKeyboardFocus;
	}

	public bool RequestKeyboardFocus() {
		LegacyKeyboardFocusScope.Acquire(this);
		return SetKeyboardFocus(true);
	}

	public void SetText(string text, int firstHighlightChar = -1) {
		_text = SanitizeText(text);
		CaretIndex = _text.Length;
		_highlightStart = firstHighlightChar >= 0 && firstHighlightChar < _text.Length
			? firstHighlightChar
			: -1;
		SyncSelectionFromText();
		TextChanged?.Invoke(this);
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
			SetText(_text[.._maxChars]);
		}
	}

	public int AddStringToHead(string label) {
		return _listBox.AddStringToHead(label);
	}

	public int AddString(string label) {
		return _listBox.AddString(label);
	}

	public int FindString(string prefix) {
		return _listBox.FindString(prefix);
	}

	public int FindStringExact(string label) {
		return _listBox.FindStringExact(label);
	}

	public void RemoveString(int index) {
		_listBox.RemoveString(index);
		if (_listBox.GetNumberOfItems() == 0) {
			SetExpanded(false);
		}
	}

	public string GetString(int index) {
		return _listBox.GetString(index);
	}

	public int SetString(int index, string label) {
		return _listBox.SetString(index, label);
	}

	public void SetDataValue(int index, uint dataValue) {
		_listBox.SetDataValue(index, dataValue);
	}

	public uint GetDataValue(int index) {
		return _listBox.GetDataValue(index);
	}

	public void SetColorValue(int index, Color color) {
		_listBox.SetColorValue(index, color);
	}

	public Color GetColorValue(int index) {
		return _listBox.GetColorValue(index);
	}

	public int GetCurrentSelection() {
		return _listBox.GetCurrentSelection();
	}

	public int SetCurrentSelection(int newIndex) {
		var old = _listBox.SetCurrentSelection(newIndex);
		if (newIndex >= 0) {
			var label = _listBox.GetString(newIndex);
			_text = label;
			CaretIndex = _text.Length;
			_highlightStart = -1;
			TextChanged?.Invoke(this);
		}

		return old;
	}

	public int GetCaretPosition() {
		return _listBox.GetCaretPosition();
	}

	public int SetCaretPosition(int newIndex) {
		return _listBox.SetCaretPosition(newIndex);
	}

	public void ResetContent() {
		_listBox.ResetContent();
		SetExpanded(false);
	}

	public int GetNumberOfItems() {
		return _listBox.GetNumberOfItems();
	}

	public int GetTopVisibleString() {
		return _listBox.GetTopVisibleString();
	}

	public int GetBottomVisibleString() {
		return _listBox.GetBottomVisibleString();
	}

	public void EnsureVisible(int index) {
		_listBox.EnsureVisible(index);
	}

	public void ScrollPageUp() {
		_listBox.ScrollPageUp();
	}

	public void ScrollPageDown() {
		_listBox.ScrollPageDown();
	}

	public void ScrollLineUp() {
		_listBox.ScrollLineUp();
	}

	public void ScrollLineDown() {
		_listBox.ScrollLineDown();
	}

	public void ScrollHome() {
		_listBox.ScrollHome();
	}

	public void ScrollEnd() {
		_listBox.ScrollEnd();
	}

	public void CaretPageUp() {
		_listBox.CaretPageUp();
	}

	public void CaretPageDown() {
		_listBox.CaretPageDown();
	}

	public void CaretLineUp() {
		_listBox.CaretLineUp();
	}

	public void CaretLineDown() {
		_listBox.CaretLineDown();
	}

	public void CaretHome() {
		_listBox.CaretHome();
	}

	public void CaretEnd() {
		_listBox.CaretEnd();
	}

	public bool HitTest(Vector2 screenPoint) {
		if (IsExpanded) {
			return _listBox.HitTest(screenPoint) || ContainsPoint(screenPoint);
		}

		return ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		if (!_visible || !Visible) {
			return;
		}

		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Down when pointerEvent.Button == MouseButton.Left:
				HandleLeftPointerDown(pointerEvent);
				break;
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		_caretBlinkElapsed += deltaTime;
		if (_caretBlinkElapsed >= CaretBlinkPeriodSeconds * 2f) {
			_caretBlinkElapsed -= CaretBlinkPeriodSeconds * 2f;
		}

		if (!_enabled || !_visible || !Visible) {
			return;
		}

		var snapshot = Input.CaptureUiSnapshot();
		if (IsExpanded) {
			if (snapshot.BackPressed || snapshot.EscapePressed) {
				SetExpanded(false);
				return;
			}

			if (snapshot.LeftPressed) {
				var pointer = snapshot.PointerPosition;
				if (!ContainsPoint(pointer) && !_listBox.HitTest(pointer)) {
					SetExpanded(false);
					return;
				}
			}
		}

		if (!_hasKeyboardFocus) {
			return;
		}

		if (IsExpanded) {
			if (Raylib.IsKeyPressed(KeyboardKey.Delete)) {
				DeleteExpandedSelection();
				return;
			}

			if (snapshot.AcceptPressed && _listBox.GetCurrentSelection() >= 0) {
				CommitSelectionFromList();
			}

			return;
		}

		if (snapshot.NavigateDown && _listBox.GetNumberOfItems() > 0) {
			SetExpanded(true);
			return;
		}

		if (snapshot.AcceptPressed && _listBox.GetNumberOfItems() > 0) {
			SetExpanded(true);
			return;
		}

		var previousLength = _text.Length;
		var textChanged = ConsumeTypingInput();
		if (textChanged) {
			ApplyPrefixAutocomplete(previousLength);
			SyncSelectionFromText();
			TextChanged?.Invoke(this);
		}
	}

	protected override void Draw() {
		if (!_visible || !Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		DrawEditSurface();
		DrawEditText();
	}

	protected override void OnDispose() {
		DisposeEditArt();
		base.OnDispose();
	}

	private void HandleLeftPointerDown(UiPointerEvent pointerEvent) {
		if (!_enabled) {
			return;
		}

		var localPointer = pointerEvent.Position;
		if (_listBox.HitTest(localPointer)) {
			RequestKeyboardFocus();
			UpdateChildFocus();
			return;
		}

		if (ContainsPoint(localPointer)) {
			RequestKeyboardFocus();
			_highlightStart = -1;
			CaretIndex = ResolveCaretIndex(localPointer);
			UpdateChildFocus();
			pointerEvent.MarkHandled();
		}
	}

	private void HandleListBoxCaretMoved(LegacyListBoxNode listBox) {
		if (_syncingSelectionFromText) {
			return;
		}

		var selection = listBox.GetCurrentSelection();
		if (selection < 0) {
			return;
		}

		_text = listBox.GetString(selection);
		CaretIndex = _text.Length;
		_highlightStart = -1;
		TextChanged?.Invoke(this);
	}

	private void HandleListBoxSelectionCommitted(LegacyListBoxNode listBox) {
		if (listBox.GetCurrentSelection() < 0) {
			SetExpanded(false);
			return;
		}

		CommitSelectionFromList();
	}

	private void CommitSelectionFromList() {
		var selection = _listBox.GetCurrentSelection();
		if (selection < 0) {
			SetExpanded(false);
			return;
		}

		_text = _listBox.GetString(selection);
		CaretIndex = _text.Length;
		_highlightStart = -1;
		SetExpanded(false);
		TextChanged?.Invoke(this);
		SelectionCommitted?.Invoke(this);
	}

	private void SetExpanded(bool expanded) {
		IsExpanded = expanded && _enabled && _visible && Visible && _listBox.GetNumberOfItems() > 0;
		_button.PushState = IsExpanded;
		_listBox.SetVisible(IsExpanded);
		if (IsExpanded) {
			var focusIndex = _listBox.GetCurrentSelection();
			if (focusIndex < 0 && _listBox.GetNumberOfItems() > 0) {
				focusIndex = 0;
				_listBox.SetCurrentSelection(focusIndex);
			}

			if (focusIndex >= 0) {
				_listBox.EnsureVisible(focusIndex);
			}
		}

		UpdateChildFocus();
	}

	private void ToggleExpanded() {
		if (!_enabled) {
			return;
		}

		RequestKeyboardFocus();
		SetExpanded(!IsExpanded);
	}

	private void UpdateChildFocus() {
		_button.SetKeyboardFocus(_hasKeyboardFocus && !IsExpanded);
		_listBox.SetKeyboardFocus(_hasKeyboardFocus && IsExpanded);
	}

	private bool ConsumeTypingInput() {
		CaretIndex = Math.Clamp(CaretIndex, 0, _text.Length);
		var changed = false;

		if (Raylib.IsKeyPressed(KeyboardKey.Backspace)) {
			if (HasHighlightedSuffix()) {
				if (_highlightStart > 0) {
					_text = _text.Remove(_highlightStart - 1, 1);
					CaretIndex = _highlightStart - 1;
				} else {
					_text = string.Empty;
					CaretIndex = 0;
				}
				_highlightStart = -1;
				changed = true;
			} else if (_text.Length > 0 && CaretIndex > 0) {
				_text = _text.Remove(CaretIndex - 1, 1);
				CaretIndex--;
				changed = true;
			}
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Delete)) {
			if (HasHighlightedSuffix()) {
				_text = _text[.._highlightStart];
				CaretIndex = _text.Length;
				_highlightStart = -1;
				changed = true;
			} else if (_text.Length > 0 && CaretIndex < _text.Length) {
				_text = _text.Remove(CaretIndex, 1);
				changed = true;
			}
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Home)) {
			CaretIndex = 0;
			_highlightStart = -1;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.End)) {
			CaretIndex = _text.Length;
			_highlightStart = -1;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Left)) {
			CaretIndex = Math.Max(0, CaretIndex - 1);
			_highlightStart = -1;
		}

		if (Raylib.IsKeyPressed(KeyboardKey.Right)) {
			CaretIndex = Math.Min(_text.Length, CaretIndex + 1);
			_highlightStart = -1;
		}

		for (var codepoint = Raylib.GetCharPressed(); codepoint > 0; codepoint = Raylib.GetCharPressed()) {
			if (!Rune.IsValid(codepoint) || char.IsControl((char)codepoint)) {
				continue;
			}

			var incoming = char.ConvertFromUtf32(codepoint);
			if (_text.Length - HighlightedSuffixLength() + incoming.Length > _maxChars) {
				continue;
			}

			if (HasHighlightedSuffix()) {
				_text = _text[.._highlightStart];
				CaretIndex = _text.Length;
				_highlightStart = -1;
			}

			if (CaretIndex < 0 || CaretIndex > _text.Length) {
				CaretIndex = _text.Length;
			}

			_text = _text.Insert(CaretIndex, incoming);
			CaretIndex += incoming.Length;
			changed = true;
		}

		return changed;
	}

	private void DeleteExpandedSelection() {
		var selection = _listBox.GetCurrentSelection();
		if (selection < 0) {
			return;
		}

		_listBox.RemoveString(selection);
		if (_listBox.GetNumberOfItems() == 0) {
			_text = string.Empty;
			CaretIndex = 0;
			_highlightStart = -1;
			SetExpanded(false);
			TextChanged?.Invoke(this);
			return;
		}

		var nextSelection = Math.Clamp(selection, 0, _listBox.GetNumberOfItems() - 1);
		_listBox.SetCurrentSelection(nextSelection);
		_text = _listBox.GetString(nextSelection);
		CaretIndex = _text.Length;
		_highlightStart = -1;
		_listBox.EnsureVisible(nextSelection);
		TextChanged?.Invoke(this);
	}

	private void ApplyPrefixAutocomplete(int previousLength) {
		if (_text.Length <= previousLength || string.IsNullOrWhiteSpace(_text)) {
			_highlightStart = -1;
			return;
		}

		var index = _listBox.FindString(_text);
		if (index < 0) {
			_highlightStart = -1;
			return;
		}

		var full = _listBox.GetString(index);
		if (string.IsNullOrEmpty(full) || full.Length <= _text.Length) {
			_highlightStart = -1;
			return;
		}

		var typedLength = _text.Length;
		_text = full;
		CaretIndex = _text.Length;
		_highlightStart = typedLength;
	}

	private void SyncSelectionFromText() {
		var exact = _listBox.FindStringExact(_text);
		if (exact >= 0) {
			SetCurrentSelectionFromText(exact);
			return;
		}

		var prefix = _listBox.FindString(_text);
		if (prefix >= 0) {
			SetCurrentSelectionFromText(prefix);
		}
	}

	private void SetCurrentSelectionFromText(int index) {
		_syncingSelectionFromText = true;
		try {
			_listBox.SetCurrentSelection(index);
		} finally {
			_syncingSelectionFromText = false;
		}
	}

	private void DrawEditSurface() {
		var bounds = GetEditGlobalBounds();
		if (_editArt is not null) {
			var slice = _editArt.Texture.GetSlice();
			var source = _editArt.Frames.GetFrameRegion(0);
			Raylib.DrawTexturePro(slice.Texture, source, bounds, Vector2.Zero, 0f, Color.White);
			return;
		}

		Raylib.DrawRectangleRec(bounds, PrimitiveBackgroundFill);
		Raylib.DrawRectangleLinesEx(bounds, 1f, PrimitiveOutline);
	}

	private void DrawEditText() {
		var bounds = GetEditGlobalBounds();
		var textX = bounds.X + DefaultPaddingX;
		var textY = bounds.Y + Math.Max(DefaultPaddingY, (bounds.Height - _fontSize) * 0.5f - 1f);
		var displayText = _text;

		if (HasHighlightedSuffix()) {
			var prefix = _text[.._highlightStart];
			var highlighted = _text[_highlightStart..];
			var prefixWidth = UiText.MeasureWidth(prefix, _fontSize, TextStyle);
			var highlightWidth = UiText.MeasureWidth(highlighted, _fontSize, TextStyle);
			var highlightRect = new Rectangle(
				textX + prefixWidth - 1f,
				textY + 1f,
				highlightWidth + 2f,
				_fontSize + 2f);
			Raylib.DrawRectangleRec(highlightRect, new Color(44, 74, 112, 220));
		}

		UiText.Draw(
			displayText,
			textX,
			textY,
			_fontSize,
			_enabled ? NormalTextColor : DisabledTextColor,
			TextStyle);

		if (!_hasKeyboardFocus || IsExpanded || (_caretBlinkElapsed >= CaretBlinkPeriodSeconds)) {
			return;
		}

		var caretText = HasHighlightedSuffix() ? _text[.._highlightStart] : _text;
		if (CaretIndex >= 0 && CaretIndex <= caretText.Length) {
			caretText = caretText[..CaretIndex];
		}

		var caretX = textX + UiText.MeasureWidth(caretText, _fontSize, TextStyle);
		Raylib.DrawLineEx(
			new Vector2(caretX, textY),
			new Vector2(caretX, textY + _fontSize + 1f),
			1f,
			CaretColor);
	}

	private Rectangle GetEditGlobalBounds() {
		var position = GlobalPosition;
		var scale = GlobalScale;
		return new Rectangle(
			position.X + (_editArea.Left * scale.X),
			position.Y + (_editArea.Top * scale.Y),
			Math.Abs((_editArea.Right - _editArea.Left) * scale.X),
			Math.Abs((_editArea.Bottom - _editArea.Top) * scale.Y));
	}

	private int ResolveCaretIndex(Vector2 pointerPosition) {
		var bounds = GetEditGlobalBounds();
		var visibleText = HasHighlightedSuffix() ? _text[.._highlightStart] : _text;
		var relativeX = pointerPosition.X - (bounds.X + DefaultPaddingX);
		if (relativeX <= 0f || string.IsNullOrEmpty(visibleText)) {
			return 0;
		}

		var nearestIndex = visibleText.Length;
		var nearestDistance = float.MaxValue;
		for (var index = 0; index <= visibleText.Length; index++) {
			var width = UiText.MeasureWidth(visibleText[..index], _fontSize, TextStyle);
			var distance = MathF.Abs(relativeX - width);
			if (distance < nearestDistance) {
				nearestDistance = distance;
				nearestIndex = index;
			}
		}

		return nearestIndex;
	}

	private bool HasHighlightedSuffix() {
		return _highlightStart >= 0 && _highlightStart < _text.Length;
	}

	private int HighlightedSuffixLength() {
		return HasHighlightedSuffix() ? _text.Length - _highlightStart : 0;
	}

	private void DisposeEditArt() {
		_editArt?.Dispose();
		_editArt = null;
	}

	private static AtlasFramesResource? LoadArt(VfxAnimationDataRepository repository, string shapeFile) {
		if (!repository.TryGetAtlasByShapeId(shapeFile, out var atlasEntry)) {
			return null;
		}

		return new AtlasFramesResource(
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: false),
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: true),
			TextureFilter.Point);
	}

	private static RECT ResolveEditArea(COMBOBOX_DATA data, GT_EDIT editArchetype) {
		var buttonStartX = data.ButtonData.XOrigin > 0
			? data.ButtonData.XOrigin
			: Math.Max(0, data.ScreenRect.Right - data.ScreenRect.Left - 20);
		var width = editArchetype.Width > 0
			? editArchetype.Width
			: Math.Max(0, buttonStartX - data.EditData.XOrigin);
		var collapsedHeight = data.ListboxData.YOrigin > data.EditData.YOrigin
			? data.ListboxData.YOrigin - data.EditData.YOrigin
			: 0;
		var height = editArchetype.Height > 0
			? editArchetype.Height
			: collapsedHeight > 0
				? collapsedHeight
				: Math.Max(0, data.ScreenRect.Bottom - data.ScreenRect.Top - data.EditData.YOrigin);

		return new RECT {
			Left = data.EditData.XOrigin,
			Top = data.EditData.YOrigin,
			Right = data.EditData.XOrigin + width,
			Bottom = data.EditData.YOrigin + height
		};
	}

	private static string SanitizeText(string text) {
		return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
	}

	private static float ResolveFontSize(string fontName) {
		return fontName switch {
			"Font!!DropCustom" => 14f,
			"Font!!Button" => 16f,
			_ => DefaultFontSize
		};
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color(color.Red, color.Green, color.Blue, byte.MaxValue);
	}
}
