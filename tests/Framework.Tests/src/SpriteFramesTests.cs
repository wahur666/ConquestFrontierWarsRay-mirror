using System.Text.Json;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class SpriteFramesTests {
	[Fact]
	public void FromTexture_CreatesSingleFullSizeFrame() {
		var texture = new FakeTexture(new Texture2D.TextureSlice(
			new Raylib_cs.Texture2D(),
			new Rectangle(0f, 0f, 32f, 16f)));

		var frames = SpriteFrames.FromTexture(texture);
		var frame = frames.GetFrameRegion(0);

		Assert.Equal(1, frames.Count);
		Assert.Equal(new Rectangle(0f, 0f, 32f, 16f), frame);
		Assert.Equal(1, texture.LoadCount);
		Assert.Equal(1, texture.GetSliceCount);
	}

	[Fact]
	public void FromAtlas_NormalizesAtlasJsonIntoIndexedFrameRegions() {
		var texture = new FakeTexture(new Texture2D.TextureSlice(
			new Raylib_cs.Texture2D(),
			new Rectangle(0f, 0f, 128f, 128f)));
		var path = CreateAtlasFile(new {
			frames = new[] {
				new { x = 1, y = 2, width = 3, height = 4 },
				new { x = 5, y = 6, width = 7, height = 8 }
			}
		});

		try {
			var atlas = new AtlasDefinitionResource(path);
			var frames = SpriteFrames.FromAtlas(texture, atlas);

			Assert.Equal(2, frames.Count);
			Assert.Equal(new Rectangle(1f, 2f, 3f, 4f), frames.GetFrameRegion(0));
			Assert.Equal(new Rectangle(5f, 6f, 7f, 8f), frames.GetFrameRegion(1));
		} finally {
			File.Delete(path);
		}
	}

	[Fact]
	public void FromGrid_GeneratesIndexedRegionsWithSpacingAndFrameLimit() {
		var texture = new FakeTexture(new Texture2D.TextureSlice(
			new Raylib_cs.Texture2D(),
			new Rectangle(0f, 0f, 64f, 32f)));

		var frames = SpriteFrames.FromGrid(
			texture,
			frameWidth: 16,
			frameHeight: 16,
			spacingX: 2,
			frameCount: 3);

		Assert.Equal(3, frames.Count);
		Assert.Equal(new Rectangle(0f, 0f, 16f, 16f), frames.GetFrameRegion(0));
		Assert.Equal(new Rectangle(18f, 0f, 16f, 16f), frames.GetFrameRegion(1));
		Assert.Equal(new Rectangle(36f, 0f, 16f, 16f), frames.GetFrameRegion(2));
	}

	[Fact]
	public void GetFrameTexture_CreatesAtlasTextureForIndexedFrame() {
		var texture = new FakeTexture(new Texture2D.TextureSlice(
			new Raylib_cs.Texture2D(),
			new Rectangle(10f, 20f, 64f, 64f)));
		var frames = new SpriteFrames(texture, [
			new Rectangle(2f, 3f, 8f, 9f)
		]);

		var frameTexture = frames.GetFrameTexture(0);
		var slice = frameTexture.GetSlice();

		Assert.Equal(new Rectangle(12f, 23f, 8f, 9f), slice.Source);
	}

	private static string CreateAtlasFile(object payload) {
		var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
		File.WriteAllText(path, JsonSerializer.Serialize(payload));
		return path;
	}
}
