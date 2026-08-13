using System.Numerics;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Exposes one rectangular region from a larger texture.
/// </summary>
internal sealed class AtlasTexture : Texture2D {
	/// <summary>
	/// Creates a texture view into an atlas texture.
	/// </summary>
	public AtlasTexture(Texture2D atlas, Rectangle region) {
		Atlas = atlas ?? throw new ArgumentNullException(nameof(atlas));
		Region = region;
	}

	/// <summary>
	/// Source atlas texture.
	/// </summary>
	public Texture2D Atlas { get; }

	/// <summary>
	/// Rectangle inside the atlas to draw.
	/// </summary>
	public Rectangle Region { get; set; }

	/// <summary>
	/// Size of the selected atlas region.
	/// </summary>
	public override Vector2 Size => new(Region.Width, Region.Height);

	internal override TextureSlice GetSlice() {
		EnsureLoaded();
		var atlasSlice = Atlas.GetSlice();

		return new TextureSlice(
			atlasSlice.Texture,
			new Rectangle(
				atlasSlice.Source.X + Region.X,
				atlasSlice.Source.Y + Region.Y,
				Region.Width,
				Region.Height));
	}

	protected override void LoadCore() {
		Atlas.GetSlice();
	}
}
