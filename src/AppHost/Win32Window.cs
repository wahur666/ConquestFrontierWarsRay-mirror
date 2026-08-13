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

	private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

	private readonly nint hwnd;
	private readonly nint originalProc;
	private readonly WndProcDelegate newProcDelegate;
	private readonly Action onTick;

	public Win32Window(Action onTick)
	{
		this.onTick = onTick;

		hwnd = GetForegroundWindow();

		newProcDelegate = WindowProc;
		nint newProcPtr = Marshal.GetFunctionPointerForDelegate(newProcDelegate);
		originalProc = SetWindowLongPtr(hwnd, GWLP_WNDPROC, newProcPtr);
	}

	private nint WindowProc(nint hWnd, uint msg, nint wParam, nint lParam)
	{
		switch (msg)
		{
			case WM_ENTERSIZEMOVE:
				SetTimer(hWnd, TIMER_ID, 1, nint.Zero);
				break;
			case WM_EXITSIZEMOVE:
				KillTimer(hWnd, TIMER_ID);
				break;
			case WM_TIMER:
				onTick();
				break;
		}

		return CallWindowProc(originalProc, hWnd, msg, wParam, lParam);
	}

	[DllImport("user32.dll")]
	private static extern nint GetForegroundWindow();

	[DllImport("user32.dll")]
	private static extern nint SetWindowLongPtr(nint hWnd, int nIndex, nint dwNewLong);

	[DllImport("user32.dll")]
	private static extern nint CallWindowProc(nint lpPrevWndFunc, nint hWnd, uint msg, nint wParam, nint lParam);

	[DllImport("user32.dll")]
	private static extern bool SetTimer(nint hWnd, nint nIDEvent, uint uElapse, nint lpTimerFunc);

	[DllImport("user32.dll")]
	private static extern bool KillTimer(nint hWnd, nint uIDEvent);
}
