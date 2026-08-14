using System.Diagnostics;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.App;

/// <summary>
/// Keeps frames advancing while Windows enters the modal title-bar move or size loop.
/// </summary>
internal sealed class WindowsModalMoveLoopWorkaround : IDisposable {
	private const int GwlWndProc = -4;
	private const uint WmTimer = 0x0113;
	private const uint WmEnterSizeMove = 0x0231;
	private const uint WmExitSizeMove = 0x0232;
	private static readonly nint TimerId = 1;

	private readonly nint _windowHandle;
	private readonly Action<float> _runFrame;
	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
	private readonly WndProc _windowProc;
	private readonly nint _previousWindowProc;
	private bool _disposed;
	private bool _inModalMoveLoop;
	private long _lastTicks;

	private WindowsModalMoveLoopWorkaround(nint windowHandle, Action<float> runFrame) {
		_windowHandle = windowHandle;
		_runFrame = runFrame;
		_windowProc = WindowProc;
		_previousWindowProc = SetWindowLongPtr(_windowHandle, GwlWndProc, Marshal.GetFunctionPointerForDelegate(_windowProc));
		_lastTicks = _stopwatch.ElapsedTicks;
	}

	/// <summary>
	/// Installs the workaround for the current raylib window when supported.
	/// </summary>
	public static WindowsModalMoveLoopWorkaround? TryInstall(Action<float> runFrame) {
		ArgumentNullException.ThrowIfNull(runFrame);

		if (!OperatingSystem.IsWindows()) {
			return null;
		}

		unsafe {
			var windowHandle = (nint)Raylib.GetWindowHandle();
			if (windowHandle == 0) {
				return null;
			}

			return new WindowsModalMoveLoopWorkaround(windowHandle, runFrame);
		}
	}

	public void Dispose() {
		if (_disposed) {
			return;
		}

		KillTimer(_windowHandle, TimerId);
		SetWindowLongPtr(_windowHandle, GwlWndProc, _previousWindowProc);
		_disposed = true;
	}

	private nint WindowProc(nint windowHandle, uint message, nint wParam, nint lParam) {
		switch (message) {
			case WmEnterSizeMove:
				_inModalMoveLoop = true;
				_lastTicks = _stopwatch.ElapsedTicks;
				SetTimer(windowHandle, TimerId, 16, 0);
				break;

			case WmExitSizeMove:
				_inModalMoveLoop = false;
				KillTimer(windowHandle, TimerId);
				_lastTicks = _stopwatch.ElapsedTicks;
				break;

			case WmTimer when _inModalMoveLoop && wParam == TimerId:
				var now = _stopwatch.ElapsedTicks;
				var deltaTime = (now - _lastTicks) / (float)Stopwatch.Frequency;
				_lastTicks = now;
				_runFrame(MathF.Min(deltaTime, 0.05f));
				return 0;
		}

		return CallWindowProc(_previousWindowProc, windowHandle, message, wParam, lParam);
	}

	private delegate nint WndProc(nint windowHandle, uint message, nint wParam, nint lParam);

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
	private static extern nint SetWindowLongPtr(nint windowHandle, int index, nint newLong);

	[DllImport("user32.dll", EntryPoint = "CallWindowProcW", SetLastError = true)]
	private static extern nint CallWindowProc(nint previousWindowProc, nint windowHandle, uint message, nint wParam, nint lParam);

	[DllImport("user32.dll", SetLastError = true)]
	private static extern nint SetTimer(nint windowHandle, nint timerId, uint intervalMilliseconds, nint timerCallback);

	[DllImport("user32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool KillTimer(nint windowHandle, nint timerId);
}
