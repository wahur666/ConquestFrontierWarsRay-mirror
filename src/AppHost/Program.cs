using System;
using System.IO;
using System.Linq;
using Raylib_cs;

namespace ConquestFrontierWarsRay;

internal static class Program
{
	private static Music currentTrack;

	static void Main()
	{
		Raylib.InitWindow(800, 450, "raylib-cs");
		Raylib.InitAudioDevice();

		string ostDirectory = Path.Combine(AppContext.BaseDirectory, "conquest_frontier_wars_ost");
		string firstTrackPath = Directory
			.EnumerateFiles(ostDirectory, "*.mp3", SearchOption.TopDirectoryOnly)
			.OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
			.First();
		currentTrack = Raylib.LoadMusicStream(firstTrackPath);
		Raylib.SetMusicVolume(currentTrack, 0.5f);
		Raylib.PlayMusicStream(currentTrack);

		var win32 = new Win32Window(onTick: Tick);

		while (!Raylib.WindowShouldClose())
		{
			Tick();
		}

		Raylib.StopMusicStream(currentTrack);
		Raylib.UnloadMusicStream(currentTrack);
		Raylib.CloseAudioDevice();
		Raylib.CloseWindow();
	}

	static void Tick()
	{
		Raylib.UpdateMusicStream(currentTrack);
		Draw();
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
		Raylib.DrawText("Playing first OST track at 50% volume", 20, 48, 20, Color.DarkGray);
		Raylib.DrawCircle((int)x, (int)y, 25, Color.Maroon);
		Raylib.EndDrawing();
	}
}
