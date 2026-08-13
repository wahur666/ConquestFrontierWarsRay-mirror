using ConquestFrontierWarsRay.SceneReference.Core;
using ConquestFrontierWarsRay.SceneReference.Input;
using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.UI;

/// <summary>
///     A vertical list of selectable labels, navigated with the d-pad/stick (via MenuNavigation)
///     and confirmed with A. Reused by full-screen menus and by PauseDialog.
/// </summary>
public sealed class MenuList {
	private readonly List<(string Label, Action OnSelect)> _items = new();

	public int SelectedIndex { get; private set; }

	public void AddItem(string label, Action onSelect) {
		_items.Add((label, onSelect));
	}

	public void HandleInput() {
		if (_items.Count == 0) {
			return;
		}

		if (MenuNavigation.Up) {
			SelectedIndex = (SelectedIndex - 1 + _items.Count) % _items.Count;
		}

		if (MenuNavigation.Down) {
			SelectedIndex = (SelectedIndex + 1) % _items.Count;
		}

		if (MenuNavigation.Confirm) {
			_items[SelectedIndex].OnSelect.Invoke();
		}
	}

	public void Draw(int centerX, int startY, int itemSpacing, int fontSize) {
		for (var i = 0; i < _items.Count; i++) {
			var selected = i == SelectedIndex;
			var label = selected ? $"> {_items[i].Label} <" : _items[i].Label;
			var color = selected ? Color.Lime : Color.RayWhite;

			var width = CombatUiAssets.MeasureText(label, fontSize);
			CombatUiAssets.DrawText(label, centerX - (width / 2f), startY + (i * itemSpacing), fontSize, color);
		}
	}
}
