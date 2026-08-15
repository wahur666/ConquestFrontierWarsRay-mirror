using Raylib_cs;
using System.Diagnostics;
using ConquestFrontierWarsRay.Framework;
using ConquestFrontierWarsRay.Framework.App;

namespace ConquestFrontierWarsRay.Windowing;

/// <summary>
/// Owns the Raylib window and runs the main loop.
/// </summary>
public sealed class RaylibApplication : IDisposable {
	private readonly SceneTree _sceneTree;
	private readonly WindowOptions _windowOptions;
	private readonly Stopwatch _frameClock = new();
	private bool _disposed;
	private bool _isRunningFrame;
	private long _lastFrameTicks;

	/// <summary>
	/// Creates the app runner with one root node.
	/// </summary>
	public RaylibApplication(WindowOptions windowOptions, Node root)
		: this(windowOptions, new SceneTree(root, new InputManager())) {
	}

	/// <summary>
	/// Creates the app runner with an existing scene tree.
	/// </summary>
	public RaylibApplication(WindowOptions windowOptions, SceneTree sceneTree) {
		_windowOptions = windowOptions;
		_sceneTree = sceneTree ?? throw new ArgumentNullException(nameof(sceneTree));
	}

	/// <summary>
	/// Releases the root node and closes the window.
	/// </summary>
	public void Dispose() {
		if (_disposed) {
			return;
		}

		_sceneTree.Dispose();

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
		_sceneTree.Input.SetupHotkeys();
		_sceneTree.Start();
		_frameClock.Restart();
		_lastFrameTicks = _frameClock.ElapsedTicks;
		using var modalMoveLoopWorkaround = Win32Window.TryInstall(() => RunFrame(GetDeltaTime()));

		try {
			while (true) {
				if (_sceneTree.IsQuitRequested || Raylib.WindowShouldClose()) {
					break;
				}

				RunFrame(GetDeltaTime());
			}
		} finally {
			_sceneTree.Stop();
		}
	}

	private void ThrowIfDisposed() {
		ObjectDisposedException.ThrowIf(_disposed, this);
	}

	private float GetDeltaTime() {
		var now = _frameClock.ElapsedTicks;
		var deltaTime = (now - _lastFrameTicks) / (float)Stopwatch.Frequency;
		_lastFrameTicks = now;
		return deltaTime;
	}

	private void RunFrame(float deltaTime) {
		if (_isRunningFrame || _sceneTree.IsQuitRequested || Raylib.WindowShouldClose()) {
			return;
		}

		_isRunningFrame = true;

		try {
			_sceneTree.Input.Update(deltaTime);
			_sceneTree.Update(deltaTime);
			Raylib.BeginDrawing();
			_sceneTree.Draw();
			Raylib.EndDrawing();
		} finally {
			_isRunningFrame = false;
		}
	}
}
