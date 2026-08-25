using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Tracks named input actions and a quit gesture.
/// </summary>
public sealed class InputManager {
	private const float StickTriggerThreshold = 0.6f;
	private const int PrimaryGamepad = 0;
	private const float ExitHoldDurationSeconds = 1.25f;
	private readonly Dictionary<string, List<InputBinding>> _bindings = new(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> _currentStates = new(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> _previousStates = new(StringComparer.Ordinal);
	private readonly GamepadState _gamepad = new();
	private float _exitHoldElapsed;
	private bool _exitRequestedThisFrame;
	private bool _exitTriggeredDuringCurrentHold;

	/// <summary>
	/// Standard action name for UI up navigation.
	/// </summary>
	public const string UiUpAction = "UiUp";

	/// <summary>
	/// Standard action name for UI down navigation.
	/// </summary>
	public const string UiDownAction = "UiDown";

	/// <summary>
	/// Standard action name for UI left navigation.
	/// </summary>
	public const string UiLeftAction = "UiLeft";

	/// <summary>
	/// Standard action name for UI right navigation.
	/// </summary>
	public const string UiRightAction = "UiRight";

	/// <summary>
	/// Standard action name for UI accept/confirm.
	/// </summary>
	public const string UiAcceptAction = "UiAccept";

	/// <summary>
	/// Standard action name for UI cancel/escape.
	/// </summary>
	public const string UiEscapeAction = "UiEscape";

	/// <summary>
	/// Standard action name for UI back.
	/// </summary>
	public const string UiBackAction = "UiBack";

	/// <summary>
	/// Raised every frame while an action is down.
	/// </summary>
	public event Action<string>? ActionJustPressed;

	/// <summary>
	/// Raised on the frame an action is released.
	/// </summary>
	public event Action<string>? ActionJustReleased;

	/// <summary>
	/// Raised every frame while an action is down.
	/// </summary>
	public event Action<string>? ActionPressed;

	/// <summary>
	/// Raised every frame while an action is up.
	/// </summary>
	public event Action<string>? ActionReleased;

	/// <summary>
	/// Polled state for the primary gamepad.
	/// </summary>
	public GamepadState Gamepad => _gamepad;

	/// <summary>
	/// Returns true on the frame UI up navigation is triggered.
	/// </summary>
	public bool UiUp => IsActionJustPressed(UiUpAction);

	/// <summary>
	/// Returns true on the frame UI down navigation is triggered.
	/// </summary>
	public bool UiDown => IsActionJustPressed(UiDownAction);

	/// <summary>
	/// Returns true on the frame UI left navigation is triggered.
	/// </summary>
	public bool UiLeft => IsActionJustPressed(UiLeftAction);

	/// <summary>
	/// Returns true on the frame UI right navigation is triggered.
	/// </summary>
	public bool UiRight => IsActionJustPressed(UiRightAction);

	/// <summary>
	/// Returns true on the frame the primary cancel key is pressed.
	/// </summary>
	public bool UiEsc => IsActionJustPressed(UiEscapeAction);

	/// <summary>
	/// Returns true on the frame the primary accept key is pressed.
	/// </summary>
	public bool UiAccept => IsActionJustPressed(UiAcceptAction);

	/// <summary>
	/// Returns true on the frame the secondary back key is pressed.
	/// </summary>
	public bool UiBack => IsActionJustPressed(UiBackAction);

	/// <summary>
	/// Registers one keyboard chord for an action. Calling this again adds another binding to the same action.
	/// </summary>
	public void RegisterAction(string actionName, params KeyboardKey[] keys) {
		ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
		ArgumentNullException.ThrowIfNull(keys);

		if (keys.Length == 0) {
			throw new ArgumentException("At least one key is required for an input binding.", nameof(keys));
		}

		RegisterBinding(actionName, new KeyboardChordBinding(keys));
	}

	/// <summary>
	/// Registers one gamepad button binding for an action. Calling this again adds another binding to the same action.
	/// </summary>
	public void RegisterAction(string actionName, GamepadButton button) {
		ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

		RegisterBinding(actionName, new GamepadButtonBinding(button));
	}

	/// <summary>
	/// Registers one gamepad axis-direction binding for an action. Calling this again adds another binding to the same action.
	/// </summary>
	public void RegisterAction(string actionName, GamepadAxis axis, InputAxisDirection direction, float triggerThreshold = StickTriggerThreshold) {
		ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

		if (triggerThreshold <= 0f || triggerThreshold > 1f) {
			throw new ArgumentOutOfRangeException(nameof(triggerThreshold), triggerThreshold, "Trigger threshold must be within (0, 1].");
		}

		RegisterBinding(actionName, new GamepadAxisBinding(axis, direction, triggerThreshold));
	}

	/// <summary>
	/// Returns true only on the frame the action becomes pressed.
	/// </summary>
	public bool IsActionJustPressed(string actionName) {
		ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

		return GetActionState(_currentStates, actionName) && !GetActionState(_previousStates, actionName);
	}

	/// <summary>
	/// Returns true only on the frame the action becomes released.
	/// </summary>
	public bool IsActionJustReleased(string actionName) {
		ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

		return !GetActionState(_currentStates, actionName) && GetActionState(_previousStates, actionName);
	}

	/// <summary>
	/// Returns true while the action is held.
	/// </summary>
	public bool IsActionPressed(string actionName) {
		ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

		return GetActionState(_currentStates, actionName);
	}

	/// <summary>
	/// Returns true while the action is not held.
	/// </summary>
	public bool IsActionReleased(string actionName) {
		ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

		return !GetActionState(_currentStates, actionName);
	}

	/// <summary>
	/// Returns true on the frame the quit gesture finishes.
	/// </summary>
	public bool IsExitRequested() {
		return _exitRequestedThisFrame;
	}

	/// <summary>
	/// Updates all action states for the current frame.
	/// </summary>
	public void Update(float deltaTime) {
		_exitRequestedThisFrame = false;
		_gamepad.Poll();

		foreach (var (actionName, bindings) in _bindings) {
			var previousState = _currentStates[actionName];
			var currentState = bindings.Any(binding => binding.IsDown(_gamepad));

			_previousStates[actionName] = previousState;
			_currentStates[actionName] = currentState;

			if (currentState) {
				ActionPressed?.Invoke(actionName);
			} else {
				ActionReleased?.Invoke(actionName);
			}

			if (currentState && !previousState) {
				ActionJustPressed?.Invoke(actionName);
			} else if (!currentState && previousState) {
				ActionJustReleased?.Invoke(actionName);
			}
		}

		UpdateExitGesture(deltaTime);
	}

	/// <summary>
	/// Samples the current frame into a single UI input snapshot.
	/// </summary>
	public UiInputSnapshot CaptureUiSnapshot() {
		return new UiInputSnapshot(
			PointerPosition: Raylib.GetMousePosition(),
			LeftPressed: Raylib.IsMouseButtonPressed(MouseButton.Left),
			LeftDown: Raylib.IsMouseButtonDown(MouseButton.Left),
			LeftReleased: Raylib.IsMouseButtonReleased(MouseButton.Left),
			MiddlePressed: Raylib.IsMouseButtonPressed(MouseButton.Middle),
			MiddleDown: Raylib.IsMouseButtonDown(MouseButton.Middle),
			MiddleReleased: Raylib.IsMouseButtonReleased(MouseButton.Middle),
			RightPressed: Raylib.IsMouseButtonPressed(MouseButton.Right),
			RightDown: Raylib.IsMouseButtonDown(MouseButton.Right),
			RightReleased: Raylib.IsMouseButtonReleased(MouseButton.Right),
			WheelDelta: Raylib.GetMouseWheelMove(),
			NavigateUp: IsActionJustPressedOrDefault(UiUpAction),
			NavigateDown: IsActionJustPressedOrDefault(UiDownAction),
			NavigateLeft: IsActionJustPressedOrDefault(UiLeftAction),
			NavigateRight: IsActionJustPressedOrDefault(UiRightAction),
			AcceptPressed: IsActionJustPressedOrDefault(UiAcceptAction),
			BackPressed: IsActionJustPressedOrDefault(UiBackAction),
			EscapePressed: IsActionJustPressedOrDefault(UiEscapeAction));
	}

	private void UpdateExitGesture(float deltaTime) {
		if (!Raylib.IsGamepadAvailable(PrimaryGamepad)) {
			_exitHoldElapsed = 0f;
			_exitTriggeredDuringCurrentHold = false;
			return;
		}

		var isBackHeld = Raylib.IsGamepadButtonDown(PrimaryGamepad, GamepadButton.MiddleLeft);
		var isStartHeld = Raylib.IsGamepadButtonDown(PrimaryGamepad, GamepadButton.MiddleRight);

		if (!isBackHeld || !isStartHeld) {
			_exitHoldElapsed = 0f;
			_exitTriggeredDuringCurrentHold = false;
			return;
		}

		_exitHoldElapsed += deltaTime;

		if (_exitTriggeredDuringCurrentHold || _exitHoldElapsed < ExitHoldDurationSeconds) {
			return;
		}

		_exitRequestedThisFrame = true;
		_exitTriggeredDuringCurrentHold = true;
	}

	private bool GetActionState(Dictionary<string, bool> states, string actionName) {
		if (!_bindings.ContainsKey(actionName)) {
			throw new KeyNotFoundException($"Input action '{actionName}' is not registered.");
		}

		return states[actionName];
	}

	private bool IsActionJustPressedOrDefault(string actionName) {
		return _bindings.ContainsKey(actionName) && IsActionJustPressed(actionName);
	}

	private void RegisterBinding(string actionName, InputBinding binding) {
		ArgumentNullException.ThrowIfNull(binding);

		if (!_bindings.TryGetValue(actionName, out var bindings)) {
			bindings = [];
			_bindings[actionName] = bindings;
			_currentStates[actionName] = false;
			_previousStates[actionName] = false;
		}

		bindings.Add(binding);
	}

	private abstract class InputBinding {
		public abstract bool IsDown(GamepadState gamepad);
	}

	private sealed class KeyboardChordBinding : InputBinding {
		private readonly KeyboardKey[] _keys;

		public KeyboardChordBinding(KeyboardKey[] keys) {
			_keys = [.. keys];
		}

		public override bool IsDown(GamepadState gamepad) {
			foreach (var key in _keys) {
				if (!Raylib.IsKeyDown(key)) {
					return false;
				}
			}

			return true;
		}
	}

	private sealed class GamepadButtonBinding : InputBinding {
		private readonly GamepadButton _button;

		public GamepadButtonBinding(GamepadButton button) {
			_button = button;
		}

		public override bool IsDown(GamepadState gamepad) {
			return _button switch {
				GamepadButton.LeftFaceUp => gamepad.DPadUp,
				GamepadButton.LeftFaceRight => gamepad.DPadRight,
				GamepadButton.LeftFaceDown => gamepad.DPadDown,
				GamepadButton.LeftFaceLeft => gamepad.DPadLeft,
				GamepadButton.RightFaceUp => gamepad.Y,
				GamepadButton.RightFaceRight => gamepad.B,
				GamepadButton.RightFaceDown => gamepad.A,
				GamepadButton.RightFaceLeft => gamepad.X,
				GamepadButton.LeftTrigger1 => gamepad.LB,
				GamepadButton.RightTrigger1 => gamepad.RB,
				GamepadButton.MiddleLeft => gamepad.Back,
				GamepadButton.Middle => gamepad.Guide,
				GamepadButton.MiddleRight => gamepad.Start,
				GamepadButton.LeftThumb => gamepad.L3,
				GamepadButton.RightThumb => gamepad.R3,
				_ => Raylib.IsGamepadButtonDown(PrimaryGamepad, _button)
			};
		}
	}

	private sealed class GamepadAxisBinding : InputBinding {
		private readonly GamepadAxis _axis;
		private readonly InputAxisDirection _direction;
		private readonly float _triggerThreshold;

		public GamepadAxisBinding(GamepadAxis axis, InputAxisDirection direction, float triggerThreshold) {
			_axis = axis;
			_direction = direction;
			_triggerThreshold = triggerThreshold;
		}

		public override bool IsDown(GamepadState gamepad) {
			var axisValue = _axis switch {
				GamepadAxis.LeftX => gamepad.LeftStickX,
				GamepadAxis.LeftY => gamepad.LeftStickY,
				GamepadAxis.RightX => gamepad.RightStickX,
				GamepadAxis.RightY => gamepad.RightStickY,
				GamepadAxis.LeftTrigger => gamepad.LeftTrigger,
				GamepadAxis.RightTrigger => gamepad.RightTrigger,
				_ => Raylib.GetGamepadAxisMovement(PrimaryGamepad, _axis)
			};

			return _direction switch {
				InputAxisDirection.Positive => axisValue >= _triggerThreshold,
				InputAxisDirection.Negative => axisValue <= -_triggerThreshold,
				_ => false
			};
		}
	}

	/// <summary>
	/// Registers the default input actions used by the app.
	/// </summary>
	public void SetupHotkeys() {
		RegisterAction(UiUpAction, KeyboardKey.Up);
		RegisterAction(UiUpAction, GamepadButton.LeftFaceUp);
		RegisterAction(UiUpAction, GamepadAxis.LeftY, InputAxisDirection.Negative);

		RegisterAction(UiDownAction, KeyboardKey.Down);
		RegisterAction(UiDownAction, GamepadButton.LeftFaceDown);
		RegisterAction(UiDownAction, GamepadAxis.LeftY, InputAxisDirection.Positive);

		RegisterAction(UiLeftAction, KeyboardKey.Left);
		RegisterAction(UiLeftAction, GamepadButton.LeftFaceLeft);
		RegisterAction(UiLeftAction, GamepadAxis.LeftX, InputAxisDirection.Negative);

		RegisterAction(UiRightAction, KeyboardKey.Right);
		RegisterAction(UiRightAction, GamepadButton.LeftFaceRight);
		RegisterAction(UiRightAction, GamepadAxis.LeftX, InputAxisDirection.Positive);

		RegisterAction(UiAcceptAction, KeyboardKey.Enter);
		RegisterAction(UiAcceptAction, GamepadButton.RightFaceDown);

		RegisterAction(UiEscapeAction, KeyboardKey.Escape);
		RegisterAction(UiEscapeAction, GamepadButton.RightFaceRight);

		RegisterAction(UiBackAction, KeyboardKey.F12);
		RegisterAction(UiBackAction, GamepadButton.MiddleLeft);
	}
}
