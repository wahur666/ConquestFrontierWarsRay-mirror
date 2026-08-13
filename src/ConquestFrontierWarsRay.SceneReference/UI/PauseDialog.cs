using ConquestFrontierWarsRay.SceneReference.Core;
using ConquestFrontierWarsRay.SceneReference.Input;
using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.UI;

/// <summary>
///     A modal confirmation popup: dims the screen, shows a title and a navigable MenuList.
///     B/Cancel dismisses it without acting. Scenes own an instance, open it on their trigger
///     (Back button, a combo, etc), and skip their own gameplay input/update while it's open.
/// </summary>
public sealed class PauseDialog {
	private const int PanelWidth = 400;
	private const int PanelHeight = 200;
	private readonly MenuList _menu = new();

	private readonly string _title;

	public PauseDialog(string title) {
		_title = title;
	}

	public bool IsOpen { get; private set; }

	public void AddItem(string label, Action onSelect) {
		_menu.AddItem(label, () => {
			Close();
			onSelect();
		});
	}

	/// <summary>Adds an item that just closes the dialog and unpauses, no scene change.</summary>
	public void AddResumeItem(string label = "Resume") {
		AddItem(label, () => { });
	}

	public void Open() {
		IsOpen = true;
	}

	public void Close() {
		IsOpen = false;
	}

	public void HandleInput() {
		if (!IsOpen) {
			return;
		}

		_menu.HandleInput();

		if (MenuNavigation.Cancel) {
			Close();
		}
	}

	public void Draw() {
		if (!IsOpen) {
			return;
		}

		Raylib.DrawRectangle(0, 0, Display.Width, Display.Height, new Color(0, 0, 0, 180));

		var panelX = (Display.Width - PanelWidth) / 2;
		var panelY = (Display.Height - PanelHeight) / 2;
		Raylib.DrawRectangle(panelX, panelY, PanelWidth, PanelHeight, Color.DarkGray);
		Raylib.DrawRectangleLines(panelX, panelY, PanelWidth, PanelHeight, Color.RayWhite);

		CombatUiAssets.DrawTextCentered(_title, panelX + (PanelWidth / 2f), panelY + 24f, 24, Color.RayWhite);

		_menu.Draw(Display.Width / 2, panelY + 90, 36, 20);
	}
}
