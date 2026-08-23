using System.Numerics;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored selectable text list that mirrors the Trim-era listbox contract.
/// </summary>
/// <remarks>
/// The control keeps the legacy imperative item API, authored text-area geometry,
/// caret-versus-activation event split, and scroll-window semantics. Visuals can
/// be driven from one optional exported shape atlas plus primitive fallback.
/// </remarks>
public sealed class LegacyListBoxNode : Control, IUiPointerEventHandler {
	private const float DefaultFontSize = 14f;
	private const float DefaultHorizontalPadding = 6f;
	private const float DefaultVerticalPadding = 1f;
	private const float ScrollBarHorizontalScale = 0.55f;
	private readonly List<ItemEntry> _items = [];
	private AtlasFramesResource? _art;
	private bool _enabled = true;
	private bool _hasKeyboardFocus;
	private int _hoveredVisibleRow = -1;
	private bool _isHovered;
	private bool _isStatic;
	private int _pageLines;
	private LegacyScrollBarNode? _scrollBar;
	private bool _scrollBarRequested;
	private RECT _textArea = new();
	private int _topLine;
	private bool _visible = true;

	public LegacyListBoxNode(string? name = null) : base(name) {
	}

	/// <summary>
	/// Raised whenever the caret/selection index changes.
	/// </summary>
	public event Action<LegacyListBoxNode>? CaretMoved;

	/// <summary>
	/// Raised when the listbox commits its selected item.
	/// </summary>
	public event Action<LegacyListBoxNode>? SelectionCommitted;

	public uint ControlId { get; set; }

	public string ScrollBarTypeId { get; private set; } = string.Empty;

	public int SelectedIndex { get; private set; } = -1;

	public string SelectedLabel => TryGetItem(SelectedIndex, out var item) ? item.Label : string.Empty;

	public int TopLine => _topLine;

	public int TextLines { get; private set; }

	public int LeadingHeight { get; private set; }

	public bool SingleClick { get; private set; }

	public bool SolidBackground { get; private set; }

	public bool NoBorder { get; private set; }

	public bool DisableMouseSelect { get; private set; }

	public float FontSize { get; set; } = DefaultFontSize;

	public Color DisabledTextColor { get; private set; } = new(100, 100, 100, 255);
	public Color NormalTextColor { get; private set; } = new(0, 138, 191, 255);
	public Color HighlightTextColor { get; private set; } = new(40, 178, 231, 255);
	public Color SelectedTextColor { get; private set; } = new(61, 17, 123, 255);
	public Color SelectedTextGrayedColor { get; private set; } = new(80, 80, 80, 255);
	public Color PrimitiveBackgroundFill { get; set; } = Color.Black;
	public Color PrimitiveOutline { get; set; } = new(140, 140, 160, 255);
	public float ArtScrollBarOffsetX { get; set; } = 0f;
	public float PrimitiveScrollBarOffsetX { get; set; } = 0f;
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;

	private bool IsPrimitive => _art is null;
	private float ScrollBarOffset => IsPrimitive ? PrimitiveScrollBarOffsetX : ArtScrollBarOffsetX;

	public IReadOnlyList<string> Items => _items.Select(static item => item.Label).ToArray();

	public bool IsPointerInputEnabled => Visible && _visible && _enabled && Size.X > 0f && Size.Y > 0f;

	public void ApplyLegacyDefinition(GT_LISTBOX archetype, LISTBOX_DATA data, VfxAnimationDataRepository? repository = null,
		UtfDbRepository? utfDbRepository = null) {
		ArgumentNullException.ThrowIfNull(archetype);
		ArgumentNullException.ThrowIfNull(data);

		DisposeArt();

		DisabledTextColor = ToColor(archetype.DisabledText);
		NormalTextColor = ToColor(archetype.NormalText);
		HighlightTextColor = ToColor(archetype.HighlightText);
		SelectedTextColor = ToColor(archetype.SelectedText);
		SelectedTextGrayedColor = ToColor(archetype.SelectedTextGrayed);
		ScrollBarTypeId = archetype.ScrollBarType;
		_textArea = data.TextArea;
		LeadingHeight = Math.Max(0, (int)data.LeadingHeight);
		_isStatic = HasFlag(data.Flags, ListboxFlags.Static);
		SingleClick = HasFlag(data.Flags, ListboxFlags.SingleClick);
		SolidBackground = HasFlag(data.Flags, ListboxFlags.SolidBackground);
		NoBorder = HasFlag(data.Flags, ListboxFlags.NoBorder);
		DisableMouseSelect = HasFlag(data.Flags, ListboxFlags.DisableMouseSelect);
		_scrollBarRequested = HasFlag(data.Flags, ListboxFlags.Scrollbar);

		if (!string.IsNullOrWhiteSpace(archetype.ShapeFile) && repository is not null) {
			LoadArt(repository, archetype.ShapeFile);
		}

		Position = new Vector2(data.XOrigin, data.YOrigin);
		ConfigureScrollBar(utfDbRepository, repository);
		var width = ResolveConfiguredWidth();
		var height = ResolveConfiguredHeight();
		Size = new Vector2(width, height);
		RecalculateTextMetrics();
		ConfigureScrollBarLayout();
		EnsureVisible(SelectedIndex >= 0 ? SelectedIndex : 0);
		UpdateScrollBarState();
	}

	public void EnableListbox(bool enabled) {
		_enabled = enabled;
		if (!_enabled) {
			_hasKeyboardFocus = false;
			_hoveredVisibleRow = -1;
			_isHovered = false;
		}

		_scrollBar?.EnableScrollBar(enabled);
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		if (!visible) {
			_hoveredVisibleRow = -1;
			_isHovered = false;
		}

		_scrollBar?.SetVisible(visible);
	}

	public bool SetKeyboardFocus(bool enabled) {
		if (_isStatic || !_enabled) {
			_hasKeyboardFocus = false;
			return false;
		}

		_hasKeyboardFocus = enabled && _visible && Visible;
		return true;
	}

	public int AddStringToHead(string label) {
		_items.Insert(0, CreateItem(label));
		if (SelectedIndex >= 0) {
			SelectedIndex++;
		}

		EnsureVisible(_topLine);
		UpdateScrollBarState();
		return 0;
	}

	public int AddString(string label) {
		_items.Add(CreateItem(label));
		UpdateScrollBarState();
		return _items.Count - 1;
	}

	public int FindString(string prefix) {
		if (string.IsNullOrEmpty(prefix)) {
			return -1;
		}

		for (var index = 0; index < _items.Count; index++) {
			if (_items[index].Label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
				return index;
			}
		}

		return -1;
	}

	public int FindStringExact(string label) {
		if (string.IsNullOrEmpty(label)) {
			return -1;
		}

		for (var index = 0; index < _items.Count; index++) {
			if (string.Equals(_items[index].Label, label, StringComparison.OrdinalIgnoreCase)) {
				return index;
			}
		}

		return -1;
	}

	public void RemoveString(int index) {
		if (!TryGetItem(index, out _)) {
			return;
		}

		_items.RemoveAt(index);
		if (SelectedIndex == index) {
			SelectedIndex = Math.Min(index, _items.Count - 1);
			NotifyCaretMoved();
		} else if (SelectedIndex > index) {
			SelectedIndex--;
		}

		if (_topLine > Math.Max(0, _items.Count - TextLines)) {
			_topLine = Math.Max(0, _items.Count - TextLines);
		}

		UpdateScrollBarState();
	}

	public int GetString(int index, Span<char> buffer) {
		if (!TryGetItem(index, out var item)) {
			return 0;
		}

		item.Label.AsSpan().CopyTo(buffer);
		return item.Label.Length;
	}

	public string GetString(int index) {
		return TryGetItem(index, out var item) ? item.Label : string.Empty;
	}

	public int SetString(int index, string label) {
		if (!TryGetItem(index, out var item)) {
			return -1;
		}

		_items[index] = item with { Label = SanitizeLabel(label) };
		return index;
	}

	public void SetDataValue(int index, uint dataValue) {
		if (!TryGetItem(index, out var item)) {
			return;
		}

		_items[index] = item with { DataValue = dataValue };
	}

	public uint GetDataValue(int index) {
		return TryGetItem(index, out var item) ? item.DataValue : 0u;
	}

	public void SetColorValue(int index, Color color) {
		if (!TryGetItem(index, out var item)) {
			return;
		}

		_items[index] = item with { TextColorOverride = color };
	}

	public Color GetColorValue(int index) {
		return TryGetItem(index, out var item) && item.TextColorOverride.HasValue
			? item.TextColorOverride.Value
			: Color.Blank;
	}

	public int GetCurrentSelection() {
		return SelectedIndex;
	}

	public int SetCurrentSelection(int newIndex) {
		var old = SelectedIndex;
		if (_items.Count == 0) {
			SelectedIndex = -1;
			UpdateScrollBarState();
			return old;
		}

		if (newIndex == -1) {
			SelectedIndex = -1;
			NotifyCaretMoved();
			EnsureVisible(0);
			return old;
		}

		if ((uint)newIndex >= _items.Count) {
			return old;
		}

		SelectedIndex = newIndex;
		NotifyCaretMoved();
		EnsureVisible(SelectedIndex);
		UpdateScrollBarState();
		return old;
	}

	public int GetCaretPosition() {
		return GetCurrentSelection();
	}

	public int SetCaretPosition(int newIndex) {
		return SetCurrentSelection(newIndex);
	}

	public void ResetContent() {
		_items.Clear();
		SelectedIndex = -1;
		_topLine = 0;
		_hoveredVisibleRow = -1;
		NotifyCaretMoved();
		UpdateScrollBarState();
	}

	public int GetNumberOfItems() {
		return _items.Count;
	}

	public int GetTopVisibleString() {
		return _items.Count == 0 ? -1 : _topLine;
	}

	public int GetBottomVisibleString() {
		if (_items.Count == 0 || TextLines <= 0) {
			return -1;
		}

		return Math.Min(_items.Count - 1, _topLine + TextLines - 1);
	}

	public void EnsureVisible(int index) {
		if (index < 0 || TextLines <= 0 || _items.Count <= TextLines) {
			return;
		}

		index = Math.Clamp(index, 0, _items.Count - 1);
		if (index <= _topLine) {
			_topLine = index;
			UpdateScrollBarState();
			return;
		}

		var bottomVisible = _topLine + TextLines - 1;
		if (index > bottomVisible) {
			var limit = Math.Max(0, _items.Count - TextLines);
			_topLine = Math.Min(limit, Math.Max(0, index - TextLines + 1));
			UpdateScrollBarState();
		}
	}

	public void ScrollPageUp() {
		_topLine = Math.Max(0, _topLine - _pageLines);
		UpdateScrollBarState();
	}

	public void ScrollPageDown() {
		var limit = Math.Max(0, _items.Count - TextLines + 1);
		_topLine = Math.Min(limit, _topLine + _pageLines);
		UpdateScrollBarState();
	}

	public void ScrollLineUp() {
		_topLine = Math.Max(0, _topLine - 1);
		UpdateScrollBarState();
	}

	public void ScrollLineDown() {
		var limit = Math.Max(0, _items.Count - TextLines + 1);
		_topLine = Math.Min(limit, _topLine + 1);
		UpdateScrollBarState();
	}

	public void ScrollHome() {
		_topLine = 0;
		UpdateScrollBarState();
	}

	public void ScrollEnd() {
		_topLine = Math.Max(0, _items.Count - TextLines + 1);
		UpdateScrollBarState();
	}

	public void CaretPageUp() {
		var next = SelectedIndex - _pageLines;
		if (SelectedIndex < 0) {
			next = 0;
		}

		SetCurrentSelection(Math.Max(0, next));
	}

	public void CaretPageDown() {
		var next = SelectedIndex < 0 ? _pageLines : SelectedIndex + _pageLines;
		SetCurrentSelection(Math.Min(_items.Count - 1, next));
	}

	public void CaretLineUp() {
		var next = SelectedIndex <= 0 ? 0 : SelectedIndex - 1;
		SetCurrentSelection(Math.Min(Math.Max(0, next), _items.Count - 1));
	}

	public void CaretLineDown() {
		var next = SelectedIndex < 0 ? 1 : SelectedIndex + 1;
		SetCurrentSelection(Math.Min(_items.Count - 1, next));
	}

	public void CaretHome() {
		SetCurrentSelection(_items.Count == 0 ? -1 : 0);
	}

	public void CaretEnd() {
		SetCurrentSelection(_items.Count == 0 ? -1 : _items.Count - 1);
	}

	public int GetBreakIndex(string text) {
		if (string.IsNullOrEmpty(text)) {
			return -1;
		}

		var width = Math.Max(1f, _textArea.Right - _textArea.Left - 10);
		if (UiText.MeasureWidth(text, FontSize, TextStyle) < width) {
			return -1;
		}

		var trail = 0;
		var index = 0;
		var lastPosition = 0;
		while (lastPosition < text.Length) {
			var spaceIndex = text.IndexOf(' ', lastPosition);
			if (spaceIndex < 0) {
				break;
			}

			index = spaceIndex + 1;
			var substring = text[..index];
			if (UiText.MeasureWidth(substring, FontSize, TextStyle) < width) {
				trail = index;
				lastPosition = index;
				continue;
			}

			break;
		}

		if (trail > 0) {
			return trail;
		}

		for (var i = 0; i < text.Length; i++) {
			if (UiText.MeasureWidth(text[..(i + 1)], FontSize, TextStyle) >= width) {
				return i - 1;
			}
		}

		return -1;
	}

	public bool HitTest(Vector2 screenPoint) {
		return !_visible || !Visible ? false : ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
			case UiPointerEventKind.Move:
				UpdateHover(pointerEvent.Position);
				if (!_enabled || _isStatic || DisableMouseSelect || !SingleClick) {
					break;
				}

				var hoveredIndex = GetItemIndexAt(pointerEvent.Position);
				if (hoveredIndex >= 0 && hoveredIndex != SelectedIndex) {
					SelectedIndex = hoveredIndex;
					NotifyCaretMoved();
				}
				break;
			case UiPointerEventKind.Leave:
				_isHovered = false;
				_hoveredVisibleRow = -1;
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left) {
					HandleLeftPointerDown(pointerEvent);
				}
				break;
			case UiPointerEventKind.Up:
			case UiPointerEventKind.Click:
				if (pointerEvent.Button == MouseButton.Left) {
					HandleLeftPointerUp(pointerEvent);
				}

				UpdateHover(pointerEvent.Position);
				break;
			case UiPointerEventKind.Wheel:
				if (_enabled && _hasKeyboardFocus) {
					if (pointerEvent.WheelDelta >= 0f) {
						ScrollLineUp();
					} else {
						ScrollLineDown();
					}

					pointerEvent.MarkHandled();
				}
				break;
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (!_enabled || !_hasKeyboardFocus || _isStatic || _items.Count == 0) {
			return;
		}

		var snapshot = Input.CaptureUiSnapshot();
		if (snapshot.AcceptPressed) {
			CommitSelection();
			return;
		}

		if (snapshot.NavigateUp) {
			CaretLineUp();
			return;
		}

		if (snapshot.NavigateDown) {
			CaretLineDown();
			return;
		}

		if (snapshot.NavigateLeft) {
			CaretPageUp();
			return;
		}

		if (snapshot.NavigateRight) {
			CaretPageDown();
			return;
		}
	}

	protected override void Draw() {
		if (!_visible || !Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		DrawBackground(bounds);

		var textBounds = GetGlobalTextBounds(bounds);
		if (textBounds.Width <= 0f || textBounds.Height <= 0f || TextLines <= 0) {
			return;
		}

		Raylib.BeginScissorMode(
			(int)MathF.Floor(textBounds.X),
			(int)MathF.Floor(textBounds.Y),
			(int)MathF.Ceiling(textBounds.Width),
			(int)MathF.Ceiling(textBounds.Height));

		try {
			var bottomVisible = GetBottomVisibleString();
			if (bottomVisible < 0) {
				return;
			}

			for (var index = _topLine; index <= bottomVisible; index++) {
				var rowBounds = GetVisibleRowBounds(bounds, index - _topLine);
				var selected = index == SelectedIndex;
				var hovered = index == GetHoveredIndex();

				if (selected) {
					DrawSelectionOutline(rowBounds);
				}

				var item = _items[index];
				var textColor = ResolveItemTextColor(item, hovered);
				UiText.Draw(
					item.Label,
					rowBounds.X + 2f,
					rowBounds.Y + DefaultVerticalPadding,
					FontSize,
					textColor,
					TextStyle);
			}
		} finally {
			Raylib.EndScissorMode();
		}
	}

	protected override void OnDispose() {
		DisposeArt();
		base.OnDispose();
	}

	private void CommitSelection() {
		if (SelectedIndex >= 0 && SelectedIndex < _items.Count) {
			SelectionCommitted?.Invoke(this);
		}
	}

	private static ItemEntry CreateItem(string label) {
		return new ItemEntry(SanitizeLabel(label));
	}

	private void DisposeArt() {
		_art?.Dispose();
		_art = null;
	}

	private void ConfigureScrollBar(UtfDbRepository? utfDbRepository, VfxAnimationDataRepository? repository) {
		if (!_scrollBarRequested || string.IsNullOrWhiteSpace(ScrollBarTypeId) || utfDbRepository is null) {
			if (_scrollBar is not null) {
				_scrollBar.SetVisible(false);
			}

			return;
		}

		var scrollBarArchetype = ReadTypedEntry<GT_SCROLLBAR>(utfDbRepository, "GT_SCROLLBAR", ScrollBarTypeId);
		var upButtonArchetype = ReadTypedEntry<GT_BUTTON>(utfDbRepository, "GT_BUTTON", scrollBarArchetype.UpButtonType);
		var downButtonArchetype = ReadTypedEntry<GT_BUTTON>(utfDbRepository, "GT_BUTTON", scrollBarArchetype.DownButtonType);

		if (_scrollBar is null) {
			_scrollBar = AddChild(new LegacyScrollBarNode($"{Name}ScrollBar"));
			_scrollBar.ScrollPositionChanged += (_, position) => {
				_topLine = position;
				UpdateScrollBarState();
			};
			_scrollBar.LineUpRequested += _ => ScrollLineUp();
			_scrollBar.LineDownRequested += _ => ScrollLineDown();
			_scrollBar.PageUpRequested += _ => ScrollPageUp();
			_scrollBar.PageDownRequested += _ => ScrollPageDown();
		}

		_scrollBar.ApplyLegacyDefinition(scrollBarArchetype, upButtonArchetype, downButtonArchetype, repository);
		ConfigureScrollBarLayout();
		_scrollBar.SetVisible(_visible && Visible);
		_scrollBar.EnableScrollBar(_enabled);
	}

	private void DrawBackground(Rectangle bounds) {
		if (_art is not null) {
			var slice = _art.Texture.GetSlice();
			var source = _art.Frames.GetFrameRegion(0);
			Raylib.DrawTexturePro(slice.Texture, source, bounds, Vector2.Zero, 0f, Color.White);
			return;
		}

		if (SolidBackground && !NoBorder) {
			Raylib.DrawRectangleRec(bounds, PrimitiveBackgroundFill);
			Raylib.DrawRectangleLinesEx(bounds, 1f, PrimitiveOutline);
			return;
		}

		if (!NoBorder) {
			Raylib.DrawRectangleLinesEx(bounds, 1f, PrimitiveOutline);
		}
	}

	private void DrawSelectionOutline(Rectangle rowBounds) {
		var color = _enabled && _hasKeyboardFocus
			? SelectedTextColor
			: SelectedTextGrayedColor;
		Raylib.DrawRectangleLinesEx(rowBounds, 1f, color);
	}

	private Rectangle GetGlobalTextBounds(Rectangle bounds) {
		var right = _textArea.Right > 0 ? _textArea.Right + 1f : bounds.Width;
		var bottom = _textArea.Bottom > 0 ? _textArea.Bottom : (int)bounds.Height;
		return new Rectangle(
			bounds.X + _textArea.Left,
			bounds.Y + _textArea.Top,
			Math.Max(0f, right - _textArea.Left),
			Math.Max(0f, bottom - _textArea.Top + 1));
	}

	private float GetScrollBarOffsetWidth() {
		if (_scrollBar is null) {
			return 0f;
		}

		return _scrollBar.ButtonWidth * ScrollBarHorizontalScale;
	}

	private float GetScrollBarRightPadding() {
		return Math.Max(0f, ScrollBarOffset);
	}

	private int GetHoveredIndex() {
		if (_hoveredVisibleRow < 0) {
			return -1;
		}

		var index = _topLine + _hoveredVisibleRow;
		return index >= 0 && index < _items.Count ? index : -1;
	}

	private int GetItemIndexAt(Vector2 position) {
		var bounds = GlobalBounds;
		var textBounds = GetGlobalTextBounds(bounds);
		if (!Raylib.CheckCollisionPointRec(position, textBounds)) {
			return -1;
		}

		var actualLines = Math.Max(0, GetBottomVisibleString() - _topLine + 1);
		for (var row = 0; row < actualLines; row++) {
			if (Raylib.CheckCollisionPointRec(position, GetVisibleRowBounds(bounds, row))) {
				return _topLine + row;
			}
		}

		return -1;
	}

	private Rectangle GetVisibleRowBounds(Rectangle bounds, int visibleRow) {
		var textBounds = GetGlobalTextBounds(bounds);
		var lineHeight = GetLineHeight();
		return new Rectangle(
			textBounds.X,
			textBounds.Y + (visibleRow * lineHeight),
			textBounds.Width,
			FontSize);
	}

	private float GetLineHeight() {
		return FontSize + LeadingHeight;
	}

	private void HandleLeftPointerDown(UiPointerEvent pointerEvent) {
		if (!_enabled || _isStatic) {
			return;
		}

		var index = GetItemIndexAt(pointerEvent.Position);
		if (index < 0) {
			return;
		}

		_hasKeyboardFocus = true;
		if (SelectedIndex != index) {
			SelectedIndex = index;
			NotifyCaretMoved();
		}

		UpdateHover(pointerEvent.Position);
		pointerEvent.MarkHandled();
	}

	private void HandleLeftPointerUp(UiPointerEvent pointerEvent) {
		if (!_enabled || _isStatic || !SingleClick || !_hasKeyboardFocus) {
			return;
		}

		var index = GetItemIndexAt(pointerEvent.Position);
		if (index < 0 || index != SelectedIndex) {
			return;
		}

		CommitSelection();
		pointerEvent.MarkHandled();
	}

	private void LoadArt(VfxAnimationDataRepository repository, string shapeId) {
		if (!repository.TryGetAtlasByShapeId(shapeId, out var atlasEntry)) {
			return;
		}

		_art = new AtlasFramesResource(
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: false),
			repository.GetInterfaceAssetPath(atlasEntry.Value, metaJson: true),
			TextureFilter.Point);
		_ = _art.Frames.Count;
	}

	private void NotifyCaretMoved() {
		CaretMoved?.Invoke(this);
	}

	private void RecalculateTextMetrics() {
		var textBounds = GetGlobalTextBounds(new Rectangle(0f, 0f, Size.X, Size.Y));
		var lineHeight = Math.Max(1f, GetLineHeight());
		var totalHeight = textBounds.Height;
		TextLines = (int)(totalHeight / lineHeight);
		var remainder = totalHeight % lineHeight;
		if (remainder >= FontSize) {
			TextLines++;
		}

		TextLines = Math.Max(0, TextLines);
		_pageLines = TextLines < 4 ? TextLines : TextLines - 2;
	}

	private float ResolveConfiguredHeight() {
		return GetBaseConfiguredHeight();
	}

	private float ResolveConfiguredWidth() {
		return GetBaseConfiguredWidth() + GetScrollBarLaneWidth();
	}

	private float GetBaseConfiguredHeight() {
		return _art is not null
			? _art.Frames.GetFrameRegion(0).Height
			: Math.Max(0f, (_textArea.Bottom > 0 ? _textArea.Bottom : _textArea.Top) + 1f);
	}

	private float GetBaseConfiguredWidth() {
		return _art is not null
			? _art.Frames.GetFrameRegion(0).Width
			: Math.Max(0f, (_textArea.Right > 0 ? _textArea.Right : _textArea.Left) + 1f);
	}

	private Color ResolveItemTextColor(ItemEntry item, bool hovered) {
		if (!_enabled) {
			return DisabledTextColor;
		}

		if (item.TextColorOverride.HasValue) {
			return item.TextColorOverride.Value;
		}

		if (hovered) {
			return HighlightTextColor;
		}

		return NormalTextColor;
	}

	private static string SanitizeLabel(string label) {
		return string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
	}

	private static bool HasFlag(uint value, ListboxFlags flag) {
		return (value & (uint)flag) != 0u;
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color(color.Red, color.Green, color.Blue, (byte)255);
	}

	private bool TryGetItem(int index, out ItemEntry item) {
		if (index >= 0 && index < _items.Count) {
			item = _items[index];
			return true;
		}

		item = default;
		return false;
	}

	private void UpdateHover(Vector2 position) {
		_isHovered = ContainsPoint(position);
		_hoveredVisibleRow = -1;
		if (!_isHovered) {
			return;
		}

		var hoveredIndex = GetItemIndexAt(position);
		if (hoveredIndex >= _topLine) {
			_hoveredVisibleRow = hoveredIndex - _topLine;
		}
	}

	private void UpdateScrollBarState() {
		if (_scrollBar is null) {
			return;
		}

		var limit = Math.Max(0, _items.Count - TextLines);
		_topLine = Math.Clamp(_topLine, 0, limit);
		_scrollBar.SetScrollRange(_items.Count);
		_scrollBar.SetViewRange(TextLines);
		_scrollBar.SetScrollPosition(_topLine);
		ConfigureScrollBarLayout();
		_scrollBar.SetVisible(_visible && Visible);
	}

	private void ConfigureScrollBarLayout() {
		if (_scrollBar is null) {
			return;
		}

		_scrollBar.Scale = new Vector2(ScrollBarHorizontalScale, 1f);
		_scrollBar.Size = new Vector2(_scrollBar.ButtonWidth, Size.Y);
		var basePosition = new Vector2(GetScrollBarLaneStartX(), 0f);
		_scrollBar.Position = basePosition + new Vector2(ScrollBarOffset, 0f);
	}

	private float GetScrollBarLaneStartX() {
		return Math.Max(0f, GetBaseConfiguredWidth());
	}

	private float GetScrollBarLaneWidth() {
		if (!_scrollBarRequested) {
			return 0f;
		}

		return GetScrollBarOffsetWidth() + GetScrollBarRightPadding();
	}

	private static T ReadTypedEntry<T>(UtfDbRepository repository, string typeName, string fileName) where T : class {
		var details = repository.ReadEntryDetails("GenData.db", typeName, fileName);
		return details.TypedValue as T
			?? throw new InvalidOperationException($"Entry '{typeName}/{fileName}' did not deserialize to {typeof(T).Name}.");
	}

	[Flags]
	private enum ListboxFlags : uint {
		Static = 1 << 0,
		SingleClick = 1 << 1,
		Scrollbar = 1 << 2,
		SolidBackground = 1 << 3,
		NoBorder = 1 << 4,
		DisableMouseSelect = 1 << 5
	}

	private readonly record struct ItemEntry(string Label, uint DataValue = 0u, Color? TextColorOverride = null);
}
