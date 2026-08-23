using ConquestFrontierWarsRay.Framework;

namespace ConquestFrontierWarsRay.Data;

/// <summary>
/// Resource locator rooted at the repository asset tree.
/// </summary>
public sealed class RepoResourceLocator : AssetRootResourceLocator {
	public RepoResourceLocator() : base(RepoPaths.LocateAssetsRoot()) {
	}
}
