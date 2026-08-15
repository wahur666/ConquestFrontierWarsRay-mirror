namespace ConquestFrontierWarsRay.Globals;

public static class ConquestGlobalsConstants {
	public const int ScreenHIdealWidth = 1024;
	public const int ScreenHIdealHeight = 768;
	public const int MaxPlayers = 8;
	public const int MaxPlayersPlusOne = 9;
	public const int MaxMissionObjectives = 32;
	public const int NumRacesPlusOne = 5;
	public const int NumTechRaces = 4;
	public const int NumTechLevels = 6;
	public const int BaseMaxCrew = 250;
	public const int BaseMaxMetal = 220;
	public const int BaseMaxGas = 180;
	public const int DefaultMaxControlPointsPerPlayer = 100;
	public const int GasMultiplier = 25;
	public const int MetalMultiplier = 25;
	public const int CrewMultiplier = 10;
	public const float DefaultElapsedTime = 8.0f / 30.0f;
	public const float DefaultRealtimeFramerate = 30.0f / 8.0f;
	public const float RenderFramerate = 30.0f;
	public const float RenderPeriod = 1.0f / 30.0f;
	public const uint AdmiralMask = 0x80000000;
	public const uint SubordinateIdMask = 0x7F000000;
	public const uint PlayerIdMask = 0x0000000F;
	public const uint GroupId = 15;

	public static bool IsAdmiralPartId(uint missionId) {
		return (missionId & (AdmiralMask | SubordinateIdMask)) == AdmiralMask;
	}
}
