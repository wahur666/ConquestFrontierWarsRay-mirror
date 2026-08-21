using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.PixelFormats;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class CompressedTexture2DTests {
	[Fact]
	public void DecodeTga_ReadsDimensionsAndPixelData() {
		var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.tga");

		try {
			using (var image = new Image<Rgba32>(2, 1)) {
				image[0, 0] = new Rgba32(255, 0, 0, 255);
				image[1, 0] = new Rgba32(0, 255, 0, 128);
				image.Save(path, new TgaEncoder());
			}

			var decoded = CompressedTexture2D.DecodeTga(path);

			Assert.Equal(2, decoded.Width);
			Assert.Equal(1, decoded.Height);
			Assert.Equal(8, decoded.Rgba.Length);
			Assert.Equal(255, decoded.Rgba[0]);
			Assert.Equal(0, decoded.Rgba[1]);
			Assert.Equal(0, decoded.Rgba[2]);
			Assert.Equal(255, decoded.Rgba[3]);
		} finally {
			if (File.Exists(path)) {
				File.Delete(path);
			}
		}
	}
}
