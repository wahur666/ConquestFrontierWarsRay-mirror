using EngineLegacy = MapGen;
using XmlLegacy = ConquestFrontierWarsRay.Runtime.MapGen.Legacy;

namespace ConquestFrontierWarsRay.Runtime.MapGen;

public static class LegacyMapGenXmlAdapter {
	public static EngineLegacy.BT_MAP_GEN ToLegacyMapGen(XmlLegacy.BT_MAP_GEN source, bool moonsEnabled = false) {
		ArgumentNullException.ThrowIfNull(source);
		var result = new EngineLegacy.BT_MAP_GEN {
			MoonsEnabled = moonsEnabled,
		};

		for (var index = 0; index < Math.Min(source.Themes.Count, EngineLegacy.BT_MAP_GEN.MAX_THEMES); index++) {
			result.themes[index] = ToLegacyTheme(source.Themes[index]);
		}

		for (var index = source.Themes.Count; index < EngineLegacy.BT_MAP_GEN.MAX_THEMES; index++) {
			result.themes[index] = new EngineLegacy._terrainTheme();
		}

		return result;
	}

	private static EngineLegacy._terrainTheme ToLegacyTheme(XmlLegacy.TerrainTheme source) {
		var theme = new EngineLegacy._terrainTheme {
			sizeOk = (EngineLegacy.DMapGen.SECTOR_SIZE)source.SizeOk,
			minSize = source.MinSize,
			maxSize = source.MaxSize,
			sizeFunc = (EngineLegacy.DMapGen.DMAP_FUNC)source.SizeFunc,
			minMoonsPerPlanet = source.MinMoonPerPlanet,
			maxMoonsPerPlanet = source.MaxMoonPerPlanet,
			moonNumberFunc = (EngineLegacy.DMapGen.DMAP_FUNC)source.MoonNumberFunc,
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
			theme.terrain[index] = index < source.Terrain.Count
				? ToLegacyTerrain(source.Terrain[index])
				: new EngineLegacy.TerrainInfo();
		}

		for (var index = 0; index < theme.nuggetMetalTypes.Length; index++) {
			theme.nuggetMetalTypes[index] = index < source.NuggetMetalTypes.Count
				? ToLegacyTerrain(source.NuggetMetalTypes[index])
				: new EngineLegacy.TerrainInfo();
		}

		for (var index = 0; index < theme.nuggetGasTypes.Length; index++) {
			theme.nuggetGasTypes[index] = index < source.NuggetGasTypes.Count
				? ToLegacyTerrain(source.NuggetGasTypes[index])
				: new EngineLegacy.TerrainInfo();
		}

		for (var index = 0; index < theme.macros.Length; index++) {
			theme.macros[index] = index < source.Macros.Count
				? ToLegacyMacro(source.Macros[index])
				: new EngineLegacy.Macros();
		}

		return theme;
	}

	private static EngineLegacy.TerrainInfo ToLegacyTerrain(XmlLegacy.TerrainInfo source) {
		return new EngineLegacy.TerrainInfo {
			terrainArchType = source.TerrainArchType,
			probability = source.Probability,
			minToPlace = source.MinToPlace,
			maxToPlace = source.MaxToPlace,
			numberFunc = (EngineLegacy.DMapGen.DMAP_FUNC)source.NumberFunc,
			size = source.Size,
			requiredToPlace = source.RequiredToPlace,
			overlap = (EngineLegacy.DMapGen.OVERLAP)source.Overlap,
			placement = (EngineLegacy.DMapGen.PLACEMENT)source.Placement,
		};
	}

	private static EngineLegacy.Macros ToLegacyMacro(XmlLegacy.Macro source) {
		return new EngineLegacy.Macros {
			operation = (EngineLegacy.DMapGen.MACRO_OPERATION)source.Operation,
			range = source.Range,
			active = source.Active,
			info = new EngineLegacy.Info {
				terrainInfo = string.IsNullOrWhiteSpace(source.Info.TerrainArchType) ? null : ToLegacyTerrain(source.Info),
				overlap = source.Info.TerrainArchType.Length == 0 ? (EngineLegacy.DMapGen.OVERLAP?)source.Info.Overlap : null,
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
