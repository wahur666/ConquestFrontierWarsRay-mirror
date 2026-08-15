using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Simple dropdown control for selecting one item from a small list.
/// </summary>
public sealed class DropdownNode : Control {
	private readonly List<string> _items = [];

	public DropdownNode(string? name = null) : base(name) {
	}

	public IReadOnlyList<string> Items => _items;

	public int SelectedIndex { get; private set; } = -1;

	public string SelectedLabel => SelectedIndex >= 0 && SelectedIndex < _items.Count
		? _items[SelectedIndex]
		: string.Empty;

	public bool IsExpanded { get; private set; }

	public float FontSize { get; set; } = 14f;
	public float ItemHeight { get; set; } = 28f;
	public Color Fill { get; set; } = new(34, 42, 52, 255);
	public Color HoverFill { get; set; } = new(54, 67, 82, 255);
	public Color ExpandedFill { get; set; } = new(28, 34, 48, 255);
	public Color Outline { get; set; } = new(140, 160, 180, 200);
	public Color TextColor { get; set; } = Color.RayWhite;
	public Color SelectedItemFill { get; set; } = new(74, 108, 170, 255);
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;
	public string Placeholder { get; set; } = "Select";

	public void SetItems(IEnumerable<string> items, int selectedIndex = 0) {
		ArgumentNullException.ThrowIfNull(items);

		_items.Clear();
		_items.AddRange(items.Where(static item => !string.IsNullOrWhiteSpace(item)));

		if (_items.Count == 0) {
			SelectedIndex = -1;
			IsExpanded = false;
			return;
		}

		SelectedIndex = Math.Clamp(selectedIndex, 0, _items.Count - 1);
		IsExpanded = false;
	}

	public void SetSelectedIndex(int index) {
		if (_items.Count == 0) {
			SelectedIndex = -1;
			return;
		}

		SelectedIndex = Math.Clamp(index, 0, _items.Count - 1);
	}

	public bool HandleInput() {
		if (!Visible || Size.X <= 0f || Size.Y <= 0f || !Raylib.IsMouseButtonPressed(MouseButton.Left)) {
			return false;
		}

		var mouse = Raylib.GetMousePosition();
		var collapsedBounds = GlobalBounds;

		if (ContainsPoint(mouse)) {
			IsExpanded = !IsExpanded;
			return false;
		}

		if (!IsExpanded) {
			return false;
		}

		for (var i = 0; i < _items.Count; i++) {
			var itemBounds = GetItemBounds(i);
			if (!Raylib.CheckCollisionPointRec(mouse, itemBounds)) {
				continue;
			}

			var changed = SelectedIndex != i;
			SelectedIndex = i;
			IsExpanded = false;
			return changed;
		}

		if (!Raylib.CheckCollisionPointRec(mouse, GetExpandedBounds(collapsedBounds))) {
			IsExpanded = false;
		}

		return false;
	}

	protected override void Draw() {
		if (Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var bounds = GlobalBounds;
		var mouse = Raylib.GetMousePosition();
		var hovered = ContainsPoint(mouse);
		var fill = hovered || IsExpanded ? HoverFill : Fill;

		Raylib.DrawRectangleRec(bounds, fill);
		Raylib.DrawRectangleLinesEx(bounds, 1f, Outline);

		var label = string.IsNullOrEmpty(SelectedLabel) ? Placeholder : SelectedLabel;
		var textY = bounds.Y + ((bounds.Height - FontSize) * 0.5f) - 1f;
		UiText.Draw(label, bounds.X + 10f, textY, FontSize, TextColor, TextStyle);
		UiText.Draw(IsExpanded ? "^" : "v", bounds.X + bounds.Width - 18f, textY, FontSize, TextColor, TextStyle);

		if (!IsExpanded || _items.Count == 0) {
			return;
		}

		var expandedBounds = GetExpandedBounds(bounds);
		Raylib.DrawRectangleRec(expandedBounds, ExpandedFill);
		Raylib.DrawRectangleLinesEx(expandedBounds, 1f, Outline);

		for (var i = 0; i < _items.Count; i++) {
			var itemBounds = GetItemBounds(i);
			var itemHovered = Raylib.CheckCollisionPointRec(mouse, itemBounds);
			var itemFill = i == SelectedIndex
				? SelectedItemFill
				: itemHovered ? HoverFill : ExpandedFill;

			Raylib.DrawRectangleRec(itemBounds, itemFill);
			Raylib.DrawRectangleLinesEx(itemBounds, 1f, Outline);
			UiText.Draw(_items[i], itemBounds.X + 10f, itemBounds.Y + ((itemBounds.Height - FontSize) * 0.5f) - 1f, FontSize, TextColor, TextStyle);
		}
	}

	private Rectangle GetExpandedBounds(Rectangle collapsedBounds) {
		return new Rectangle(
			collapsedBounds.X,
			collapsedBounds.Y + collapsedBounds.Height,
			collapsedBounds.Width,
			ItemHeight * _items.Count);
	}

	private Rectangle GetItemBounds(int index) {
		var bounds = GlobalBounds;
		return new Rectangle(
			bounds.X,
			bounds.Y + bounds.Height + (index * ItemHeight),
			bounds.Width,
			ItemHeight);
	}
}
