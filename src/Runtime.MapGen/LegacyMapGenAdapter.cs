using BT = ConquestFrontierWarsRay.Data.Models.BT;
using Legacy = MapGen;
using ConquestFrontierWarsRay.Globals;

namespace ConquestFrontierWarsRay.Runtime.MapGen;

internal static class LegacyMapGenAdapter {
	public static Legacy.FULLCQGAME ToLegacyGame(CqGameSettings settings) {
		ArgumentNullException.ThrowIfNull(settings);
		var game = new Legacy.FULLCQGAME {
			numSystems = checked((int)settings.NumSystems),
			gameType = (Legacy.GAMETYPE)settings.GameType,
			gameSpeed = settings.GameSpeed,
			regenOn = settings.RegenOn,
			spectatorsOn = settings.SpectatorsOn,
			lockDiplomacyOn = settings.LockDiplomacyOn,
			money = (Legacy.MONEY)settings.Money,
			mapType = (Legacy.MAPTYPE)settings.MapType,
			templateType = (Legacy.RANDOM_TEMPLATE)settings.TemplateType,
			mapSize = (Legacy.MAPSIZE)settings.MapSize,
			terrain = (Legacy.TERRAIN)settings.Terrain,
			units = (Legacy.STARTING_UNITS)settings.Units,
			visibility = (Legacy.VISIBILITYMODE)settings.Visibility,
			commandLimit = (Legacy.COMMANDLIMIT)settings.CommandLimit,
		};

		for (var index = 0; index < settings.Slots.Length; index++) {
			var slot = settings.Slots[index];
			game.Slots.Add(new Legacy.Slot {
				type = (Legacy.TYPE)slot.Type,
				compChalange = (Legacy.COMP_CHALANGE)slot.ComputerChallenge,
				state = (Legacy.STATE)slot.State,
				race = (Legacy.RACE)slot.Race,
				color = (Legacy.COLOR)slot.Color,
				team = (Legacy.TEAM)slot.Team,
				zoneSeat = slot.ZoneSeat,
				dpid = checked((int)slot.Dpid),
			});
		}

		return game;
	}

	public static Legacy.BT_MAP_GEN ToLegacyMapGen(BT.BT_MAP_GEN source, bool moonsEnabled = false) {
		ArgumentNullException.ThrowIfNull(source);
		var result = new Legacy.BT_MAP_GEN {
			MoonsEnabled = moonsEnabled,
		};

		for (var index = 0; index < Math.Min(source.Themes.Length, Legacy.BT_MAP_GEN.MAX_THEMES); index++) {
			result.themes[index] = ToLegacyTheme(source.Themes[index]);
		}

		for (var index = source.Themes.Length; index < Legacy.BT_MAP_GEN.MAX_THEMES; index++) {
			result.themes[index] = new Legacy._terrainTheme();
		}

		return result;
	}

	private static Legacy._terrainTheme ToLegacyTheme(BT.BT_MAP_TERRAIN_THEME source) {
		var theme = new Legacy._terrainTheme {
			sizeOk = (Legacy.DMapGen.SECTOR_SIZE)source.SizeOk,
			minSize = source.MinSize,
			maxSize = source.MaxSize,
			sizeFunc = (Legacy.DMapGen.DMAP_FUNC)source.SizeFunc,
			minMoonsPerPlanet = source.MinMoonsPerPlanet,
			maxMoonsPerPlanet = source.MaxMoonsPerPlanet,
			moonNumberFunc = (Legacy.DMapGen.DMAP_FUNC)source.MoonNumberFunc,
			okForPlayerStart = source.OkForPlayerStart,
			okForRemoteSystem = source.OkForRemoteSystem,
		};

		CopyStrings(source.SystemKit, theme.systemKit);
		CopyStrings(source.MetalPlanets, theme.metalPlanets);
		CopyStrings(source.GasPlanets, theme.gasPlanets);
		CopyStrings(source.HabitablePlanets, theme.habitablePlanets);
		CopyStrings(source.OtherPlanets, theme.otherPlanets);
		CopyStrings(source.MoonTypes, theme.moonTypes);
		CopyUInts(source.NumHabitablePlanets, theme.numHabitablePlanets);
		CopyUInts(source.NumMetalPlanets, theme.numMetalPlanets);
		CopyUInts(source.NumGasPlanets, theme.numGasPlanets);
		CopyUInts(source.NumOtherPlanets, theme.numOtherPlanets);
		CopyUInts(source.NumNuggetPatchesMetal, theme.numNuggetPatchesMetal);
		CopyUInts(source.NumNuggetPatchesGas, theme.numNuggetPatchesGas);
		CopyFloats(source.Density, theme.density);

		for (var index = 0; index < theme.terrain.Length; index++) {
			theme.terrain[index] = index < source.Terrain.Length
				? ToLegacyTerrain(source.Terrain[index])
				: new Legacy.TerrainInfo();
		}

		for (var index = 0; index < theme.nuggetMetalTypes.Length; index++) {
			theme.nuggetMetalTypes[index] = index < source.NuggetMetalTypes.Length
				? ToLegacyTerrain(source.NuggetMetalTypes[index])
				: new Legacy.TerrainInfo();
		}

		for (var index = 0; index < theme.nuggetGasTypes.Length; index++) {
			theme.nuggetGasTypes[index] = index < source.NuggetGasTypes.Length
				? ToLegacyTerrain(source.NuggetGasTypes[index])
				: new Legacy.TerrainInfo();
		}

		for (var index = 0; index < theme.macros.Length; index++) {
			theme.macros[index] = index < source.Macros.Length
				? ToLegacyMacro(source.Macros[index])
				: new Legacy.Macros();
		}

		return theme;
	}

	private static Legacy.TerrainInfo ToLegacyTerrain(BT.BT_MAP_TERRAIN_INFO source) {
		return new Legacy.TerrainInfo {
			terrainArchType = source.TerrainArchType,
			probability = source.Probability,
			minToPlace = source.MinToPlace,
			maxToPlace = source.MaxToPlace,
			numberFunc = (Legacy.DMapGen.DMAP_FUNC)source.NumberFunc,
			size = source.Size,
			requiredToPlace = source.RequiredToPlace,
			overlap = (Legacy.DMapGen.OVERLAP)source.Overlap,
			placement = (Legacy.DMapGen.PLACEMENT)source.Placement,
		};
	}

	private static Legacy.Macros ToLegacyMacro(BT.BT_MAP_MACRO source) {
		return new Legacy.Macros {
			operation = (Legacy.DMapGen.MACRO_OPERATION)source.Operation,
			range = source.Range,
			active = source.Active,
			info = new Legacy.Info {
				terrainInfo = string.IsNullOrWhiteSpace(source.Info.TerrainArchType) ? null : ToLegacyTerrain(source.Info),
				overlap = source.Info.TerrainArchType.Length == 0 ? (Legacy.DMapGen.OVERLAP?)source.Info.Overlap : null,
			},
		};
	}

	private static void CopyStrings(IReadOnlyList<string> source, string[] destination) {
		for (var index = 0; index < destination.Length; index++) {
			destination[index] = index < source.Count ? source[index] : string.Empty;
		}
	}

	private static void CopyUInts(IReadOnlyList<uint> source, uint[] destination) {
		for (var index = 0; index < destination.Length; index++) {
			destination[index] = index < source.Count ? source[index] : 0u;
		}
	}

	private static void CopyFloats(IReadOnlyList<float> source, float[] destination) {
		for (var index = 0; index < destination.Length; index++) {
			destination[index] = index < source.Count ? source[index] : 0f;
		}
	}
}
