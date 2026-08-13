using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Tracks named input actions and a quit gesture.
/// </summary>
internal sealed class InputManager {
	private const int PrimaryGamepad = 0;
	private const float ExitHoldDurationSeconds = 1.25f;
	private readonly Dictionary<string, List<InputBinding>> _bindings = new(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> _currentStates = new(StringComparer.Ordinal);
	private readonly Dictionary<string, bool> _previousStates = new(StringComparer.Ordinal);
	private float _exitHoldElapsed;
	private bool _exitRequestedThisFrame;
	private bool _exitTriggeredDuringCurrentHold;

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
