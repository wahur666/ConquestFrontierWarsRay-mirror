using System.Runtime.InteropServices;
using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.Input;

/// <summary>
///     Polls gamepad 0 once per frame and exposes it with Xbox-controller naming, since that's
///     the layout the target handheld's built-in controller follows (minus L3/R3 on some units).
///     Raylib's generic button/axis names are mapped here: RightFace* are ABXY, LeftFace* are the
///     d-pad, Trigger1 are the bumpers (LB/RB), Trigger2 are the analog triggers (LT/RT).
/// </summary>
public static class GamepadInput {
	public const int PlayerIndex = 0;
	private const float StickDeadzone = 0.15f;

	public static bool IsConnected { get; private set; }
	public static string Name { get; private set; } = string.Empty;

	public static bool A { get; private set; }
	public static bool B { get; private set; }
	public static bool X { get; private set; }
	public static bool Y { get; private set; }

	public static bool DPadUp { get; private set; }
	public static bool DPadRight { get; private set; }
	public static bool DPadDown { get; private set; }
	public static bool DPadLeft { get; private set; }

	public static bool LB { get; private set; }
	public static bool RB { get; private set; }

	public static bool Back { get; private set; }
	public static bool Guide { get; private set; }
	public static bool Start { get; private set; }

	/// <summary>Left stick click. Device may be missing this button entirely.</summary>
	public static bool L3 { get; private set; }

	/// <summary>Right stick click. Device may be missing this button entirely.</summary>
	public static bool R3 { get; private set; }

	public static float LeftStickX { get; private set; }
	public static float LeftStickY { get; private set; }
	public static float RightStickX { get; private set; }
	public static float RightStickY { get; private set; }

	/// <summary>0 (released) .. 1 (fully pressed).</summary>
	public static float LeftTrigger { get; private set; }

	/// <summary>0 (released) .. 1 (fully pressed).</summary>
	public static float RightTrigger { get; private set; }

	// "Just pressed this frame" edge triggers, for menu navigation and one-shot actions.
	public static bool APressed { get; private set; }
	public static bool BPressed { get; private set; }
	public static bool XPressed { get; private set; }
	public static bool YPressed { get; private set; }
	public static bool StartPressed { get; private set; }
	public static bool BackPressed { get; private set; }
	public static bool L3Pressed { get; private set; }
	public static bool R3Pressed { get; private set; }
	public static bool DPadUpPressed { get; private set; }
	public static bool DPadRightPressed { get; private set; }
	public static bool DPadDownPressed { get; private set; }
	public static bool DPadLeftPressed { get; private set; }

	public static void Poll() {
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

		// Trigger axes report -1 (released) .. 1 (fully pressed); normalize to 0..1.
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

	private static void Reset() {
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
