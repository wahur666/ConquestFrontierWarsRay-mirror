using Raylib_cs;

namespace RaySharp;

internal static class VideoLibrary {
	internal static VideoPlayer? TryLoadStartupVideo(UiState ui) {
		string? path = ResolveDemoVideoPath();
		if (path is null) {
			return null;
		}

		try {
			return VideoPlayer.Open(path);
		} catch (Exception ex) {
			ui.Status = $"Could not load startup video: {ex.Message}";
			return null;
		}
	}

	internal static VideoPlayer? TryLoadDemoVideo(UiState ui) {
		string? path = ResolveDemoVideoPath();
		if (path is null) {
			return null;
		}

		try {
			VideoPlayer videoPlayer = VideoPlayer.Open(path);
			ui.Status = $"Loaded video: {videoPlayer.FileName} ({FormatFrameRate(videoPlayer.FrameRate)})";
			return videoPlayer;
		} catch (Exception ex) {
			ui.Status = $"Could not load video: {ex.Message}";
			return null;
		}
	}

	internal static string FormatFrameRate(double frameRate) {
		return frameRate > 0.0 ? $"{frameRate:0.##} FPS" : "unknown FPS";
	}

	private static string? ResolveDemoVideoPath() {
		// return AppAssets.ResolveAssetPath("Deadlands - Limbo.webm");
		return AppAssets.ResolveAssetPath("cq_intro.mp4");
	}
}
