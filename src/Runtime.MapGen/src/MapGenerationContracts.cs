using ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Globals;

namespace ConquestFrontierWarsRay.Runtime.MapGen;

public sealed class MapGenerationRequest {
	public required BT_MAP_GEN MapGenData { get; init; }
	public required CqGameSettings GameSettings { get; init; }
	public required int Seed { get; init; }
	public string MapName { get; init; } = string.Empty;
}

public sealed class GeneratedMap {
	public required int Seed { get; init; }
	public required int PlayerCount { get; init; }
	public required int SystemCount { get; init; }
	public required CqRandomTemplate TemplateType { get; init; }
	public required IReadOnlyList<GeneratedSystem> Systems { get; init; }
	public required IReadOnlyList<GeneratedJumpgate> Jumpgates { get; init; }
	public string RawOutput { get; init; } = string.Empty;
}

public sealed class GeneratedSystem {
	public required int Index { get; init; }
	public required int SectorGridX { get; init; }
	public required int SectorGridY { get; init; }
	public required uint LocalX { get; init; }
	public required uint LocalY { get; init; }
	public required uint Size { get; init; }
	public required int PlayerId { get; init; }
	public required int ConnectionOrder { get; init; }
	public required int ThemeIndex { get; init; }
	public required string ThemeKey { get; init; }
}

public sealed class GeneratedJumpgate {
	public required int SourceSystemIndex { get; init; }
	public required int TargetSystemIndex { get; init; }
}
