namespace ConquestFrontierWarsRay.SceneReference.Input;

/// <summary>
///     Turns raw GamepadInput state into one-shot menu intents (d-pad and left stick both
///     move the selection). Call Poll() once per frame, after GamepadInput.Poll().
/// </summary>
public static class MenuNavigation {
	private static bool _comboLatch;

	public static bool Up { get; private set; }
	public static bool Down { get; private set; }
	public static bool Left { get; private set; }
	public static bool Right { get; private set; }

	public static bool Confirm { get; private set; }
	public static bool Cancel { get; private set; }
	public static bool Back { get; private set; }

	/// <summary>Fires once on the frame both L3 and R3 become held together.</summary>
	public static bool ExitComboPressed { get; private set; }

	public static void Poll() {
		Up = InputManager.UiUp;
		Down = InputManager.UiDown;
		Left = InputManager.UiLeft;
		Right = InputManager.UiRight;

		Confirm = InputManager.UiAccept;
		Cancel = InputManager.UiEsc;
		Back = InputManager.UiBack;

		var comboDown = GamepadInput.L3 && GamepadInput.R3;
		ExitComboPressed = InputManager.UiBack || (comboDown && !_comboLatch);
		_comboLatch = comboDown;
	}
}
