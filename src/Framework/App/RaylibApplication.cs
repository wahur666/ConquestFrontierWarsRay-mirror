using Raylib_cs;
using ConquestFrontierWarsRay.Framework;
using System.Diagnostics;

namespace ConquestFrontierWarsRay.Framework.App;

/// <summary>
/// Owns the Raylib window and runs the main loop.
/// </summary>
public sealed class RaylibApplication : IDisposable {
	private readonly InputManager _input = new();
	private readonly Node _root;
	private readonly WindowOptions _windowOptions;
	private readonly Stopwatch _frameClock = new();
	private bool _disposed;
	private bool _isRunningFrame;
	private bool _shouldQuit;
	private long _lastFrameTicks;

	/// <summary>
	/// Creates the app runner with one root node.
	/// </summary>
	public RaylibApplication(WindowOptions windowOptions, Node root) {
		_windowOptions = windowOptions;
		_root = root ?? throw new ArgumentNullException(nameof(root));
	}

	/// <summary>
	/// Releases the root node and closes the window.
	/// </summary>
	public void Dispose() {
		if (_disposed) {
			return;
		}

		_root.Dispose();

		if (Raylib.IsWindowReady()) {
			Raylib.CloseWindow();
		}

		_disposed = true;
	}

	/// <summary>
	/// Starts the window, enters the node tree, and runs frames until quit.
	/// </summary>
	public void Run() {
		ThrowIfDisposed();

		Raylib.SetConfigFlags(_windowOptions.StartupFlags);
		Raylib.InitWindow(_windowOptions.Width, _windowOptions.Height, _windowOptions.Title);
		Raylib.SetTargetFPS(_windowOptions.TargetFps);
		Raylib.SetExitKey(KeyboardKey.Null);
		_input.SetupHotkeys();
		_root.AttachContextRecursive(new NodeContext(_input, SignalQuit));
		_root.InitializeRecursive();
		_root.EnterTreeRecursive();
		_frameClock.Restart();
		_lastFrameTicks = _frameClock.ElapsedTicks;
		using var modalMoveLoopWorkaround = WindowsModalMoveLoopWorkaround.TryInstall(RunFrame);

		try {
			while (true) {
				if (_shouldQuit || Raylib.WindowShouldClose()) {
					break;
				}

				RunFrame(GetDeltaTime());
			}
		} finally {
			_root.ExitTreeRecursive();
		}
	}

	private void ThrowIfDisposed() {
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	private void SignalQuit() {
		_shouldQuit = true;
	}

	private float GetDeltaTime() {
		var now = _frameClock.ElapsedTicks;
		var deltaTime = (now - _lastFrameTicks) / (float)Stopwatch.Frequency;
		_lastFrameTicks = now;
		return deltaTime;
	}

	private void RunFrame(float deltaTime) {
		if (_isRunningFrame || _shouldQuit || Raylib.WindowShouldClose()) {
			return;
		}

		_isRunningFrame = true;

		try {
			_input.Update(deltaTime);
			_root.UpdateRecursive(deltaTime);
			Raylib.BeginDrawing();
			_root.DrawRecursive();
			Raylib.EndDrawing();
		} finally {
			_isRunningFrame = false;
		}
	}
}
