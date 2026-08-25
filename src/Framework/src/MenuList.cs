using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Vertical menu list driven by one-shot menu navigation intents.
/// </summary>
public sealed class MenuList {
	private readonly List<(string Label, Action OnSelect)> _items = [];

	public int SelectedIndex { get; private set; }

	public void AddItem(string label, Action onSelect) {
		ArgumentException.ThrowIfNullOrWhiteSpace(label);
		ArgumentNullException.ThrowIfNull(onSelect);

		_items.Add((label, onSelect));
	}

	public void SetSelectedIndex(int index) {
		if (_items.Count == 0) {
			SelectedIndex = 0;
			return;
		}

		SelectedIndex = Math.Clamp(index, 0, _items.Count - 1);
	}

	public void HandleInput(MenuNavigation navigation) {
		ArgumentNullException.ThrowIfNull(navigation);

		if (_items.Count == 0) {
			return;
		}

		if (navigation.Up) {
			SelectedIndex = (SelectedIndex - 1 + _items.Count) % _items.Count;
		}

		if (navigation.Down) {
			SelectedIndex = (SelectedIndex + 1) % _items.Count;
		}

		if (navigation.Confirm) {
			_items[SelectedIndex].OnSelect();
		}
	}

	public void Draw(float centerX, float startY, float itemSpacing, float fontSize) {
		for (var i = 0; i < _items.Count; i++) {
			var selected = i == SelectedIndex;
			var label = selected ? $"> {_items[i].Label} <" : _items[i].Label;
			var color = selected ? Color.Lime : Color.RayWhite;
			var width = UiText.MeasureWidth(label, fontSize);
			UiText.Draw(label, centerX - (width * 0.5f), startY + (i * itemSpacing), fontSize, color);
		}
	}
}
