using ConquestFrontierWarsRay.Data;

namespace ConquestFrontierWarsRay.Data.Tests.Common;

public sealed class RepoPathsTests {
	[Fact]
	public void LocateRepoRoot_Finds_Workspace_Root_Instead_Of_Output_Assets_Copy() {
		var repoRoot = RepoPaths.LocateRepoRoot();
		var assetsRoot = RepoPaths.LocateAssetsRoot();
		var vfxJsonPath = RepoPaths.LocateVfxAnimationDataPath();

		Assert.Equal("ConquestFrontierWarsRay", Path.GetFileName(repoRoot));
		Assert.True(Directory.Exists(Path.Combine(repoRoot, ".git")));
		Assert.True(Directory.Exists(Path.Combine(repoRoot, "src")));
		Assert.Equal(Path.Combine(repoRoot, "assets"), assetsRoot);
		Assert.Equal(Path.Combine(assetsRoot, "DB", "vfx-animation-data.json"), vfxJsonPath);
		Assert.True(File.Exists(vfxJsonPath));
	}
}
