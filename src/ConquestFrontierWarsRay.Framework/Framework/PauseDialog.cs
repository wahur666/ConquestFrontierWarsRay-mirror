using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Small modal pause dialog with a dimmed backdrop and menu list.
/// </summary>
internal sealed class PauseDialog {
	private const int PanelWidth = 400;
	private const int PanelHeight = 200;
	private readonly MenuList _menu = new();
	private readonly string _title;

	public PauseDialog(string title) {
		_title = string.IsNullOrWhiteSpace(title) ? "Pause" : title;
	}

	public bool IsOpen { get; private set; }

	public void AddItem(string label, Action onSelect) {
		_menu.AddItem(label, () => {
			Close();
			onSelect();
		});
	}

	public void AddResumeItem(string label = "Resume") {
		AddItem(label, () => {
		});
	}

	public void Open() {
		IsOpen = true;
	}

	public void Close() {
		IsOpen = false;
	}

	public void HandleInput(MenuNavigation navigation) {
		ArgumentNullException.ThrowIfNull(navigation);

		if (!IsOpen) {
			return;
		}

		_menu.HandleInput(navigation);

		if (navigation.Cancel) {
			Close();
		}
	}

	public void Draw() {
		if (!IsOpen) {
			return;
		}

		var screenWidth = Raylib.GetScreenWidth();
		var screenHeight = Raylib.GetScreenHeight();
		Raylib.DrawRectangle(0, 0, screenWidth, screenHeight, new Color(0, 0, 0, 180));

		var panelX = (screenWidth - PanelWidth) / 2;
		var panelY = (screenHeight - PanelHeight) / 2;
		Raylib.DrawRectangle(panelX, panelY, PanelWidth, PanelHeight, Color.DarkGray);
		Raylib.DrawRectangleLines(panelX, panelY, PanelWidth, PanelHeight, Color.RayWhite);

		UiText.DrawCentered(_title, panelX + (PanelWidth * 0.5f), panelY + 24f, 24f, Color.RayWhite);
		_menu.Draw(screenWidth * 0.5f, panelY + 90f, 36f, 20f);
	}
}
