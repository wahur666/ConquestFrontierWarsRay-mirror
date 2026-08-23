using Raylib_cs;
using System.Diagnostics;
using ConquestFrontierWarsRay.Framework;
using ConquestFrontierWarsRay.Framework.App;

namespace ConquestFrontierWarsRay.Windowing;

/// <summary>
/// Owns the Raylib window and runs the main loop.
/// </summary>
public sealed class RaylibApplication : IDisposable {
	private readonly WindowOptions _windowOptions;
	private readonly Stopwatch _frameClock = new();
	private SceneTree? _sceneTree;
	private bool _bootstrapped;
	private bool _disposed;
	private bool _isRunningFrame;
	private long _lastFrameTicks;

	/// <summary>
	/// Creates the app runner with one set of window options.
	/// </summary>
	public RaylibApplication(WindowOptions windowOptions) {
		_windowOptions = windowOptions;
	}

	/// <summary>
	/// Releases the root node and closes the window.
	/// </summary>
	public void Dispose() {
		if (_disposed) {
			return;
		}

		_sceneTree?.Dispose();
		_sceneTree = null;

		if (Raylib.IsAudioDeviceReady()) {
			Raylib.CloseAudioDevice();
		}

		if (Raylib.IsWindowReady()) {
			Raylib.CloseWindow();
		}

		_disposed = true;
	}

	/// <summary>
	/// Initializes the Raylib window, audio device, and process-wide input state.
	/// </summary>
	public void Bootstrap() {
		ThrowIfDisposed();

		if (_bootstrapped) {
			return;
		}

		Raylib.SetConfigFlags(_windowOptions.StartupFlags);
		Raylib.InitWindow(_windowOptions.Width, _windowOptions.Height, _windowOptions.Title);

		if (!Raylib.IsAudioDeviceReady()) {
			Raylib.InitAudioDevice();
		}

		Raylib.SetTargetFPS(_windowOptions.TargetFps);
		Raylib.SetExitKey(KeyboardKey.Null);
		_bootstrapped = true;
	}

	/// <summary>
	/// Starts the supplied scene tree and runs frames until quit.
	/// </summary>
	public void Run(SceneTree sceneTree) {
		ThrowIfDisposed();
		ArgumentNullException.ThrowIfNull(sceneTree);

		Bootstrap();

		if (_sceneTree is not null) {
			throw new InvalidOperationException("RaylibApplication already has an active scene tree.");
		}

		_sceneTree = sceneTree;
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
			_sceneTree.Dispose();
			_sceneTree = null;
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
		if (_sceneTree is null || _isRunningFrame || _sceneTree.IsQuitRequested || Raylib.WindowShouldClose()) {
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
