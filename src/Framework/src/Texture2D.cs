using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Base resource for things that can provide a 2D texture slice.
/// </summary>
public abstract class Texture2D : Resource {
	/// <summary>
	/// Creates a texture resource.
	/// </summary>
	protected Texture2D(string? resourcePath = null) : base(resourcePath) {
	}

	/// <summary>
	/// Size of the drawable texture area in pixels.
	/// </summary>
	public abstract Vector2 Size { get; }

	public abstract TextureSlice GetSlice();

	/// <summary>
	/// Native texture data plus the source rectangle to draw.
	/// </summary>
	public readonly record struct TextureSlice(Raylib_cs.Texture2D Texture, Rectangle Source);
}
