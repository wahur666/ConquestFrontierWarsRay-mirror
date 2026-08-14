using System.Numerics;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.Tests;

public sealed class TextureAndSpriteTests {
	[Fact]
	public void CompressedTextureConstructor_RejectsBlankPath() {
		Assert.Throws<ArgumentException>(() => new CompressedTexture2D(" "));
	}

	[Fact]
	public void AtlasTexture_ExposesRegionAndLoadsUnderlyingAtlas() {
		var atlasTexture = new FakeTexture(new Texture2D.TextureSlice(
			new Raylib_cs.Texture2D(),
			new Rectangle(10f, 20f, 100f, 200f)));
		var region = new Rectangle(3f, 4f, 5f, 6f);
		var texture = new AtlasTexture(atlasTexture, region);

		var slice = texture.GetSlice();

		Assert.Same(atlasTexture, texture.Atlas);
		Assert.Equal(region, texture.Region);
		Assert.Equal(new Vector2(5f, 6f), texture.Size);
		Assert.Equal(1, atlasTexture.LoadCount);
		Assert.Equal(2, atlasTexture.GetSliceCount);
		Assert.Equal(13f, slice.Source.X);
		Assert.Equal(24f, slice.Source.Y);
		Assert.Equal(5f, slice.Source.Width);
		Assert.Equal(6f, slice.Source.Height);
	}

	[Fact]
	public void AtlasTexture_RejectsNullAtlas() {
		Assert.Throws<ArgumentNullException>(() => new AtlasTexture(null!, new Rectangle()));
	}

	[Fact]
	public void Sprite_UsesExpectedDefaultsAndAllowsTextureSwap() {
		var firstTexture = new FakeTexture(new Texture2D.TextureSlice(
			new Raylib_cs.Texture2D(),
			new Rectangle(0f, 0f, 32f, 16f)));
		var secondTexture = new FakeTexture(new Texture2D.TextureSlice(
			new Raylib_cs.Texture2D(),
			new Rectangle(0f, 0f, 8f, 4f)));
		var sprite = new Sprite(firstTexture, "Sprite");

		sprite.SetTexture(secondTexture);

		Assert.Equal("Sprite", sprite.Name);
		Assert.Equal(new Vector2(0.5f, 0.5f), sprite.Pivot);
		Assert.Equal(Color.White, sprite.Modulate);
	}

	[Fact]
	public void Sprite_RejectsNullTexture() {
		Assert.Throws<ArgumentNullException>(() => new Sprite(null!));

		var texture = new FakeTexture(new Texture2D.TextureSlice(
			new Raylib_cs.Texture2D(),
			new Rectangle(0f, 0f, 1f, 1f)));
		var sprite = new Sprite(texture);

		Assert.Throws<ArgumentNullException>(() => sprite.SetTexture(null!));
	}
}
