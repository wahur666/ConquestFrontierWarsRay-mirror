namespace ConquestFrontierWarsRay.Globals;

public sealed class ConquestGlobalState {
	public ConquestGlobalState() {
		PlayerAssignments = new byte[ConquestGlobalsConstants.MaxPlayers];
		PlayerNames = Enumerable.Repeat(string.Empty, ConquestGlobalsConstants.MaxPlayers).ToArray();
		PlayerRaces = new MRace[ConquestGlobalsConstants.MaxPlayersPlusOne];
		OneWayAllianceMasks = new byte[ConquestGlobalsConstants.MaxPlayers];
		VisibilityMasks = new byte[ConquestGlobalsConstants.MaxPlayers];
		ColorAssignments = new byte[ConquestGlobalsConstants.MaxPlayersPlusOne];
		PlayerResources = Enumerable.Range(0, ConquestGlobalsConstants.MaxPlayersPlusOne)
			.Select(static _ => new PlayerResourceState())
			.ToArray();
		CurrentTechLevels = Enumerable.Range(0, ConquestGlobalsConstants.MaxPlayersPlusOne)
			.Select(static _ => new TechNode())
			.ToArray();
		WorkingTechLevels = Enumerable.Range(0, ConquestGlobalsConstants.MaxPlayersPlusOne)
			.Select(static _ => new TechNode())
			.ToArray();
		AvailableTechLevel = new TechNode();
		PlayerUpgradeLevels = Enumerable.Range(0, ConquestGlobalsConstants.MaxPlayersPlusOne)
			.Select(static _ => Enumerable.Range(0, ConquestGlobalsConstants.NumRacesPlusOne)
				.Select(static _ => new PlayerUpgradeLevels())
				.ToArray())
			.ToArray();
		GameStats = Enumerable.Range(0, ConquestGlobalsConstants.MaxPlayersPlusOne)
			.Select(static _ => new PlayerGameStats())
			.ToArray();
		PlayerScores = new byte[ConquestGlobalsConstants.MaxPlayersPlusOne];
		PlayerResignedBySlot = new bool[ConquestGlobalsConstants.MaxPlayers];
		Objectives = new MissionObjectivesState();
		GameSettings = new CqGameSettings();
	}

	public byte[] PlayerAssignments { get; }
	public string[] PlayerNames { get; }
	public MRace[] PlayerRaces { get; }
	public byte[] OneWayAllianceMasks { get; }
	public byte[] VisibilityMasks { get; }
	public byte[] ColorAssignments { get; }
	public PlayerResourceState[] PlayerResources { get; }
	public TechNode[] CurrentTechLevels { get; }
	public TechNode[] WorkingTechLevels { get; }
	public TechNode AvailableTechLevel { get; }
	public PlayerUpgradeLevels[][] PlayerUpgradeLevels { get; }
	public PlayerGameStats[] GameStats { get; }
	public byte[] PlayerScores { get; }
	public bool[] PlayerResignedBySlot { get; }
	public MissionObjectivesState Objectives { get; }
	public CqGameSettings GameSettings { get; }
	public uint LastPartNumber { get; set; }
	public uint CurrentPlayerId { get; set; }
	public uint HostPlayerId { get; set; }
	public uint MissionId { get; set; }
	public bool GlobalLightingEnabled { get; set; }
	public bool ScriptUiControlEnabled { get; set; }
	public bool GameActive { get; set; }
	public uint UpdateCount { get; set; }
	public uint LastStreamId { get; set; }
	public uint LastTeletypeId { get; set; }
	public string ScriptLibraryName { get; set; } = string.Empty;
	public string TerrainFilename { get; set; } = string.Empty;
}
