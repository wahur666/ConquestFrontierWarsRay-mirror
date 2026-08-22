using System.Numerics;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored dropdown that keeps Trim-era placement and imperative item APIs.
/// </summary>
/// <remarks>
/// Configure it from one legacy button archetype, one legacy listbox archetype,
/// and one authored <see cref="DROPDOWN_DATA"/> placement record. Item content
/// still comes from the owning screen logic through methods like
/// <see cref="AddString(string)"/> and <see cref="SetCurrentSelection(int)"/>.
/// </remarks>
public sealed class LegacyDropdownNode : Control, IUiPointerEventHandler {
	private const float DefaultFontSize = 16f;
	private const float DefaultItemHeight = 18f;
	private const float DefaultTextInset = 8f;
	private const float DefaultArrowInset = 16f;
	private readonly List<ItemEntry> _items = [];
	private bool _enabled = true;
	private bool _hasKeyboardFocus;
	private bool _hoveredButton;
	private int _hoveredItemIndex = -1;
	private float _buttonTextInset = DefaultTextInset;
	private float _fontSize = DefaultFontSize;
	private bool _visible = true;
	private RECT _listTextArea = new();
	private Vector2 _listOffset = Vector2.Zero;

	public LegacyDropdownNode(string? name = null) : base(name) {
	}

	/// <summary>
	/// Raised when a user commits a dropdown selection, mirroring legacy CQE_LIST_SELECTION behavior.
	/// </summary>
	public event Action<LegacyDropdownNode>? SelectionCommitted;

	public uint ControlId { get; set; }

	public bool IsExpanded { get; private set; }

	public int SelectedIndex { get; private set; } = -1;

	public string SelectedLabel => TryGetItem(SelectedIndex, out var item) ? item.Label : string.Empty;

	public bool IsPointerInputEnabled => Visible && _visible && Size.X > 0f && Size.Y > 0f;

	public IReadOnlyList<string> Items => _items.Select(static item => item.Label).ToArray();

	public Color DisabledTextColor { get; private set; } = new(100, 100, 100, 255);
	public Color NormalTextColor { get; private set; } = new(180, 160, 120, 255);
	public Color HighlightTextColor { get; private set; } = new(200, 180, 140, 255);
	public Color SelectedItemTextColor { get; private set; } = new(61, 17, 123, 255);
	public Color SelectedItemGrayedTextColor { get; private set; } = new(80, 80, 80, 255);
	public Color ButtonFill { get; set; } = new(42, 48, 58, 255);
	public Color ButtonHoverFill { get; set; } = new(52, 58, 70, 255);
	public Color ExpandedFill { get; set; } = new(24, 30, 42, 255);
	public Color Outline { get; set; } = new(132, 146, 168, 220);
	public Color SelectedItemFill { get; set; } = new(60, 72, 112, 255);
	public Color HoverItemFill { get; set; } = new(38, 54, 78, 255);
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;
	public string Placeholder { get; set; } = string.Empty;
	public float ItemHeight { get; set; } = DefaultItemHeight;
	public float FontSize {
		get => _fontSize;
		set => _fontSize = Math.Max(8f, value);
	}

	public void ApplyLegacyDefinition(GT_BUTTON buttonArchetype, GT_LISTBOX listboxArchetype, DROPDOWN_DATA data) {
		ArgumentNullException.ThrowIfNull(buttonArchetype);
		ArgumentNullException.ThrowIfNull(listboxArchetype);
		ArgumentNullException.ThrowIfNull(data);

		DisabledTextColor = ToColor(buttonArchetype.DisabledText);
		NormalTextColor = ToColor(buttonArchetype.NormalText);
		HighlightTextColor = ToColor(buttonArchetype.HighlightText);
		SelectedItemTextColor = ToColor(listboxArchetype.SelectedText);
		SelectedItemGrayedTextColor = ToColor(listboxArchetype.SelectedTextGrayed);
		_buttonTextInset = Math.Max(DefaultTextInset, buttonArchetype.LeftMargin > 0 ? buttonArchetype.LeftMargin : DefaultTextInset);
		_listOffset = new Vector2(data.ListboxData.XOrigin, data.ListboxData.YOrigin);
		_listTextArea = data.ListboxData.TextArea;
		ItemHeight = data.ListboxData.LeadingHeight > 0
			? Math.Max(12f, data.ListboxData.LeadingHeight)
			: DefaultItemHeight;

		var width = Math.Max(0f, data.ScreenRect.Right - data.ScreenRect.Left);
		var height = Math.Max(0f, data.ScreenRect.Bottom - data.ScreenRect.Top);
		Size = new Vector2(width, height);
		Position = new Vector2(data.ScreenRect.Left, data.ScreenRect.Top);
	}

	public void EnableDropdown(bool enabled) {
		_enabled = enabled;
		if (!_enabled) {
			SetExpanded(false);
			_hasKeyboardFocus = false;
			_hoveredButton = false;
			_hoveredItemIndex = -1;
		}
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		if (!visible) {
			SetExpanded(false);
			_hoveredButton = false;
			_hoveredItemIndex = -1;
		}
	}

	public void SetKeyboardFocus(bool enabled) {
		_hasKeyboardFocus = enabled && _enabled && _visible && Visible;
		if (!_hasKeyboardFocus) {
			SetExpanded(false);
		}
	}

	public void SetSelectionColor(Color color) {
		NormalTextColor = color;
	}

	public int AddStringToHead(string label) {
		label = SanitizeLabel(label);
		_items.Insert(0, new ItemEntry(label));
		if (SelectedIndex >= 0) {
			SelectedIndex++;
		}

		return 0;
	}

	public int AddString(string label) {
		label = SanitizeLabel(label);
		_items.Add(new ItemEntry(label));
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
			SelectedIndex = -1;
		} else if (SelectedIndex > index) {
			SelectedIndex--;
		}

		if (_hoveredItemIndex == index) {
			_hoveredItemIndex = -1;
		} else if (_hoveredItemIndex > index) {
			_hoveredItemIndex--;
		}
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
		if (_items.Count == 0) {
			SelectedIndex = -1;
			return -1;
		}

		SelectedIndex = Math.Clamp(newIndex, 0, _items.Count - 1);
		return SelectedIndex;
	}

	public void ResetContent() {
		_items.Clear();
		SelectedIndex = -1;
		_hoveredItemIndex = -1;
		SetExpanded(false);
	}

	public int GetNumberOfItems() {
		return _items.Count;
	}

	public bool HitTest(Vector2 screenPoint) {
		if (IsExpanded) {
			return true;
		}

		return ContainsPoint(screenPoint);
	}

	public void OnPointerEvent(UiPointerEvent pointerEvent) {
		if (!_visible || !Visible) {
			return;
		}

		switch (pointerEvent.Kind) {
			case UiPointerEventKind.Enter:
			case UiPointerEventKind.Move:
				UpdateHover(pointerEvent.Position);
				break;
			case UiPointerEventKind.Leave:
				_hoveredButton = false;
				_hoveredItemIndex = -1;
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left) {
					HandleLeftPointerDown(pointerEvent);
				}
				break;
			case UiPointerEventKind.Up:
			case UiPointerEventKind.Click:
			case UiPointerEventKind.Wheel:
				UpdateHover(pointerEvent.Position);
				break;
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (!_enabled || !_visible || !Visible || !_hasKeyboardFocus || _items.Count == 0) {
			return;
		}

		var snapshot = Input.CaptureUiSnapshot();
		if (!IsExpanded) {
			if (snapshot.AcceptPressed) {
				SetExpanded(true);
			}

			return;
		}

		if (snapshot.NavigateDown) {
			MoveHoveredSelection(1);
		}

		if (snapshot.NavigateUp) {
			MoveHoveredSelection(-1);
		}

		if (snapshot.AcceptPressed) {
			CommitSelection(_hoveredItemIndex >= 0 ? _hoveredItemIndex : SelectedIndex);
		}

		if (snapshot.BackPressed || snapshot.EscapePressed) {
			SetExpanded(false);
		}
	}

	protected override void Draw() {
		if (!_visible || !Visible || Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var buttonBounds = GlobalBounds;
		var buttonFill = !_enabled
			? Blend(ButtonFill, Color.Black, 0.25f)
			: IsExpanded || _hoveredButton ? ButtonHoverFill : ButtonFill;

		Raylib.DrawRectangleRec(buttonBounds, buttonFill);
		Raylib.DrawRectangleLinesEx(buttonBounds, 1f, Outline);

		var label = string.IsNullOrEmpty(SelectedLabel) ? Placeholder : SelectedLabel;
		var buttonTextColor = ResolveButtonTextColor();
		var buttonTextY = buttonBounds.Y + ((buttonBounds.Height - FontSize) * 0.5f) - 1f;
		UiText.Draw(label, buttonBounds.X + _buttonTextInset, buttonTextY, FontSize, buttonTextColor, TextStyle);
		UiText.Draw(IsExpanded ? "^" : "v", buttonBounds.X + buttonBounds.Width - DefaultArrowInset, buttonTextY, FontSize, buttonTextColor, TextStyle);

		if (!IsExpanded) {
			return;
		}

		var expandedBounds = GetExpandedBounds(buttonBounds);
		Raylib.DrawRectangleRec(expandedBounds, ExpandedFill);
		Raylib.DrawRectangleLinesEx(expandedBounds, 1f, Outline);

		for (var index = 0; index < _items.Count; index++) {
			var itemBounds = GetItemBounds(expandedBounds, index);
			var isSelected = index == SelectedIndex;
			var isHovered = index == _hoveredItemIndex;
			var fill = isSelected
				? SelectedItemFill
				: isHovered ? HoverItemFill : ExpandedFill;
			var textColor = ResolveItemTextColor(index, isSelected, isHovered);

			Raylib.DrawRectangleRec(itemBounds, fill);
			Raylib.DrawRectangleLinesEx(itemBounds, 1f, Outline);
			UiText.Draw(_items[index].Label, itemBounds.X + GetListTextInset(), itemBounds.Y + ((itemBounds.Height - FontSize) * 0.5f) - 1f, FontSize, textColor, TextStyle);
		}
	}

	private void CommitSelection(int index) {
		if (!TryGetItem(index, out _)) {
			SetExpanded(false);
			return;
		}

		SelectedIndex = index;
		SetExpanded(false);
		SelectionCommitted?.Invoke(this);
	}

	private Rectangle GetExpandedBounds(Rectangle buttonBounds) {
		var configuredWidth = Math.Max(0f, _listTextArea.Right - _listTextArea.Left);
		var configuredHeight = Math.Max(0f, _listTextArea.Bottom - _listTextArea.Top);
		var width = Math.Max(buttonBounds.Width, configuredWidth + (GetListTextInset() * 2f));
		var height = Math.Max(configuredHeight, Math.Max(1, _items.Count) * ItemHeight);

		return new Rectangle(
			buttonBounds.X + _listOffset.X,
			buttonBounds.Y + _listOffset.Y,
			width,
			height);
	}

	private Rectangle GetItemBounds(Rectangle expandedBounds, int index) {
		return new Rectangle(
			expandedBounds.X,
			expandedBounds.Y + (index * ItemHeight),
			expandedBounds.Width,
			ItemHeight);
	}

	private float GetListTextInset() {
		return Math.Max(DefaultTextInset, _listTextArea.Left > 0 ? _listTextArea.Left : DefaultTextInset);
	}

	private void HandleLeftPointerDown(UiPointerEvent pointerEvent) {
		if (!_enabled) {
			return;
		}

		_hasKeyboardFocus = true;
		var position = pointerEvent.Position;
		var buttonBounds = GlobalBounds;

		if (IsExpanded) {
			var expandedBounds = GetExpandedBounds(buttonBounds);
			for (var index = 0; index < _items.Count; index++) {
				var itemBounds = GetItemBounds(expandedBounds, index);
				if (!Raylib.CheckCollisionPointRec(position, itemBounds)) {
					continue;
				}

				UpdateHover(position);
				CommitSelection(index);
				pointerEvent.MarkHandled();
				return;
			}

			if (Raylib.CheckCollisionPointRec(position, buttonBounds)) {
				SetExpanded(false);
				UpdateHover(position);
				pointerEvent.MarkHandled();
				return;
			}

			SetExpanded(false);
			_hoveredButton = false;
			_hoveredItemIndex = -1;
			pointerEvent.MarkHandled();
			return;
		}

		if (Raylib.CheckCollisionPointRec(position, buttonBounds)) {
			SetExpanded(true);
			UpdateHover(position);
			pointerEvent.MarkHandled();
		}
	}

	private void MoveHoveredSelection(int delta) {
		if (_items.Count == 0) {
			return;
		}

		var origin = _hoveredItemIndex >= 0 ? _hoveredItemIndex : SelectedIndex;
		if (origin < 0) {
			origin = 0;
		}

		_hoveredItemIndex = Math.Clamp(origin + delta, 0, _items.Count - 1);
	}

	private Color ResolveButtonTextColor() {
		if (!_enabled) {
			return DisabledTextColor;
		}

		if (_hoveredButton || IsExpanded || _hasKeyboardFocus) {
			return HighlightTextColor;
		}

		return NormalTextColor;
	}

	private Color ResolveItemTextColor(int index, bool isSelected, bool isHovered) {
		if (!_enabled) {
			return isSelected ? SelectedItemGrayedTextColor : DisabledTextColor;
		}

		if (TryGetItem(index, out var item) && item.TextColorOverride.HasValue) {
			return item.TextColorOverride.Value;
		}

		if (isSelected) {
			return SelectedItemTextColor;
		}

		if (isHovered) {
			return HighlightTextColor;
		}

		return NormalTextColor;
	}

	private void SetExpanded(bool expanded) {
		IsExpanded = expanded && _items.Count > 0;
		if (!IsExpanded) {
			_hoveredItemIndex = -1;
		}
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
		_hoveredButton = ContainsPoint(position);
		_hoveredItemIndex = -1;
		if (!IsExpanded) {
			return;
		}

		var expandedBounds = GetExpandedBounds(GlobalBounds);
		for (var index = 0; index < _items.Count; index++) {
			if (Raylib.CheckCollisionPointRec(position, GetItemBounds(expandedBounds, index))) {
				_hoveredItemIndex = index;
				return;
			}
		}
	}

	private static Color Blend(Color baseColor, Color tint, float tintAmount) {
		tintAmount = Math.Clamp(tintAmount, 0f, 1f);
		var baseAmount = 1f - tintAmount;
		return new Color(
			(int)MathF.Round((baseColor.R * baseAmount) + (tint.R * tintAmount)),
			(int)MathF.Round((baseColor.G * baseAmount) + (tint.G * tintAmount)),
			(int)MathF.Round((baseColor.B * baseAmount) + (tint.B * tintAmount)),
			(int)MathF.Round((baseColor.A * baseAmount) + (tint.A * tintAmount)));
	}

	private static string SanitizeLabel(string label) {
		return string.IsNullOrWhiteSpace(label) ? string.Empty : label.Trim();
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color(color.Red, color.Green, color.Blue, (byte)255);
	}

	private readonly record struct ItemEntry(string Label, uint DataValue = 0u, Color? TextColorOverride = null);
}
