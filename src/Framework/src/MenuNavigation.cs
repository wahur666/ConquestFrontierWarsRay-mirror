namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Converts frame input into one-shot menu navigation intents.
/// </summary>
public sealed class MenuNavigation {
	private bool _comboLatch;

	public bool Up { get; private set; }
	public bool Down { get; private set; }
	public bool Left { get; private set; }
	public bool Right { get; private set; }
	public bool Confirm { get; private set; }
	public bool Cancel { get; private set; }
	public bool Back { get; private set; }
	public bool ExitComboPressed { get; private set; }

	public void Update(InputManager input) {
		ArgumentNullException.ThrowIfNull(input);

		Up = input.UiUp;
		Down = input.UiDown;
		Left = input.UiLeft;
		Right = input.UiRight;
		Confirm = input.UiAccept;
		Cancel = input.UiEsc;
		Back = input.UiBack;

		var comboDown = input.Gamepad.L3 && input.Gamepad.R3;
		ExitComboPressed = input.UiBack || (comboDown && !_comboLatch);
		_comboLatch = comboDown;
	}
}
