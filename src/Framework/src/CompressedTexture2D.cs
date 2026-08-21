using System.Numerics;
using Raylib_cs;
using SixLabors.ImageSharp.PixelFormats;
using ImageSharpImage = SixLabors.ImageSharp.Image;
using RaylibImage = Raylib_cs.Image;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Loads a texture from an image file on disk.
/// </summary>
public sealed class CompressedTexture2D : Texture2D {
	private Raylib_cs.Texture2D _texture;

	internal sealed class DecodedImageData {
		public required byte[] Rgba { get; init; }
		public required int Width { get; init; }
		public required int Height { get; init; }
	}

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

	public override TextureSlice GetSlice() {
		EnsureLoaded();

		return new TextureSlice(
			_texture,
			new Rectangle(0f, 0f, _texture.Width, _texture.Height));
	}

	protected override void LoadCore() {
		if (string.Equals(Path.GetExtension(ResourcePath), ".tga", StringComparison.OrdinalIgnoreCase)) {
			LoadTgaTexture();
			return;
		}

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
			ApplyConfiguredFilter();
		} finally {
			if (Raylib.IsImageValid(image)) {
				Raylib.UnloadImage(image);
			}
		}
	}

	internal static DecodedImageData DecodeTga(string path) {
		ArgumentException.ThrowIfNullOrWhiteSpace(path);

		using var image = ImageSharpImage.Load<Rgba32>(path);
		var pixels = new byte[image.Width * image.Height * 4];
		image.CopyPixelDataTo(pixels);
		return new DecodedImageData {
			Rgba = pixels,
			Width = image.Width,
			Height = image.Height
		};
	}

	private unsafe void LoadTgaTexture() {
		var decoded = DecodeTga(ResourcePath!);

		fixed (byte* pixels = decoded.Rgba) {
			RaylibImage image = new() {
				Data = pixels,
				Width = decoded.Width,
				Height = decoded.Height,
				Mipmaps = 1,
				Format = PixelFormat.UncompressedR8G8B8A8
			};

			var texture = Raylib.LoadTextureFromImage(image);
			if (!Raylib.IsTextureValid(texture)) {
				throw new InvalidOperationException($"Failed to create texture resource from TGA image '{ResourcePath}'.");
			}

			_texture = texture;
			ApplyConfiguredFilter();
		}
	}

	protected override void ApplyTextureFilterCore(TextureFilter filter) {
		if (Raylib.IsTextureValid(_texture)) {
			Raylib.SetTextureFilter(_texture, filter);
		}
	}

	protected override void UnloadCore() {
		if (Raylib.IsTextureValid(_texture)) {
			Raylib.UnloadTexture(_texture);
		}

		_texture = default;
	}
}
