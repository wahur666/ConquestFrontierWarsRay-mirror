using BT = ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Globals;
using LegacyEngine = MapGen;

namespace ConquestFrontierWarsRay.Runtime.MapGen;

public sealed class MapGenerationService {
	public MapGenerationRequest CreateRequest(ConquestGlobalsService globals, BT.BT_MAP_GEN mapGenData, int seed, string? mapName = null) {
		ArgumentNullException.ThrowIfNull(globals);
		ArgumentNullException.ThrowIfNull(mapGenData);
		return new MapGenerationRequest {
			MapGenData = mapGenData,
			GameSettings = globals.GetGameSettings(),
			Seed = seed,
			MapName = mapName ?? string.Empty,
		};
	}

	public IReadOnlyList<int> GetPossibleSystemNumbers(CqGameSettings settings) {
		var legacyGame = LegacyMapGenAdapter.ToLegacyGame(settings);
		return LegacyEngine.MapGen.GetPossibleSystemNumbers(legacyGame);
	}

	public GeneratedMap Generate(MapGenerationRequest request) {
		var run = RunLegacy(request);
		var map = run.Engine.CurrentMap;
		var systems = map.systems
			.Where(static system => system is not null)
			.Take(map.systemCount)
			.Select(system => new GeneratedSystem {
				Index = system.index,
				SectorGridX = system.sectorGridX,
				SectorGridY = system.sectorGridY,
				LocalX = system.x,
				LocalY = system.y,
				Size = system.size,
				PlayerId = system.playerID,
				ConnectionOrder = system.connectionOrder,
				ThemeIndex = FindThemeIndex(run.LegacyMapGen, system.theme),
				ThemeKey = GetThemeKey(system.theme),
			})
			.ToArray();
		var jumpgates = map.jumpgate
			.Where(static gate => gate is not null && gate.created)
			.Take((int)map.numJumpGates)
			.Select(gate => new GeneratedJumpgate {
				SourceSystemIndex = gate.system1.index,
				TargetSystemIndex = gate.system2.index,
			})
			.ToArray();

		return new GeneratedMap {
			Seed = request.Seed,
			PlayerCount = map.numPlayers,
			SystemCount = map.systemCount,
			TemplateType = request.GameSettings.TemplateType,
			Systems = systems,
			Jumpgates = jumpgates,
			RawOutput = run.Output,
		};
	}

	public string GenerateRawOutput(MapGenerationRequest request) {
		return RunLegacy(request).Output;
	}

	private static LegacyRunResult RunLegacy(MapGenerationRequest request) {
		ArgumentNullException.ThrowIfNull(request);
		var legacyMapGen = LegacyMapGenAdapter.ToLegacyMapGen(request.MapGenData);
		var legacyGame = LegacyMapGenAdapter.ToLegacyGame(request.GameSettings);
		var engine = new LegacyEngine.MapGen(legacyMapGen, LegacyMapGenDefaults.CreateBaseFieldData());

		var previousOut = Console.Out;
		using var writer = new StringWriter();
		Console.SetOut(writer);
		try {
			engine.GenerateMap(legacyGame, request.Seed);
		}
		finally {
			Console.SetOut(previousOut);
		}

		return new LegacyRunResult(engine, legacyMapGen, writer.ToString());
	}

	private static int FindThemeIndex(LegacyEngine.BT_MAP_GEN source, LegacyEngine._terrainTheme theme) {
		for (var index = 0; index < source.themes.Length; index++) {
			if (ReferenceEquals(source.themes[index], theme)) {
				return index;
			}
		}

		return -1;
	}

	private static string GetThemeKey(LegacyEngine._terrainTheme theme) {
		static string FirstNonEmpty(IEnumerable<string> values) => values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

		var key = FirstNonEmpty(theme.systemKit);
		if (!string.IsNullOrEmpty(key)) {
			return key;
		}

		key = FirstNonEmpty(theme.habitablePlanets);
		if (!string.IsNullOrEmpty(key)) {
			return key;
		}

		key = FirstNonEmpty(theme.gasPlanets);
		if (!string.IsNullOrEmpty(key)) {
			return key;
		}

		return FirstNonEmpty(theme.metalPlanets);
	}

	private sealed record LegacyRunResult(LegacyEngine.MapGen Engine, LegacyEngine.BT_MAP_GEN LegacyMapGen, string Output);
}
