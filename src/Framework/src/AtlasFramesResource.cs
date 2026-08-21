using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework;

/// <summary>
/// Composite resource that owns an atlas texture, its frame metadata, and the sliced frame collection.
/// </summary>
public sealed class AtlasFramesResource : Resource {
	private readonly AtlasDefinitionResource _atlasDefinition;
	private readonly SpriteFrames _frames;
	private readonly CompressedTexture2D _texture;

	public AtlasFramesResource(string texturePath, string atlasPath, TextureFilter? filter = null) : base(atlasPath) {
		_texture = Own(new CompressedTexture2D(texturePath) {
			Filter = filter
		});
		_atlasDefinition = Own(new AtlasDefinitionResource(atlasPath));
		_frames = Own(SpriteFrames.FromAtlas(_texture, _atlasDefinition));
	}

	public CompressedTexture2D Texture {
		get {
			EnsureLoaded();
			return _texture;
		}
	}

	public AtlasDefinitionResource AtlasDefinition {
		get {
			EnsureLoaded();
			return _atlasDefinition;
		}
	}

	public SpriteFrames Frames {
		get {
			EnsureLoaded();
			return _frames;
		}
	}

	protected override void LoadCore() {
		_ = _frames.GetFrameRegion(0);
	}
}
