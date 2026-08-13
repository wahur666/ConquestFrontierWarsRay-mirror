using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Base resource for things that can provide a 2D texture slice.
/// </summary>
internal abstract class Texture2D : Resource {
	/// <summary>
	/// Creates a texture resource.
	/// </summary>
	protected Texture2D(string? resourcePath = null) : base(resourcePath) {
	}

	/// <summary>
	/// Size of the drawable texture area in pixels.
	/// </summary>
	public abstract Vector2 Size { get; }

	internal abstract TextureSlice GetSlice();

	/// <summary>
	/// Native texture data plus the source rectangle to draw.
	/// </summary>
	internal readonly record struct TextureSlice(Raylib_cs.Texture2D Texture, Rectangle Source);
}
