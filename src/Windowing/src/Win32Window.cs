using System.Runtime.InteropServices;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Windowing;

/// <summary>
/// Keeps raylib responsive during the native move/resize modal loop on Windows.
/// The timer path is intentionally throttled and non-reentrant so dragging the
/// window does not flood the UI thread with nested draw calls.
/// </summary>
public sealed class Win32Window : IDisposable {
	private const uint WM_SETICON = 0x0080;
	private const int WM_ENTERSIZEMOVE = 0x0231;
	private const int WM_EXITSIZEMOVE = 0x0232;
	private const int WM_TIMER = 0x0113;
	private const int GWLP_WNDPROC = -4;
	private const int ICON_SMALL = 0;
	private const int ICON_BIG = 1;
	private const uint IMAGE_ICON = 1;
	private const uint LR_LOADFROMFILE = 0x0010;
	private const int SM_CXSMICON = 49;
	private const int SM_CYSMICON = 50;
	private const int SM_CXICON = 11;
	private const int SM_CYICON = 12;
	private const int TimerId = 1;
	private const uint TimerIntervalMs = 16;

	private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

	private readonly nint _hwnd;
	private readonly nint _originalProc;
	private readonly WndProcDelegate _newProcDelegate;
	private readonly Action _onTick;
	private bool _disposed;
	private bool _inSizeMove;
	private bool _isTicking;

	private Win32Window(nint hwnd, Action onTick) {
		_onTick = onTick ?? throw new ArgumentNullException(nameof(onTick));
		_hwnd = hwnd;
		_newProcDelegate = WindowProc;
		var newProcPtr = Marshal.GetFunctionPointerForDelegate(_newProcDelegate);
		_originalProc = SetWindowLongPtr(_hwnd, GWLP_WNDPROC, newProcPtr);
		if (_originalProc == nint.Zero) {
			throw new InvalidOperationException("Failed to subclass the raylib window.");
		}
	}

	/// <summary>
	/// Installs the modal move/resize workaround for the current raylib window when supported.
	/// </summary>
	public static Win32Window? TryInstall(Action onTick) {
		ArgumentNullException.ThrowIfNull(onTick);

		if (!OperatingSystem.IsWindows()) {
			return null;
		}

		unsafe {
			var hwnd = (nint)Raylib.GetWindowHandle();
			if (hwnd == nint.Zero) {
				return null;
			}

			return new Win32Window(hwnd, onTick);
		}
	}

	/// <summary>
	/// Loads a Windows .ico file and applies it to the current raylib window.
	/// </summary>
	public static void TrySetWindowIcon(string iconPath) {
		ArgumentException.ThrowIfNullOrWhiteSpace(iconPath);

		if (!OperatingSystem.IsWindows() || !File.Exists(iconPath)) {
			return;
		}

		unsafe {
			var hwnd = (nint)Raylib.GetWindowHandle();
			if (hwnd == nint.Zero) {
				return;
			}

			var smallIcon = LoadImage(
				nint.Zero,
				iconPath,
				IMAGE_ICON,
				GetSystemMetrics(SM_CXSMICON),
				GetSystemMetrics(SM_CYSMICON),
				LR_LOADFROMFILE);
			var largeIcon = LoadImage(
				nint.Zero,
				iconPath,
				IMAGE_ICON,
				GetSystemMetrics(SM_CXICON),
				GetSystemMetrics(SM_CYICON),
				LR_LOADFROMFILE);

			if (smallIcon != nint.Zero) {
				SendMessage(hwnd, WM_SETICON, ICON_SMALL, smallIcon);
			}

			if (largeIcon != nint.Zero) {
				SendMessage(hwnd, WM_SETICON, ICON_BIG, largeIcon);
			}
		}
	}

	public void Dispose() {
		if (_disposed) {
			return;
		}

		_disposed = true;
		_inSizeMove = false;
		KillTimer(_hwnd, TimerId);
		SetWindowLongPtr(_hwnd, GWLP_WNDPROC, _originalProc);
		GC.SuppressFinalize(this);
	}

	~Win32Window() {
		Dispose();
	}

	private nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam) {
		switch (msg) {
			case WM_ENTERSIZEMOVE:
				_inSizeMove = true;
				SetTimer(hWnd, TimerId, TimerIntervalMs, nint.Zero);
				break;
			case WM_EXITSIZEMOVE:
				_inSizeMove = false;
				KillTimer(hWnd, TimerId);
				break;
			case WM_TIMER when _inSizeMove && wParam == TimerId:
				TryTickDuringSizeMove();
				return 0;
		}

		return CallWindowProc(_originalProc, hWnd, msg, wParam, lParam);
	}

	private void TryTickDuringSizeMove() {
		if (_isTicking) {
			return;
		}

		_isTicking = true;
		try {
			_onTick();
		} finally {
			_isTicking = false;
		}
	}

	[DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
	private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

	[DllImport("user32.dll", EntryPoint = "CallWindowProcW")]
	private static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	private static extern nint SetTimer(nint hWnd, nint nIDEvent, uint uElapse, nint lpTimerFunc);

	[DllImport("user32.dll")]
	private static extern bool KillTimer(nint hWnd, nint uIDEvent);

	[DllImport("user32.dll", EntryPoint = "LoadImageW", CharSet = CharSet.Unicode)]
	private static extern nint LoadImage(nint hInst, string name, uint type, int cx, int cy, uint fuLoad);

	[DllImport("user32.dll")]
	private static extern int GetSystemMetrics(int nIndex);

	[DllImport("user32.dll", EntryPoint = "SendMessageW")]
	private static extern nint SendMessage(nint hWnd, uint msg, nint wParam, nint lParam);
}
