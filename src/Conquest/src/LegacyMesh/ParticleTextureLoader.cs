using System;
using System.Collections.Generic;
using Raylib_cs;

namespace RaySharp.Particle;

internal static class ParticleTextureLoader {
	public static unsafe ParticleTextureResource Load(
		IReadOnlyDictionary<string, UtfTextureImage> textures,
		string textureName) {
		UtfTextureImage? image = string.IsNullOrWhiteSpace(textureName)
			? null
			: UtfTextureUtils.FindTextureImage(textures, textureName);

		if (image is not null) {
			fixed (byte* pixels = image.Rgba) {
				Image rayImage = new() {
					Data = pixels,
					Width = image.Width,
					Height = image.Height,
					Mipmaps = 1,
					Format = PixelFormat.UncompressedR8G8B8A8
				};
				Texture2D texture = Raylib.LoadTextureFromImage(rayImage);
				Raylib.SetTextureFilter(texture, TextureFilter.Bilinear);
				return new ParticleTextureResource(texture, $"{image.Name} ({image.Width}x{image.Height})", false);
			}
		}

		Image fallback = Raylib.GenImageColor(32, 32, Color.Blank);
		for (int y = 0; y < 32; y++) {
			for (int x = 0; x < 32; x++) {
				var dx = (x - 15.5f) / 15.5f;
				var dy = (y - 15.5f) / 15.5f;
				var distance = MathF.Sqrt(dx * dx + dy * dy);
				var alpha = (byte)(Math.Clamp(1f - distance, 0f, 1f) * 255f);
				Raylib.ImageDrawPixel(ref fallback, x, y, new Color((byte)255, (byte)255, (byte)255, alpha));
			}
		}

		Texture2D fallbackTexture = Raylib.LoadTextureFromImage(fallback);
		Raylib.UnloadImage(fallback);
		Raylib.SetTextureFilter(fallbackTexture, TextureFilter.Bilinear);
		return new ParticleTextureResource(fallbackTexture, "fallback sprite", true);
	}
}
