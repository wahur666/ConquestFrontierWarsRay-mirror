using ConquestFrontierWarsRay.Data.VfxAnimation;
using ConquestFrontierWarsRay.Framework;
using Raylib_cs;

namespace ConquestFrontierWarsRay.Framework.TestApp;

internal static class VfxAnimationAtlasExtensions {
	public static AtlasFramesResource CreateAtlasFramesResource(this VfxAnimationDataRepository repository, AtlasData atlasData, TextureFilter? filter = null) {
		ArgumentNullException.ThrowIfNull(repository);
		ArgumentNullException.ThrowIfNull(atlasData);

		return new AtlasFramesResource(
			repository.GetInterfaceAssetPath(atlasData, metaJson: false),
			repository.GetInterfaceAssetPath(atlasData, metaJson: true),
			filter);
	}
}
