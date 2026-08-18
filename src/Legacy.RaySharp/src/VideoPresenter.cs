using System.Numerics;
using Raylib_cs;

namespace RaySharp;

internal static class VideoPresenter {
	internal static void DrawStartupVideo(VideoPlayer videoPlayer) {
		Rectangle destination = FitRectangle(
			videoPlayer.Width,
			videoPlayer.Height,
			Raylib.GetScreenWidth(),
			Raylib.GetScreenHeight());

		videoPlayer.Draw(destination, Color.White);
	}

	internal static Rectangle FitRectangle(float sourceWidth, float sourceHeight, float targetWidth, float targetHeight) {
		if (sourceWidth <= 0.0f || sourceHeight <= 0.0f || targetWidth <= 0.0f || targetHeight <= 0.0f) {
			return new Rectangle(0.0f, 0.0f, targetWidth, targetHeight);
		}

		float scale = MathF.Min(targetWidth / sourceWidth, targetHeight / sourceHeight);
		float width = sourceWidth * scale;
		float height = sourceHeight * scale;
		return new Rectangle((targetWidth - width) * 0.5f, (targetHeight - height) * 0.5f, width, height);
	}
}