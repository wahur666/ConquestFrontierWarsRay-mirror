using Raylib_cs;

namespace ConquestFrontierWarsRay.SceneReference.Input;

public class InputManager {
	private const float StickTriggerThreshold = 0.6f;
	private const float StickReleaseThreshold = 0.35f;
	private static bool _stickLatchUp;
	private static bool _stickLatchDown;
	private static bool _stickLatchLeft;
	private static bool _stickLatchRight;

	public static bool UiUp => GamepadInput.DPadUpPressed || Raylib.IsKeyPressed(KeyboardKey.Up) ||
	                           StickCrossed(GamepadInput.LeftStickY, true, ref _stickLatchUp);

	public static bool UiDown => GamepadInput.DPadDownPressed || Raylib.IsKeyPressed(KeyboardKey.Down) ||
	                             StickCrossed(GamepadInput.LeftStickY, false, ref _stickLatchDown);

	public static bool UiLeft => GamepadInput.DPadLeftPressed || Raylib.IsKeyPressed(KeyboardKey.Left) ||
	                             StickCrossed(GamepadInput.LeftStickX, true, ref _stickLatchLeft);

	public static bool UiRight => GamepadInput.DPadRightPressed || Raylib.IsKeyPressed(KeyboardKey.Right) ||
	                              StickCrossed(GamepadInput.LeftStickX, false, ref _stickLatchRight);

	public static bool UiEsc => Raylib.IsKeyPressed(KeyboardKey.Escape) || GamepadInput.BPressed;
	public static bool UiAccept => Raylib.IsKeyPressed(KeyboardKey.Enter) || GamepadInput.APressed;
	public static bool UiBack => Raylib.IsKeyPressed(KeyboardKey.Backspace) || GamepadInput.BackPressed;

	private static bool StickCrossed(float axisValue, bool negative, ref bool latch) {
		var magnitude = negative ? -axisValue : axisValue;

		if (magnitude > StickTriggerThreshold) {
			if (latch) {
				return false;
			}

			latch = true;
			return true;
		}

		if (magnitude < StickReleaseThreshold) {
			latch = false;
		}

		return false;
	}
}
