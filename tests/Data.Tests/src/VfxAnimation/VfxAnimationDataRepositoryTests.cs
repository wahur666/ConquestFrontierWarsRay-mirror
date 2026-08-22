using ConquestFrontierWarsRay.Data.VfxAnimation;

namespace ConquestFrontierWarsRay.Data.Tests.VfxAnimation;

public sealed class VfxAnimationDataRepositoryTests {
	[Fact]
	public void Repository_Loads_Generated_Vfx_Animation_Data() {
		var repository = VfxAnimationDataRepository.LocateFromRepo();

		var data = repository.Load();

		Assert.NotEmpty(data.Image);
		Assert.NotEmpty(data.Atlas);

		var andromWorm = Assert.Contains("Animate!!AndromWorm", data.Image);
		Assert.Equal("VFXShape!!AndromWorm", andromWorm.VfxShapeId);
		Assert.Equal("AndromWorm.tga", andromWorm.Filename);

		var fuzz = Assert.Contains("Animate!!Fuzz", data.Atlas);
		Assert.Equal("VFXShape!!AnimateFuzz", fuzz.VfxShapeId);
		Assert.Equal("animFuzz_atlas.png", fuzz.Filename);
		Assert.Equal("animFuzz_atlas.json", fuzz.MetaJson);
	}

	[Fact]
	public void TryGetAtlasByShapeId_AcceptsLegacyShpShapeFileName() {
		var repository = VfxAnimationDataRepository.LocateFromRepo();

		var found = repository.TryGetAtlasByShapeId("listDropColor.shp", out var atlas);

		Assert.True(found);
		Assert.Equal("listDropColor", atlas.Key);
		Assert.Equal("listDropColor", atlas.Value.VfxShapeId);
		Assert.Equal("listDropColor_atlas.png", atlas.Value.Filename);
	}
}
