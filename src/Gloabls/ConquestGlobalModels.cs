namespace ConquestFrontierWarsRay.Globals;

public enum InterfaceResolution {
	NoResolution = 0,
	InGameResolution,
	FrontEndResolution,
}

public enum TextureLod {
	UltraLow = 0,
	Low = 1,
	Medium = 2,
	High = 3,
}

public enum MissionEndResult {
	Resign = 1,
	Won = 2,
	Lost = 3,
	Quit = 4,
	Splash = 5,
}

public enum UpgradeQueryType {
	Fighter,
	Shields,
}

public enum ObjectiveState {
	Pending,
	Complete,
	Failed,
}

public enum PlayerTechLevel : byte {
	Level0,
	Level1,
	Level2,
	Level3,
	Level4,
	Level5,
}

public enum CqDifficulty {
	NoDifficulty,
	Easy,
	Average,
	Hard,
}

public enum CqSlotType {
	Human,
	Computer,
}

public enum CqComputerChallenge {
	Easy,
	Average,
	Hard,
	Impossible,
	Nightmare,
}

public enum CqSlotState {
	Open,
	Closed,
	Active,
	Ready,
}

public enum CqRace {
	NoRace,
	Terran,
	Mantis,
	Solarian,
	Vyrium,
}

public enum CqColor {
	UndefinedColor,
	Yellow,
	Red,
	Blue,
	Pink,
	Green,
	Orange,
	Purple,
	Aqua,
}

public enum CqTeam {
	NoTeam,
	Team1,
	Team2,
	Team3,
	Team4,
}

public enum CqGameType {
	KillUnits = -2,
	KillHqPlatforms = -1,
	MissionDefined = 0,
	KillPlatformsFabricators = 1,
}

public enum CqMoney {
	Low = -2,
	Medium = -1,
	High = 0,
}

public enum CqMapType {
	Selected = -2,
	User = -1,
	Random = 0,
}

public enum CqRandomTemplate {
	NewRandom = -2,
	Random = -1,
	Ring = 0,
	Star = 1,
}

public enum CqMapSize {
	Small = -2,
	Medium = -1,
	Large = 0,
}

public enum CqTerrain {
	Light = -2,
	Medium = -1,
	Heavy = 0,
}

public enum CqStartingUnits {
	Minimal = -2,
	Medium = -1,
	Large = 0,
}

public enum CqVisibilityMode {
	Normal = -1,
	Explored = 0,
	All = 1,
}

public enum CqCommandLimit {
	Low = -2,
	Normal = -1,
	Mid = 0,
	High = 1,
}

public sealed class CqGameSlot {
	public CqSlotType Type { get; set; }
	public CqComputerChallenge ComputerChallenge { get; set; }
	public CqSlotState State { get; set; }
	public CqRace Race { get; set; }
	public CqColor Color { get; set; }
	public CqTeam Team { get; set; }
	public uint ZoneSeat { get; set; }
	public uint Dpid { get; set; }
}

public sealed class CqGameSettings {
	public uint Version { get; set; }
	public CqGameType GameType { get; set; }
	public int GameSpeed { get; set; }
	public bool RegenOn { get; set; }
	public bool SpectatorsOn { get; set; }
	public bool LockDiplomacyOn { get; set; }
	public uint NumSystems { get; set; }
	public CqMoney Money { get; set; }
	public CqMapType MapType { get; set; }
	public CqRandomTemplate TemplateType { get; set; }
	public CqMapSize MapSize { get; set; }
	public CqTerrain Terrain { get; set; }
	public CqStartingUnits Units { get; set; }
	public CqVisibilityMode Visibility { get; set; }
	public CqCommandLimit CommandLimit { get; set; }
	public byte ActiveSlots { get; set; }
	public bool HostBusy { get; set; }
	public byte StartCountdown { get; set; }
	public CqGameSlot[] Slots { get; } = Enumerable.Range(0, ConquestGlobalsConstants.MaxPlayers)
		.Select(static _ => new CqGameSlot())
		.ToArray();
}

public sealed class RaceTechMask {
	public uint Tech { get; set; }
	public uint Build { get; set; }
	public uint Common { get; set; }
	public uint CommonExtra { get; set; }
	public uint Cq2Var1 { get; set; }
	public uint Cq2Var2 { get; set; }

	public RaceTechMask Clone() {
		return new RaceTechMask {
			Tech = Tech,
			Build = Build,
			Common = Common,
			CommonExtra = CommonExtra,
			Cq2Var1 = Cq2Var1,
			Cq2Var2 = Cq2Var2,
		};
	}
}

public sealed class TechNode {
	private const uint NoCompMask = 0x0FFFFFFF;
	private const uint RaceMask = 0x0FFFFFFF;

	public RaceTechMask[] Races { get; } = Enumerable.Range(0, ConquestGlobalsConstants.NumTechRaces)
		.Select(static _ => new RaceTechMask())
		.ToArray();

	public RaceTechMask GetRace(MRace race) {
		var index = (int)race - 1;
		if (index < 0 || index >= Races.Length) {
			throw new ArgumentOutOfRangeException(nameof(race));
		}

		return Races[index];
	}

	public bool HasTech(MRace race, uint tech, uint build, uint common, uint commonExtra, uint cq2Var1, uint cq2Var2) {
		if (race == MRace.M_NO_RACE) {
			return false;
		}

		var raceMask = GetRace(race);
		return ((tech & raceMask.Tech) & NoCompMask) == (tech & NoCompMask)
		       && ((build & raceMask.Build) & NoCompMask) == (build & NoCompMask)
		       && ((cq2Var1 & raceMask.Cq2Var1) & NoCompMask) == (cq2Var1 & NoCompMask)
		       && ((cq2Var2 & raceMask.Cq2Var2) & NoCompMask) == (cq2Var2 & NoCompMask)
		       && (common & raceMask.Common) == common
		       && (commonExtra & raceMask.CommonExtra) == commonExtra;
	}

	public bool HasSomeTech(MRace race, uint tech, uint build, uint common, uint commonExtra, uint cq2Var1, uint cq2Var2) {
		if (race == MRace.M_NO_RACE) {
			return false;
		}

		var raceMask = GetRace(race);
		return (tech & raceMask.Tech & RaceMask) != 0
		       || (build & raceMask.Build & RaceMask) != 0
		       || (cq2Var1 & raceMask.Cq2Var1 & RaceMask) != 0
		       || (cq2Var2 & raceMask.Cq2Var2 & RaceMask) != 0
		       || (common & raceMask.Common) != 0
		       || (commonExtra & raceMask.CommonExtra) != 0;
	}

	public void InitializeFullTree() {
		foreach (var race in Races) {
			race.Tech = 0xFFFFFFFF;
			race.Build = 0xFFFFFFFF;
			race.Common = 0x0FFFFFFF;
			race.CommonExtra = 0xFFFFFFFF;
			race.Cq2Var1 = 0xFFFFFFFF;
			race.Cq2Var2 = 0xFFFFFFFF;
		}
	}

	public TechNode Clone() {
		var clone = new TechNode();
		for (var index = 0; index < Races.Length; index++) {
			var source = Races[index];
			var target = clone.Races[index];
			target.Tech = source.Tech;
			target.Build = source.Build;
			target.Common = source.Common;
			target.CommonExtra = source.CommonExtra;
			target.Cq2Var1 = source.Cq2Var1;
			target.Cq2Var2 = source.Cq2Var2;
		}

		return clone;
	}
}

public sealed class PlayerUpgradeLevels {
	public PlayerTechLevel Engine { get; set; }
	public PlayerTechLevel Hull { get; set; }
	public PlayerTechLevel Supplies { get; set; }
	public PlayerTechLevel Targeting { get; set; }
	public PlayerTechLevel Damage { get; set; }
	public PlayerTechLevel Shields { get; set; }
	public PlayerTechLevel Sensors { get; set; }
	public PlayerTechLevel Fighter { get; set; }
	public PlayerTechLevel Tanker { get; set; }
	public PlayerTechLevel Tender { get; set; }
	public PlayerTechLevel Fleet { get; set; }

	public PlayerUpgradeLevels Clone() {
		return (PlayerUpgradeLevels)MemberwiseClone();
	}
}

public sealed class MissionObjective {
	public required uint StringId { get; init; }
	public bool IsSecondary { get; set; }
	public ObjectiveState State { get; set; } = ObjectiveState.Pending;
}

public sealed class MissionObjectivesState {
	public uint MissionNameStringId { get; set; }
	public uint OverviewStringId { get; set; }
	public List<MissionObjective> Items { get; } = [];
}

public sealed class PlayerResourceState {
	public uint Gas { get; set; }
	public uint Metal { get; set; }
	public uint Crew { get; set; }
	public uint TotalCommandPoints { get; set; }
	public uint UsedCommandPoints { get; set; }
	public uint MaxGas { get; set; }
	public uint MaxMetal { get; set; }
	public uint MaxCrew { get; set; }
	public uint MaxCommandPoints { get; set; }

	public PlayerResourceState Clone() {
		return (PlayerResourceState)MemberwiseClone();
	}
}

public sealed class PlayerGameStats {
	public uint MetalGained { get; set; }
	public uint NumUnitsBuilt { get; set; }
	public uint GasGained { get; set; }
	public uint NumUnitsDestroyed { get; set; }
	public uint NumAdmiralsBuilt { get; set; }
	public uint CrewGained { get; set; }
	public uint NumUnitsLost { get; set; }
	public uint NumPlatformsBuilt { get; set; }
	public uint NumPlatformsDestroyed { get; set; }
	public uint NumJumpgatesControlled { get; set; }
	public uint NumPlatformsLost { get; set; }
	public uint NumUnitsConverted { get; set; }
	public uint NumPlatformsConverted { get; set; }
	public uint NumResearchComplete { get; set; }
	public byte PercentSystemsExplored { get; set; }

	public PlayerGameStats Clone() {
		return (PlayerGameStats)MemberwiseClone();
	}
}
