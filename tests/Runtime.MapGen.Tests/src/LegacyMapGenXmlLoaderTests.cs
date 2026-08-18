using ConquestFrontierWarsRay.Runtime.MapGen.Legacy;
using LegacyEngine = MapGen;
using Xunit;

namespace ConquestFrontierWarsRay.Runtime.MapGen.Tests;

public sealed class LegacyMapGenXmlLoaderTests {
	[Fact]
	public void LoadFromDefaultRepoXml_ParsesMapGenThemes() {
		var xmlPath = LegacyMapGenXmlLoader.LocateDefaultXmlPath();

		var mapGen = LegacyMapGenXmlLoader.LoadFromFile(xmlPath);

		Assert.NotEmpty(mapGen.Themes);
		Assert.Equal("MAPGEN!!Map", mapGen.Id);
		Assert.Equal("new1", mapGen.Themes[0].SystemKit[0]);
	}

	[Fact]
	public void XmlModel_ConvertsToLegacyEngineMapGen() {
		var xmlPath = LegacyMapGenXmlLoader.LocateDefaultXmlPath();
		var xmlMapGen = LegacyMapGenXmlLoader.LoadFromFile(xmlPath);

		var mapGen = LegacyMapGenXmlAdapter.ToLegacyMapGen(xmlMapGen);

		Assert.Equal(LegacyEngine.BT_MAP_GEN.MAX_THEMES, mapGen.themes.Length);
		Assert.Equal("new1", mapGen.themes[0].systemKit[0]);
		Assert.Equal(LegacyEngine.DMapGen.DMAP_FUNC.MORE_IS_LIKLY, mapGen.themes[0].sizeFunc);
	}
}
