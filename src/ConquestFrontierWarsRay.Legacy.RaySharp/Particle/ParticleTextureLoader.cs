using Raylib_cs;

namespace RaySharp.Particle;

internal static class ParticleTextureLoader {
	public static unsafe ParticleTextureResource Load(
		IReadOnlyDictionary<string, UtfTextureImage> textures,
		string textureName,
		ParticleEditorLog? log = null) {
		UtfTextureImage? image = string.IsNullOrWhiteSpace(textureName)
			? null
			: UtfTextureUtils.FindTextureImage(textures, textureName);

		if (image is not null) {
			log?.Info($"Texture match '{textureName}' -> '{image.Name}' {image.Width}x{image.Height}, alpha={image.HasAlpha}.");
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
				log?.Info($"Raylib texture id={texture.Id}, size={texture.Width}x{texture.Height}.");
				return new ParticleTextureResource(texture, $"{image.Name} ({image.Width}x{image.Height})", false);
			}
		}

		log?.Warning(string.IsNullOrWhiteSpace(textureName)
			? "Particle has no texture name; using fallback sprite."
			: $"Texture '{textureName}' not found in {textures.Count} decoded textures; using fallback sprite.");
		Image fallback = Raylib.GenImageColor(32, 32, Color.Blank);
		for (int y = 0; y < 32; y++) {
			for (int x = 0; x < 32; x++) {
				float dx = (x - 15.5f) / 15.5f;
				float dy = (y - 15.5f) / 15.5f;
				float distance = MathF.Sqrt(dx * dx + dy * dy);
				byte alpha = (byte)(Math.Clamp(1.0f - distance, 0.0f, 1.0f) * 255.0f);
				Raylib.ImageDrawPixel(ref fallback, x, y, new Color(255, 255, 255, (int)alpha));
			}
		}

		Texture2D fallbackTexture = Raylib.LoadTextureFromImage(fallback);
		Raylib.UnloadImage(fallback);
		Raylib.SetTextureFilter(fallbackTexture, TextureFilter.Bilinear);
		log?.Info($"Fallback texture id={fallbackTexture.Id}, size={fallbackTexture.Width}x{fallbackTexture.Height}.");
		return new ParticleTextureResource(fallbackTexture, "fallback sprite", true);
	}
}
