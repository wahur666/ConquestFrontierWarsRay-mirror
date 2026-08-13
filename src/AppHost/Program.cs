using System;
using Raylib_cs;

namespace ConquestFrontierWarsRay;

internal static class Program
{
	static void Main()
	{
		Raylib.InitWindow(800, 450, "raylib-cs");

		var win32 = new Win32Window(onRedrawNeeded: Draw);

		while (!Raylib.WindowShouldClose())
		{
			Draw();
		}

		Raylib.CloseWindow();
	}

	static void Draw()
	{
		// Use elapsed time (not frame count) so motion stays smooth even
		// when frames are only ticking via the WM_TIMER during drag/resize.
		float t = (float)Raylib.GetTime();
		int screenW = Raylib.GetScreenWidth();
		int screenH = Raylib.GetScreenHeight();

		float x = screenW / 2f + MathF.Cos(t * 2f) * (screenW / 3f);
		float y = screenH / 2f + MathF.Sin(t * 3f) * (screenH / 3f);

		Raylib.BeginDrawing();
		Raylib.ClearBackground(Color.Black);
		Raylib.DrawText("Rendering while dragging!", 20, 20, 20, Color.DarkGray);
		Raylib.DrawCircle((int)x, (int)y, 25, Color.Maroon);
		Raylib.EndDrawing();
	}
}
