using System.Runtime.InteropServices;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Polled state for one gamepad using Xbox-style button naming.
/// </summary>
internal sealed class GamepadState {
	private const int PlayerIndex = 0;
	private const float StickDeadzone = 0.15f;

	public bool IsConnected { get; private set; }
	public string Name { get; private set; } = string.Empty;

	public bool A { get; private set; }
	public bool B { get; private set; }
	public bool X { get; private set; }
	public bool Y { get; private set; }

	public bool DPadUp { get; private set; }
	public bool DPadRight { get; private set; }
	public bool DPadDown { get; private set; }
	public bool DPadLeft { get; private set; }

	public bool LB { get; private set; }
	public bool RB { get; private set; }

	public bool Back { get; private set; }
	public bool Guide { get; private set; }
	public bool Start { get; private set; }
	public bool L3 { get; private set; }
	public bool R3 { get; private set; }

	public float LeftStickX { get; private set; }
	public float LeftStickY { get; private set; }
	public float RightStickX { get; private set; }
	public float RightStickY { get; private set; }
	public float LeftTrigger { get; private set; }
	public float RightTrigger { get; private set; }

	public bool APressed { get; private set; }
	public bool BPressed { get; private set; }
	public bool XPressed { get; private set; }
	public bool YPressed { get; private set; }
	public bool StartPressed { get; private set; }
	public bool BackPressed { get; private set; }
	public bool L3Pressed { get; private set; }
	public bool R3Pressed { get; private set; }
	public bool DPadUpPressed { get; private set; }
	public bool DPadRightPressed { get; private set; }
	public bool DPadDownPressed { get; private set; }
	public bool DPadLeftPressed { get; private set; }

	public void Poll() {
		IsConnected = Raylib.IsGamepadAvailable(PlayerIndex);
		if (!IsConnected) {
			Reset();
			return;
		}

		unsafe {
			Name = Marshal.PtrToStringUTF8((IntPtr)Raylib.GetGamepadName(PlayerIndex)) ?? string.Empty;
		}

		A = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.RightFaceDown);
		B = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.RightFaceRight);
		X = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.RightFaceLeft);
		Y = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.RightFaceUp);

		DPadUp = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.LeftFaceUp);
		DPadRight = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.LeftFaceRight);
		DPadDown = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.LeftFaceDown);
		DPadLeft = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.LeftFaceLeft);

		LB = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.LeftTrigger1);
		RB = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.RightTrigger1);

		Back = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.MiddleLeft);
		Guide = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.Middle);
		Start = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.MiddleRight);

		L3 = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.LeftThumb);
		R3 = Raylib.IsGamepadButtonDown(PlayerIndex, GamepadButton.RightThumb);

		LeftStickX = ApplyDeadzone(Raylib.GetGamepadAxisMovement(PlayerIndex, GamepadAxis.LeftX));
		LeftStickY = ApplyDeadzone(Raylib.GetGamepadAxisMovement(PlayerIndex, GamepadAxis.LeftY));
		RightStickX = ApplyDeadzone(Raylib.GetGamepadAxisMovement(PlayerIndex, GamepadAxis.RightX));
		RightStickY = ApplyDeadzone(Raylib.GetGamepadAxisMovement(PlayerIndex, GamepadAxis.RightY));

		LeftTrigger = Normalize01(Raylib.GetGamepadAxisMovement(PlayerIndex, GamepadAxis.LeftTrigger));
		RightTrigger = Normalize01(Raylib.GetGamepadAxisMovement(PlayerIndex, GamepadAxis.RightTrigger));

		APressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.RightFaceDown);
		BPressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.RightFaceRight);
		XPressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.RightFaceLeft);
		YPressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.RightFaceUp);
		StartPressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.MiddleRight);
		BackPressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.MiddleLeft);
		L3Pressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.LeftThumb);
		R3Pressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.RightThumb);
		DPadUpPressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.LeftFaceUp);
		DPadRightPressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.LeftFaceRight);
		DPadDownPressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.LeftFaceDown);
		DPadLeftPressed = Raylib.IsGamepadButtonPressed(PlayerIndex, GamepadButton.LeftFaceLeft);
	}

	private void Reset() {
		Name = string.Empty;
		A = B = X = Y = false;
		DPadUp = DPadRight = DPadDown = DPadLeft = false;
		LB = RB = false;
		Back = Guide = Start = false;
		L3 = R3 = false;
		LeftStickX = LeftStickY = RightStickX = RightStickY = 0f;
		LeftTrigger = RightTrigger = 0f;
		APressed = BPressed = XPressed = YPressed = StartPressed = BackPressed = false;
		L3Pressed = R3Pressed = false;
		DPadUpPressed = DPadRightPressed = DPadDownPressed = DPadLeftPressed = false;
	}

	private static float ApplyDeadzone(float value) {
		return Math.Abs(value) < StickDeadzone ? 0f : value;
	}

	private static float Normalize01(float rawAxisValue) {
		return Math.Clamp((rawAxisValue + 1f) / 2f, 0f, 1f);
	}
}
