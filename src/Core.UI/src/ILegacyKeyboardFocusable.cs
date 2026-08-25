namespace ConquestFrontierWarsRay.Core.UI;

/// <summary>
/// Minimal keyboard-focus contract shared by legacy-authored controls.
/// </summary>
public interface ILegacyKeyboardFocusable {
	bool SetKeyboardFocus(bool enabled);
}
