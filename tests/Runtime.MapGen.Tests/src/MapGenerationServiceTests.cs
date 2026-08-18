using ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Globals;
using Xunit;

namespace ConquestFrontierWarsRay.Runtime.MapGen.Tests;

public sealed class MapGenerationServiceTests {
	[Fact]
	public void CreateRequest_UsesManagedGlobalsGameSettings() {
		var globals = new ConquestGlobalsService();
		var settings = new CqGameSettings {
			TemplateType = CqRandomTemplate.Ring,
			NumSystems = 8,
			ActiveSlots = 2,
		};
		settings.Slots[0].State = CqSlotState.Ready;
		settings.Slots[1].State = CqSlotState.Active;
		globals.SetGameSettings(settings);

		var service = new MapGenerationService();
		var request = service.CreateRequest(globals, CreateMapGen(), 7);

		Assert.Equal(CqRandomTemplate.Ring, request.GameSettings.TemplateType);
		Assert.Equal((uint)8, request.GameSettings.NumSystems);
		Assert.Equal((byte)2, request.GameSettings.ActiveSlots);
	}

	[Fact]
	public void GetPossibleSystemNumbers_UsesTemplateRulesFromManagedSettings() {
		var service = new MapGenerationService();
		var settings = new CqGameSettings {
			TemplateType = CqRandomTemplate.Star,
			ActiveSlots = 3,
			NumSystems = 10,
		};
		settings.Slots[0].State = CqSlotState.Ready;
		settings.Slots[1].State = CqSlotState.Ready;
		settings.Slots[2].State = CqSlotState.Ready;

		var numbers = service.GetPossibleSystemNumbers(settings);

		Assert.Equal([4, 7, 10, 13, 16], numbers);
	}

	[Fact]
	public void Generate_IsDeterministicForSeedAndManagedSettings() {
		var globals = new ConquestGlobalsService();
		var settings = new CqGameSettings {
			TemplateType = CqRandomTemplate.Random,
			MapSize = CqMapSize.Small,
			Terrain = CqTerrain.Light,
			NumSystems = 6,
			ActiveSlots = 3,
		};
		settings.Slots[0].State = CqSlotState.Ready;
		settings.Slots[1].State = CqSlotState.Ready;
		settings.Slots[2].State = CqSlotState.Ready;
		globals.SetGameSettings(settings);

		var service = new MapGenerationService();
		var request = service.CreateRequest(globals, CreateMapGen(), 12345);

		var first = service.Generate(request);
		var second = service.Generate(request);

		Assert.Equal(3, first.PlayerCount);
		Assert.Equal(6, first.SystemCount);
		Assert.Equal(first.Systems.Select(system => (system.SectorGridX, system.SectorGridY, system.ThemeIndex, system.Size)),
			second.Systems.Select(system => (system.SectorGridX, system.SectorGridY, system.ThemeIndex, system.Size)));
	}

	[Fact]
	public void GenerateRawOutput_UsesLegacyConsoleOutputPath() {
		var globals = new ConquestGlobalsService();
		var settings = new CqGameSettings {
			TemplateType = CqRandomTemplate.Random,
			MapSize = CqMapSize.Small,
			Terrain = CqTerrain.Light,
			NumSystems = 6,
			ActiveSlots = 3,
		};
		settings.Slots[0].State = CqSlotState.Ready;
		settings.Slots[1].State = CqSlotState.Ready;
		settings.Slots[2].State = CqSlotState.Ready;
		globals.SetGameSettings(settings);

		var service = new MapGenerationService();
		var request = service.CreateRequest(globals, CreateMapGen(), 12345);

		var output = service.GenerateRawOutput(request);

		Assert.Contains("MAP GENERATION SEED = 12345", output);
		Assert.Contains("\"sectors\":", output);
	}

	[Fact]
	public void Generate_UsesLegacyRingBehaviorForNonMultipleSystemCounts() {
		var globals = new ConquestGlobalsService();
		var settings = new CqGameSettings {
			TemplateType = CqRandomTemplate.Ring,
			MapSize = CqMapSize.Small,
			NumSystems = 5,
			ActiveSlots = 2,
		};
		settings.Slots[0].State = CqSlotState.Ready;
		settings.Slots[1].State = CqSlotState.Ready;
		globals.SetGameSettings(settings);

		var service = new MapGenerationService();
		var request = service.CreateRequest(globals, CreateMapGen(), 1);

		var generated = service.Generate(request);

		Assert.Equal(4, generated.SystemCount);
		Assert.Equal(CqRandomTemplate.Ring, generated.TemplateType);
	}

	private static BT_MAP_GEN CreateMapGen() {
		return new BT_MAP_GEN {
			Themes = [
				new BT_MAP_TERRAIN_THEME {
					OkForPlayerStart = true,
					OkForRemoteSystem = false,
					SizeOk = MAP_SECTOR_SIZE.ALL_SIZE,
					MinSize = 12,
					MaxSize = 16,
					SizeFunc = MAP_DMAP_FUNC.LINEAR,
					SystemKit = ["PlayerKit"],
				},
				new BT_MAP_TERRAIN_THEME {
					OkForPlayerStart = false,
					OkForRemoteSystem = true,
					SizeOk = MAP_SECTOR_SIZE.ALL_SIZE,
					MinSize = 10,
					MaxSize = 14,
					SizeFunc = MAP_DMAP_FUNC.LINEAR,
					SystemKit = ["RemoteKit"],
				},
			],
		};
	}
}
