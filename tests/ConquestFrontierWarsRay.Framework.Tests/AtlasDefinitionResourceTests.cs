using System.Text.Json;
using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class AtlasDefinitionResourceTests {
	[Fact]
	public void Constructor_RejectsBlankPath() {
		Assert.Throws<ArgumentException>(() => new AtlasDefinitionResource(" "));
	}

	[Fact]
	public void FramesAndGetFrame_LoadAtlasData() {
		var path = CreateAtlasFile(new {
			frames = new[] {
				new { x = 1, y = 2, width = 3, height = 4 },
				new { x = 5, y = 6, width = 7, height = 8 }
			}
		});

		try {
			var atlas = new AtlasDefinitionResource(path);

			Assert.False(atlas.IsLoaded);

			var frames = atlas.Frames;
			var second = atlas.GetFrame(1);

			Assert.True(atlas.IsLoaded);
			Assert.Equal(2, frames.Count);
			Assert.Equal(1f, frames[0].X);
			Assert.Equal(2f, frames[0].Y);
			Assert.Equal(3f, frames[0].Width);
			Assert.Equal(4f, frames[0].Height);
			Assert.Equal(5f, second.X);
			Assert.Equal(6f, second.Y);
			Assert.Equal(7f, second.Width);
			Assert.Equal(8f, second.Height);
		} finally {
			File.Delete(path);
		}
	}

	[Fact]
	public void Frames_RejectEmptyAtlas() {
		var path = CreateAtlasFile(new { frames = Array.Empty<object>() });

		try {
			var atlas = new AtlasDefinitionResource(path);

			Assert.Throws<InvalidOperationException>(() => _ = atlas.Frames);
		} finally {
			File.Delete(path);
		}
	}

	[Fact]
	public void Dispose_UnloadsAtlasDefinition() {
		var path = CreateAtlasFile(new {
			frames = new[] {
				new { x = 1, y = 2, width = 3, height = 4 }
			}
		});

		try {
			var atlas = new AtlasDefinitionResource(path);
			_ = atlas.Frames;

			atlas.Dispose();

			Assert.True(atlas.IsDisposed);
			Assert.False(atlas.IsLoaded);
		} finally {
			File.Delete(path);
		}
	}

	private static string CreateAtlasFile(object payload) {
		var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
		File.WriteAllText(path, JsonSerializer.Serialize(payload));
		return path;
	}
}
