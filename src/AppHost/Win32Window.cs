using System;
using System.Runtime.InteropServices;

namespace ConquestFrontierWarsRay;

/// <summary>
/// Wraps the Win32 window-subclassing trick needed to keep raylib rendering
/// while the user is dragging or resizing the window (Windows blocks the
/// main thread with a modal loop during WM_ENTERSIZEMOVE otherwise).
/// </summary>
public class Win32Window
{
	private const int WM_ENTERSIZEMOVE = 0x0231;
	private const int WM_EXITSIZEMOVE = 0x0232;
	private const int WM_TIMER = 0x0113;
	private const int GWLP_WNDPROC = -4;
	private const int TIMER_ID = 1;

	private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	private readonly IntPtr hwnd;
	private readonly IntPtr originalProc;
	private readonly WndProcDelegate newProcDelegate;
	private readonly Action onRedrawNeeded;

	public Win32Window(Action onRedrawNeeded)
	{
		this.onRedrawNeeded = onRedrawNeeded;

		hwnd = GetForegroundWindow();

		newProcDelegate = WindowProc;
		IntPtr newProcPtr = Marshal.GetFunctionPointerForDelegate(newProcDelegate);
		originalProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, newProcPtr);
	}

	private IntPtr WindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
	{
		switch (msg)
		{
			case WM_ENTERSIZEMOVE:
				SetTimer(hWnd, (IntPtr)TIMER_ID, 1, IntPtr.Zero);
				break;
			case WM_EXITSIZEMOVE:
				KillTimer(hWnd, (IntPtr)TIMER_ID);
				break;
			case WM_TIMER:
				onRedrawNeeded();
				break;
		}

		return CallWindowProc(originalProc, hWnd, msg, wParam, lParam);
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

	[DllImport("user32.dll")]
	private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll")]
	private static extern bool SetTimer(IntPtr hWnd, IntPtr nIDEvent, uint uElapse, IntPtr lpTimerFunc);

	[DllImport("user32.dll")]
	private static extern bool KillTimer(IntPtr hWnd, IntPtr uIDEvent);
}
