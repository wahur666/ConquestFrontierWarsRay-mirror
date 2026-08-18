using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class VideoPlayerTests {
	[Fact]
	public void FitRectanglePreservesAspectRatioWithinBounds() {
		var rect = VideoPlayer.FitRectangle(1920f, 1080f, 800f, 600f);

		Assert.Equal(new Rectangle(0f, 75f, 800f, 450f), rect);
	}

	[Fact]
	public void FitRectangleFallsBackToTargetBoundsForInvalidInput() {
		var rect = VideoPlayer.FitRectangle(0f, 0f, 320f, 200f);

		Assert.Equal(new Rectangle(0f, 0f, 320f, 200f), rect);
	}
}
