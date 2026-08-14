using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Tracks named input actions and a quit gesture.
/// </summary>
public sealed class InputManager {
	private const float StickTriggerThreshold = 0.6f;
	private const float StickReleaseThreshold = 0.35f;
	private const int PrimaryGamepad = 0;
	private const float ExitHoldDurationSeconds = 1.25f;
	private readonly Dictionary<string, List<InputBinding>> _bindings = new(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> _currentStates = new(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> _previousStates = new(StringComparer.Ordinal);
	private readonly GamepadState _gamepad = new();
	private float _exitHoldElapsed;
	private bool _exitRequestedThisFrame;
	private bool _exitTriggeredDuringCurrentHold;
	private bool _stickLatchUp;
	private bool _stickLatchDown;
	private bool _stickLatchLeft;
	private bool _stickLatchRight;

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
	public bool UiUp => _gamepad.DPadUpPressed || Raylib.IsKeyPressed(KeyboardKey.Up) ||
	                    StickCrossed(_gamepad.LeftStickY, true, ref _stickLatchUp);

	/// <summary>
	/// Returns true on the frame UI down navigation is triggered.
	/// </summary>
	public bool UiDown => _gamepad.DPadDownPressed || Raylib.IsKeyPressed(KeyboardKey.Down) ||
	                      StickCrossed(_gamepad.LeftStickY, false, ref _stickLatchDown);

	/// <summary>
	/// Returns true on the frame UI left navigation is triggered.
	/// </summary>
	public bool UiLeft => _gamepad.DPadLeftPressed || Raylib.IsKeyPressed(KeyboardKey.Left) ||
	                      StickCrossed(_gamepad.LeftStickX, true, ref _stickLatchLeft);

	/// <summary>
	/// Returns true on the frame UI right navigation is triggered.
	/// </summary>
	public bool UiRight => _gamepad.DPadRightPressed || Raylib.IsKeyPressed(KeyboardKey.Right) ||
	                       StickCrossed(_gamepad.LeftStickX, false, ref _stickLatchRight);

	/// <summary>
	/// Returns true on the frame the primary cancel key is pressed.
	/// </summary>
	public bool UiEsc => Raylib.IsKeyPressed(KeyboardKey.Escape) || _gamepad.BPressed;

	/// <summary>
	/// Returns true on the frame the primary accept key is pressed.
	/// </summary>
	public bool UiAccept => Raylib.IsKeyPressed(KeyboardKey.Enter) || _gamepad.APressed;

	/// <summary>
	/// Returns true on the frame the secondary back key is pressed.
	/// </summary>
	public bool UiBack => Raylib.IsKeyPressed(KeyboardKey.Backspace) || _gamepad.BackPressed;

	private void Register(string actionName, params KeyboardKey[] keys) {
		ArgumentException.ThrowIfNullOrWhiteSpace(actionName);
		ArgumentNullException.ThrowIfNull(keys);

		if (keys.Length == 0) {
			throw new ArgumentException("At least one key is required for an input binding.", nameof(keys));
		}

		if (!_bindings.TryGetValue(actionName, out var bindings)) {
			bindings = [];
			_bindings[actionName] = bindings;
			_currentStates[actionName] = false;
			_previousStates[actionName] = false;
		}

		bindings.Add(new InputBinding(keys));
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
			var currentState = bindings.Any(binding => binding.IsDown());

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

	private sealed class InputBinding {
		private readonly KeyboardKey[] _keys;

		public InputBinding(KeyboardKey[] keys) {
			_keys = [.. keys];
		}

		public bool IsDown() {
			foreach (var key in _keys) {
				if (!Raylib.IsKeyDown(key)) {
					return false;
				}
			}

			return true;
		}
	}

	/// <summary>
	/// Registers the default input actions used by the app.
	/// </summary>
	public void SetupHotkeys() {
		Register("UiEscape", KeyboardKey.Escape);
	}
}
