using Xunit;

namespace ConquestFrontierWarsRay.Globals.Tests;

public sealed class GlobalResourceManifestTests {
	[Fact]
	public void LoadDefault_ReadsEmbeddedJsonManifest() {
		var manifest = GlobalResourceManifest.LoadDefault();

		Assert.Equal(ConquestGlobalsConstants.ScreenHIdealWidth, manifest.ScreenHIdealWidth);
		Assert.Equal(ConquestGlobalsConstants.ScreenHIdealHeight, manifest.ScreenHIdealHeight);
		Assert.Equal(9, manifest.DefaultColorTable.Count);
		Assert.Equal(new RgbColor(255, 255, 0), manifest.DefaultColorTable[1]);
		Assert.Equal(16, manifest.SectorColorTable.Count);
		Assert.Equal(new RgbColor(0, 128, 255), manifest.SectorColorTable[7]);
	}
}
