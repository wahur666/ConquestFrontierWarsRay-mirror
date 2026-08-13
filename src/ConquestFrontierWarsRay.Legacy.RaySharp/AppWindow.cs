using Raylib_cs;

namespace RaySharp;

internal static class AppWindow {
	internal static void ApplyStartupOptions(WindowState windowState, UiState ui, bool startFullScreen) {
		if (!startFullScreen) {
			return;
		}

		windowState.ConfineMouse = true;

		int monitor = Raylib.GetCurrentMonitor();
		Raylib.SetWindowSize(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
		Raylib.ToggleFullscreen();
		ui.Status = "Fullscreen enabled from --full-screen. Press F11 to return to windowed mode, F8 to release mouse.";
	}

	internal static void HandleShortcuts(WindowState windowState, UiState ui) {
		if (Raylib.IsKeyPressed(KeyboardKey.F8)) {
			windowState.ConfineMouse = !windowState.ConfineMouse;
			if (!windowState.ConfineMouse) {
				ReleaseMouseConfinement();
			}

			ui.Status = windowState.ConfineMouse
				? "Mouse confinement enabled. Press F8 to release it."
				: "Mouse confinement disabled. Press F8 to confine it.";
		}

		if (Raylib.IsKeyPressed(KeyboardKey.F9)) {
			ui.ShowTestSphereWire = !ui.ShowTestSphereWire;
			ui.Status = ui.ShowTestSphereWire
				? "Model wire overlay enabled. Press F9 to hide it."
				: "Model wire overlay hidden. Press F9 to show it.";
		}

		if (!Raylib.IsKeyPressed(KeyboardKey.F11)) {
			return;
		}

		if (!Raylib.IsWindowFullscreen()) {
			windowState.WindowedWidth = Raylib.GetScreenWidth();
			windowState.WindowedHeight = Raylib.GetScreenHeight();

			int monitor = Raylib.GetCurrentMonitor();
			Raylib.SetWindowSize(Raylib.GetMonitorWidth(monitor), Raylib.GetMonitorHeight(monitor));
			Raylib.ToggleFullscreen();
			ui.Status = "Fullscreen enabled. Press F11 to return to windowed mode.";
			return;
		}

		Raylib.ToggleFullscreen();
		Raylib.SetWindowSize(windowState.WindowedWidth, windowState.WindowedHeight);
		ui.Status = "Windowed mode enabled. Press F11 for fullscreen.";
	}

	internal static void UpdateMouseConfinement(WindowState windowState) {
		if (!windowState.ConfineMouse || !Raylib.IsWindowFocused()) {
			ReleaseMouseConfinement();
			return;
		}

		nint windowHandle = Win32.GetForegroundWindow();
		if (windowHandle == 0 || !Win32.GetClientRect(windowHandle, out Win32.NativeRect clientRect)) {
			ReleaseMouseConfinement();
			return;
		}

		Win32.NativePoint topLeft = new() { X = clientRect.Left, Y = clientRect.Top };
		Win32.NativePoint bottomRight = new() { X = clientRect.Right, Y = clientRect.Bottom };
		if (!Win32.ClientToScreen(windowHandle, ref topLeft) || !Win32.ClientToScreen(windowHandle, ref bottomRight)) {
			ReleaseMouseConfinement();
			return;
		}

		Win32.NativeRect clipRect = new() {
			Left = topLeft.X,
			Top = topLeft.Y,
			Right = bottomRight.X,
			Bottom = bottomRight.Y
		};

		Win32.ClipCursor(ref clipRect);
	}

	internal static void ReleaseMouseConfinement() {
		Win32.ClipCursor(0);
	}
}