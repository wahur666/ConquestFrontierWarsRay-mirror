using System.Numerics;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb;
using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Legacy-authored dropdown that keeps Trim-era placement and imperative item APIs.
/// </summary>
/// <remarks>
/// The collapsed button is rendered here. The expanded popup is delegated to
/// <see cref="LegacyListBoxNode"/>, which owns the list visuals, scrolling,
/// caret movement, and selection behavior.
/// </remarks>
public sealed class LegacyDropdownNode : Control, IUiPointerEventHandler {
	private const float DefaultFontSize = 16f;
	private const float DefaultTextInset = 8f;
	private const float DefaultArrowInset = 16f;
	private readonly LegacyListBoxNode _listBox;
	private bool _enabled = true;
	private bool _hasKeyboardFocus;
	private bool _hoveredButton;
	private float _buttonTextInset = DefaultTextInset;
	private float _fontSize = DefaultFontSize;
	private Vector2 _popupOffset = Vector2.Zero;
	private bool _visible = true;

	public LegacyDropdownNode(string? name = null) : base(name) {
		_listBox = AddChild(new LegacyListBoxNode($"{Name ?? "LegacyDropdown"}ListBox"));
		_listBox.SelectionCommitted += HandleListBoxSelectionCommitted;
		_listBox.SetVisible(false);
	}

	public event Action<LegacyDropdownNode>? SelectionCommitted;

	public uint ControlId {
		get => _listBox.ControlId;
		set => _listBox.ControlId = value;
	}

	public bool IsExpanded { get; private set; }

	public int SelectedIndex => _listBox.GetCurrentSelection();

	public string SelectedLabel => _listBox.SelectedLabel;

	public bool IsPointerInputEnabled => Visible && _visible && Size.X > 0f && Size.Y > 0f;

	public IReadOnlyList<string> Items => _listBox.Items;

	public Vector2 PopupOffset {
		get => _popupOffset;
		set {
			_popupOffset = value;
			ApplyPopupOffset();
		}
	}

	public Color DisabledTextColor { get; private set; } = new(100, 100, 100, 255);
	public Color NormalTextColor { get; private set; } = new(180, 160, 120, 255);
	public Color HighlightTextColor { get; private set; } = new(200, 180, 140, 255);
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;
	public string Placeholder { get; set; } = string.Empty;
	public float FontSize {
		get => _fontSize;
		set {
			_fontSize = Math.Max(8f, value);
			_listBox.FontSize = _fontSize;
		}
	}

	public void ApplyLegacyDefinition(
		GT_BUTTON buttonArchetype,
		GT_LISTBOX listboxArchetype,
		DROPDOWN_DATA data,
		VfxAnimationDataRepository? repository = null,
		UtfDbRepository? utfDbRepository = null) {
		ArgumentNullException.ThrowIfNull(buttonArchetype);
		ArgumentNullException.ThrowIfNull(listboxArchetype);
		ArgumentNullException.ThrowIfNull(data);

		DisabledTextColor = ToColor(buttonArchetype.DisabledText);
		NormalTextColor = ToColor(buttonArchetype.NormalText);
		HighlightTextColor = ToColor(buttonArchetype.HighlightText);
		_buttonTextInset = Math.Max(DefaultTextInset, buttonArchetype.LeftMargin > 0 ? buttonArchetype.LeftMargin : DefaultTextInset);

		var width = Math.Max(0f, data.ScreenRect.Right - data.ScreenRect.Left);
		var height = Math.Max(0f, data.ScreenRect.Bottom - data.ScreenRect.Top);
		Size = new Vector2(width, height);
		Position = new Vector2(data.ScreenRect.Left, data.ScreenRect.Top);
		_popupOffset = new Vector2(data.ListboxData.XOrigin, data.ListboxData.YOrigin);

		_listBox.ApplyLegacyDefinition(listboxArchetype, data.ListboxData, repository, utfDbRepository);
		ApplyPopupOffset();
		_listBox.SetVisible(false);
		_listBox.SetKeyboardFocus(false);
		_listBox.TextStyle = TextStyle;
		_listBox.FontSize = FontSize;
		_listBox.ControlId = ControlId;
		_listBox.CommitOnSingleClickPointerDown = true;
	}

	public void EnableDropdown(bool enabled) {
		_enabled = enabled;
		_listBox.EnableListbox(enabled);
		if (!_enabled) {
			_hasKeyboardFocus = false;
			_hoveredButton = false;
			SetExpanded(false);
		}
	}

	public void SetVisible(bool visible) {
		_visible = visible;
		if (!visible) {
			_hoveredButton = false;
			SetExpanded(false);
		} else {
			UpdateExpandedState();
		}
	}

	public void SetKeyboardFocus(bool enabled) {
		_hasKeyboardFocus = enabled && _enabled && _visible && Visible;
		UpdateChildFocus();
		if (!_hasKeyboardFocus) {
			SetExpanded(false);
		}
	}

	public void SetSelectionColor(Color color) {
		NormalTextColor = color;
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
		return _listBox.SetCurrentSelection(newIndex);
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
				_hoveredButton = ContainsPoint(pointerEvent.Position);
				break;
			case UiPointerEventKind.Leave:
				_hoveredButton = false;
				break;
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left) {
					HandleLeftPointerDown(pointerEvent);
				}
				break;
			case UiPointerEventKind.Up:
			case UiPointerEventKind.Click:
			case UiPointerEventKind.Wheel:
				_hoveredButton = ContainsPoint(pointerEvent.Position);
				break;
		}
	}

	protected override void OnUpdate(float deltaTime) {
		base.OnUpdate(deltaTime);

		if (!_enabled || !_visible || !Visible || !_hasKeyboardFocus) {
			return;
		}

		var snapshot = Input.CaptureUiSnapshot();
		if (!IsExpanded) {
			if (snapshot.AcceptPressed && _listBox.GetNumberOfItems() > 0) {
				SetExpanded(true);
			}

			return;
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
			? Blend(new Color(42, 48, 58, 255), Color.Black, 0.25f)
			: IsExpanded || _hoveredButton ? new Color(52, 58, 70, 255) : new Color(42, 48, 58, 255);

		Raylib.DrawRectangleRec(buttonBounds, buttonFill);
		Raylib.DrawRectangleLinesEx(buttonBounds, 1f, new Color(132, 146, 168, 220));

		var label = string.IsNullOrEmpty(SelectedLabel) ? Placeholder : SelectedLabel;
		var buttonTextColor = ResolveButtonTextColor();
		var buttonTextY = buttonBounds.Y + ((buttonBounds.Height - FontSize) * 0.5f) - 1f;
		UiText.Draw(label, buttonBounds.X + _buttonTextInset, buttonTextY, FontSize, buttonTextColor, TextStyle);
		UiText.Draw(IsExpanded ? "^" : "v", buttonBounds.X + buttonBounds.Width - DefaultArrowInset, buttonTextY, FontSize, buttonTextColor, TextStyle);
	}

	private void HandleLeftPointerDown(UiPointerEvent pointerEvent) {
		if (!_enabled) {
			return;
		}

		_hasKeyboardFocus = true;
		var position = pointerEvent.Position;
		var buttonBounds = GlobalBounds;

		if (IsExpanded) {
			if (Raylib.CheckCollisionPointRec(position, buttonBounds)) {
				SetExpanded(false);
				pointerEvent.MarkHandled();
				return;
			}

			if (_listBox.HitTest(position)) {
				if (_listBox.GetCurrentSelection() >= 0) {
					HandleListBoxSelectionCommitted(_listBox);
					pointerEvent.MarkHandled();
				}

				return;
			}

			SetExpanded(false);
			_hoveredButton = false;
			pointerEvent.MarkHandled();
			return;
		}

		if (Raylib.CheckCollisionPointRec(position, buttonBounds)) {
			SetExpanded(true);
			pointerEvent.MarkHandled();
		}
	}

	private void HandleListBoxSelectionCommitted(LegacyListBoxNode listBox) {
		if (listBox.GetCurrentSelection() < 0) {
			SetExpanded(false);
			return;
		}

		SetExpanded(false);
		SelectionCommitted?.Invoke(this);
	}

	private void SetExpanded(bool expanded) {
		IsExpanded = expanded && _enabled && _visible && Visible && _listBox.GetNumberOfItems() > 0;
		if (IsExpanded) {
			_listBox.EnsureVisible(_listBox.GetCurrentSelection() >= 0 ? _listBox.GetCurrentSelection() : 0);
		}

		UpdateExpandedState();
		UpdateChildFocus();
	}

	private void UpdateExpandedState() {
		_listBox.SetVisible(_visible && Visible && IsExpanded);
	}

	private void ApplyPopupOffset() {
		_listBox.Position = _popupOffset;
	}

	private void UpdateChildFocus() {
		if (!_hasKeyboardFocus) {
			_listBox.SetKeyboardFocus(false);
			return;
		}

		_listBox.SetKeyboardFocus(IsExpanded);
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

	private static Color Blend(Color baseColor, Color tint, float tintAmount) {
		tintAmount = Math.Clamp(tintAmount, 0f, 1f);
		var baseAmount = 1f - tintAmount;
		return new Color(
			(int)MathF.Round((baseColor.R * baseAmount) + (tint.R * tintAmount)),
			(int)MathF.Round((baseColor.G * baseAmount) + (tint.G * tintAmount)),
			(int)MathF.Round((baseColor.B * baseAmount) + (tint.B * tintAmount)),
			(int)MathF.Round((baseColor.A * baseAmount) + (tint.A * tintAmount)));
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color(color.Red, color.Green, color.Blue, (byte)255);
	}
}
