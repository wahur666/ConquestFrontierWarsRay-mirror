using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Loads a texture from an image file on disk.
/// </summary>
internal sealed class CompressedTexture2D : Texture2D {
	private Raylib_cs.Texture2D _texture;

	/// <summary>
	/// Creates a texture resource from a file path.
	/// </summary>
	public CompressedTexture2D(string path) : base(path) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);
	}

	/// <summary>
	/// Pixel size of the loaded texture.
	/// </summary>
	public override Vector2 Size {
		get {
			var slice = GetSlice();
			return new Vector2(slice.Source.Width, slice.Source.Height);
		}
	}

	internal override TextureSlice GetSlice() {
		EnsureLoaded();

		return new TextureSlice(
			_texture,
			new Rectangle(0f, 0f, _texture.Width, _texture.Height));
	}

	protected override void LoadCore() {
		var image = Raylib.LoadImage(ResourcePath!);

		try {
			if (!Raylib.IsImageValid(image)) {
				throw new InvalidOperationException($"Failed to load image for texture resource '{ResourcePath}'.");
			}

			var texture = Raylib.LoadTextureFromImage(image);

			if (!Raylib.IsTextureValid(texture)) {
				throw new InvalidOperationException($"Failed to create texture resource from image '{ResourcePath}'.");
			}

			_texture = texture;
		} finally {
			if (Raylib.IsImageValid(image)) {
				Raylib.UnloadImage(image);
			}
		}
	}

	protected override void UnloadCore() {
		if (Raylib.IsTextureValid(_texture)) {
			Raylib.UnloadTexture(_texture);
		}

		_texture = default;
	}
}
