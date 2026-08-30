using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Raylib_cs;

namespace RaySharp.Mesh;

internal static class MeshTextureLoader {
	public static unsafe Dictionary<string, MeshTextureResource> Load(IReadOnlyDictionary<string, UtfTextureImage> images) {
		Dictionary<string, MeshTextureResource> textures = new(StringComparer.OrdinalIgnoreCase);
		foreach ((string name, UtfTextureImage image) in images) {
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
				Raylib.SetTextureWrap(texture, TextureWrap.Repeat);
				textures[name] = new MeshTextureResource(texture, $"{image.Name} ({image.Width}x{image.Height})");
			}
		}

		return textures;
	}

	public static MeshTextureResource? Find(IReadOnlyDictionary<string, MeshTextureResource> textures, string? textureName) {
		if (string.IsNullOrWhiteSpace(textureName)) {
			return null;
		}

		if (textures.TryGetValue(textureName, out MeshTextureResource? exact)) {
			return exact;
		}

		string baseName = Path.GetFileNameWithoutExtension(textureName).ToLowerInvariant();
		return textures.FirstOrDefault(pair =>
			Path.GetFileNameWithoutExtension(pair.Key).ToLowerInvariant() == baseName).Value;
	}
}
