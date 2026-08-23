using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Base resource for things that can provide a 2D texture slice.
/// </summary>
public abstract class Texture2D : Resource {
	private TextureFilter? _filter = TextureFilter.Bilinear;

	/// <summary>
	/// Creates a texture resource.
	/// </summary>
	protected Texture2D(string? resourcePath = null) : base(resourcePath) {
	}

	/// <summary>
	/// Size of the drawable texture area in pixels.
	/// </summary>
	public abstract Vector2 Size { get; }

	/// <summary>
	/// Optional sampling filter applied to the backing texture.
	/// </summary>
	public TextureFilter? Filter {
		get => _filter;
		set {
			_filter = value;
			if (IsLoaded && value.HasValue) {
				ApplyTextureFilterCore(value.Value);
			}
		}
	}

	public abstract TextureSlice GetSlice();

	/// <summary>
	/// Applies the configured filter after the native texture becomes available.
	/// </summary>
	protected void ApplyConfiguredFilter() {
		if (_filter.HasValue) {
			ApplyTextureFilterCore(_filter.Value);
		}
	}

	/// <summary>
	/// Applies one filter value to the current backing texture.
	/// </summary>
	protected abstract void ApplyTextureFilterCore(TextureFilter filter);

	/// <summary>
	/// Native texture data plus the source rectangle to draw.
	/// </summary>
	public readonly record struct TextureSlice(Raylib_cs.Texture2D Texture, Rectangle Source);
}
