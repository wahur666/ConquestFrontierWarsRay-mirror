using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Mouse-driven single-selection list of clickable rows.
/// </summary>
/// <remarks>
/// Input is intentionally explicit. Call <see cref="HandleInput"/> during the
/// owning scene or UI controller update step to apply selection changes.
/// </remarks>
public sealed class ListViewNode : Control {
	private readonly List<string> _items = [];

	public ListViewNode(string? name = null) : base(name) {
	}

	public IReadOnlyList<string> Items => _items;

	public int SelectedIndex { get; private set; } = -1;

	public string SelectedLabel => SelectedIndex >= 0 && SelectedIndex < _items.Count
		? _items[SelectedIndex]
		: string.Empty;

	public float FontSize { get; set; } = 16f;
	public float ItemHeight { get; set; } = 52f;
	public float ItemSpacing { get; set; } = 8f;
	public float HorizontalPadding { get; set; } = 14f;
	public float VerticalPadding { get; set; } = 10f;
	public Color Fill { get; set; } = new(30, 38, 56, 255);
	public Color HoverFill { get; set; } = new(44, 58, 84, 255);
	public Color SelectedFill { get; set; } = new(74, 108, 170, 255);
	public Color Outline { get; set; } = new(82, 98, 126, 255);
	public Color SelectedOutline { get; set; } = Color.Gold;
	public Color TextColor { get; set; } = Color.RayWhite;
	public UiTextStyle TextStyle { get; set; } = UiTextStyle.Body;

	public void SetItems(IEnumerable<string> items, int selectedIndex = 0) {
		ArgumentNullException.ThrowIfNull(items);

		_items.Clear();
		_items.AddRange(items.Where(static item => !string.IsNullOrWhiteSpace(item)));

		if (_items.Count == 0) {
			SelectedIndex = -1;
			return;
		}

		SelectedIndex = Math.Clamp(selectedIndex, 0, _items.Count - 1);
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
		for (var i = 0; i < _items.Count; i++) {
			var itemBounds = GetItemBounds(i);
			if (!Raylib.CheckCollisionPointRec(mouse, itemBounds)) {
				continue;
			}

			var changed = SelectedIndex != i;
			SelectedIndex = i;
			return changed;
		}

		return false;
	}

	protected override void Draw() {
		if (Size.X <= 0f || Size.Y <= 0f) {
			return;
		}

		var mouse = Raylib.GetMousePosition();
		for (var i = 0; i < _items.Count; i++) {
			var itemBounds = GetItemBounds(i);
			if (itemBounds.Y >= GlobalBounds.Y + GlobalBounds.Height) {
				break;
			}

			var selected = i == SelectedIndex;
			var hovered = Raylib.CheckCollisionPointRec(mouse, itemBounds);
			var fill = selected
				? SelectedFill
				: hovered ? HoverFill : Fill;

			Raylib.DrawRectangleRec(itemBounds, fill);
			Raylib.DrawRectangleLinesEx(itemBounds, 1.25f, selected ? SelectedOutline : Outline);
			UiText.Draw(
				_items[i],
				itemBounds.X + HorizontalPadding,
				itemBounds.Y + VerticalPadding,
				FontSize,
				TextColor,
				TextStyle);
		}
	}

	private Rectangle GetItemBounds(int index) {
		var bounds = GlobalBounds;
		var y = bounds.Y + (index * (ItemHeight + ItemSpacing));
		return new Rectangle(bounds.X, y, bounds.Width, ItemHeight);
	}
}
