using System.Numerics;
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
public sealed class LegacyDropdownNode : Control, IUiPointerEventHandler, ILegacyKeyboardFocusable {
	private const float DefaultFontSize = 16f;
	private readonly LegacyButtonNode _button;
	private readonly LegacyListBoxNode _listBox;
	private Vector2 _authoredPopupOffset = Vector2.Zero;
	private bool _enabled = true;
	private bool _hasKeyboardFocus;
	private float _fontSize = DefaultFontSize;
	private Vector2? _popupOffsetOverride;
	private bool _visible = true;

	public LegacyDropdownNode(string? name = null) : base(name) {
		_button = AddChild(new LegacyButtonNode($"{Name ?? "LegacyDropdown"}Button"));
		_button.Activated += _ => ToggleExpanded();
		_button.ForceDropdownIndicator = true;
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
	public LegacyButtonNode.LegacyButtonVisualState ButtonVisualStateOverride {
		get => _button.VisualStateOverride;
		set => _button.VisualStateOverride = value;
	}

	public int SelectedIndex => _listBox.GetCurrentSelection();

	public string SelectedLabel => _listBox.SelectedLabel;

	public bool IsPointerInputEnabled => Visible && _visible && Size.X > 0f && Size.Y > 0f;

	public IReadOnlyList<string> Items => _listBox.Items;

	public Vector2 PopupOffset {
		get => _popupOffsetOverride ?? _authoredPopupOffset;
		set {
			_popupOffsetOverride = value;
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
			_button.TextStyle = TextStyle;
			_listBox.FontSize = _fontSize;
		}
	}

	public void ApplyLegacyDefinition(
		GT_BUTTON buttonArchetype,
		GT_LISTBOX listboxArchetype,
		DROPDOWN_DATA data,
		VfxAnimationDataRepository? repository = null,
		XmlDbRepository? xmlDbRepository = null) {
		ArgumentNullException.ThrowIfNull(buttonArchetype);
		ArgumentNullException.ThrowIfNull(listboxArchetype);
		ArgumentNullException.ThrowIfNull(data);

		DisabledTextColor = ToColor(buttonArchetype.DisabledText);
		NormalTextColor = ToColor(buttonArchetype.NormalText);
		HighlightTextColor = ToColor(buttonArchetype.HighlightText);

		var width = Math.Max(0f, data.ScreenRect.Right - data.ScreenRect.Left);
		var height = Math.Max(0f, data.ScreenRect.Bottom - data.ScreenRect.Top);
		Size = new Vector2(width, height);
		Position = new Vector2(data.ScreenRect.Left, data.ScreenRect.Top);
		_button.ApplyLegacyDefinition(buttonArchetype, repository: repository);
		_button.Size = Size;
		_button.EnableButton(_enabled);
		_button.SetVisible(_visible);
		_button.TextStyle = TextStyle;
		_button.Position = Vector2.Zero;
		_authoredPopupOffset = ResolvePopupOffset(data);
		_popupOffsetOverride = null;

		_listBox.ApplyLegacyDefinition(listboxArchetype, data.ListboxData, repository, xmlDbRepository);
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
		_button.EnableButton(enabled);
		_listBox.EnableListbox(enabled);
		if (!_enabled) {
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
			UpdateExpandedState();
		}
	}

	public bool SetKeyboardFocus(bool enabled) {
		_hasKeyboardFocus = enabled && _enabled && _visible && Visible;
		_button.SetKeyboardFocus(_hasKeyboardFocus);
		UpdateChildFocus();
		if (!_hasKeyboardFocus) {
			SetExpanded(false);
		}

		return _hasKeyboardFocus;
	}

	public void SetSelectionColor(Color color) {
		NormalTextColor = color;
	}

	public void SetExpandedState(bool expanded) {
		SetExpanded(expanded);
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
			case UiPointerEventKind.Down:
				if (pointerEvent.Button == MouseButton.Left) {
					HandleLeftPointerDown(pointerEvent);
				}
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
		_button.Text = string.IsNullOrEmpty(SelectedLabel) ? Placeholder : SelectedLabel;
	}

	private void HandleLeftPointerDown(UiPointerEvent pointerEvent) {
		if (!_enabled) {
			return;
		}

		_hasKeyboardFocus = true;
		var position = pointerEvent.Position;

		if (IsExpanded) {
			if (_listBox.HitTest(position)) {
				if (_listBox.GetCurrentSelection() >= 0) {
					HandleListBoxSelectionCommitted(_listBox);
					pointerEvent.MarkHandled();
				}

				return;
			}

			SetExpanded(false);
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
		_button.PushState = IsExpanded;
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
		_listBox.Position = _popupOffsetOverride ?? _authoredPopupOffset;
	}

	private void UpdateChildFocus() {
		_button.SetKeyboardFocus(_hasKeyboardFocus && !IsExpanded);
		if (!_hasKeyboardFocus) {
			_listBox.SetKeyboardFocus(false);
			return;
		}

		_listBox.SetKeyboardFocus(IsExpanded);
	}

	private void ToggleExpanded() {
		if (!_enabled) {
			return;
		}

		SetExpanded(!IsExpanded);
	}

	private static Color ToColor(GT_COLOR color) {
		return new Color(color.Red, color.Green, color.Blue, (byte)255);
	}

	private static Vector2 ResolvePopupOffset(DROPDOWN_DATA data) {
		var x = data.ListboxData.XOrigin;
		var y = data.ListboxData.YOrigin;

		if (x >= data.ScreenRect.Left) {
			x -= data.ScreenRect.Left;
		}

		if (y >= data.ScreenRect.Top) {
			y -= data.ScreenRect.Top;
		}

		return new Vector2(x, y);
	}
}
