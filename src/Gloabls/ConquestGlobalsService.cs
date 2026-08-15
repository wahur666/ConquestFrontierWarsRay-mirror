namespace ConquestFrontierWarsRay.Globals;

public sealed class ConquestGlobalsService {
	private const uint CommonEngineMask = 0x0000001F;
	private const uint CommonShieldsMask = 0x000003E0;
	private const uint CommonHullMask = 0x00007C00;
	private const uint CommonSuppliesMask = 0x000F8000;
	private const uint CommonDamageMask = 0x01F00000;
	private const uint CommonExtraFleetMask = 0x0000001F;
	private const uint CommonExtraTankerMask = 0x000003E0;
	private const uint CommonExtraTenderMask = 0x00007C00;
	private const uint CommonExtraSensorsMask = 0x000F8000;
	private const uint CommonExtraFighterMask = 0x01F00000;

	private uint _lastIssuedPartId;

	public ConquestGlobalsService() {
		State = new ConquestGlobalState();
		ResourceManifest = GlobalResourceManifest.LoadDefault();
		Reset();
	}

	public ConquestGlobalState State { get; }
	public GlobalResourceManifest ResourceManifest { get; }
	public event EventHandler? ObjectivesChanged;
	public event EventHandler<MissionObjective>? ObjectiveAdded;

	public void Reset() {
		Array.Clear(State.PlayerAssignments);
		for (var slot = 0; slot < State.PlayerNames.Length; slot++) {
			State.PlayerNames[slot] = string.Empty;
		}
		Array.Clear(State.PlayerRaces);
		Array.Clear(State.OneWayAllianceMasks);
		Array.Clear(State.VisibilityMasks);
		Array.Clear(State.ColorAssignments);
		Array.Clear(State.PlayerScores);
		Array.Clear(State.PlayerResignedBySlot);
		State.LastPartNumber = 0;
		State.CurrentPlayerId = 1;
		State.HostPlayerId = 1;
		State.MissionId = 0;
		State.GlobalLightingEnabled = true;
		State.ScriptUiControlEnabled = false;
		State.GameActive = false;
		State.UpdateCount = 0;
		State.LastStreamId = 0;
		State.LastTeletypeId = 0;
		State.ScriptLibraryName = string.Empty;
		State.TerrainFilename = string.Empty;
		ResetGameSettings();
		State.Objectives.MissionNameStringId = 0;
		State.Objectives.OverviewStringId = 0;
		State.Objectives.Items.Clear();
		_lastIssuedPartId = 0;

		for (var player = 0; player <= ConquestGlobalsConstants.MaxPlayers; player++) {
			State.PlayerRaces[player] = MRace.M_TERRAN;
			State.ColorAssignments[player] = (byte)player;
			State.PlayerResources[player].Gas = player == 0 ? 0u : ConquestGlobalsConstants.BaseMaxGas;
			State.PlayerResources[player].Metal = player == 0 ? 0u : ConquestGlobalsConstants.BaseMaxMetal;
			State.PlayerResources[player].Crew = player == 0 ? 0u : ConquestGlobalsConstants.BaseMaxCrew;
			State.PlayerResources[player].MaxGas = ConquestGlobalsConstants.BaseMaxGas;
			State.PlayerResources[player].MaxMetal = ConquestGlobalsConstants.BaseMaxMetal;
			State.PlayerResources[player].MaxCrew = ConquestGlobalsConstants.BaseMaxCrew;
			State.PlayerResources[player].MaxCommandPoints = ConquestGlobalsConstants.DefaultMaxControlPointsPerPlayer;
			State.CurrentTechLevels[player] = new TechNode();
			State.WorkingTechLevels[player] = new TechNode();
		}

		for (var playerIndex = 0; playerIndex < ConquestGlobalsConstants.MaxPlayers; playerIndex++) {
			State.PlayerAssignments[playerIndex] = (byte)(playerIndex + 1);
			State.OneWayAllianceMasks[playerIndex] = (byte)(1 << playerIndex);
			State.VisibilityMasks[playerIndex] = (byte)(1 << playerIndex);
		}

		State.AvailableTechLevel.InitializeFullTree();
	}

	public void SetCurrentPlayer(uint playerId) {
		ValidatePlayerId(playerId);
		State.CurrentPlayerId = playerId;
	}

	public void SetHostPlayer(uint playerId) {
		ValidatePlayerId(playerId);
		State.HostPlayerId = playerId;
	}

	public bool IsHost() {
		return State.CurrentPlayerId == State.HostPlayerId;
	}

	public void SetAlly(uint playerId1, uint playerId2, bool ally = true) {
		ValidatePlayerId(playerId1);
		ValidatePlayerId(playerId2);
		var index = OneWayIndex(playerId1);
		if (ally) {
			State.OneWayAllianceMasks[index] |= (byte)(1 << OneWayIndex(playerId2));
		}
		else {
			State.OneWayAllianceMasks[index] &= (byte)~(1 << OneWayIndex(playerId2));
		}
	}

	public bool AreAllies(uint playerId1, uint playerId2) {
		ValidatePlayerId(playerId1);
		ValidatePlayerId(playerId2);
		var one = GetAllyMask(playerId1);
		var two = GetAllyMask(playerId2);
		return (one & two & (1u << ((int)playerId1 - 1))) != 0;
	}

	public uint GetOneWayAllyMask(uint playerId) {
		ValidatePlayerId(playerId);
		return State.OneWayAllianceMasks[OneWayIndex(playerId)];
	}

	public uint GetAllyMask(uint playerId) {
		ValidatePlayerId(playerId);
		return CalculateTwoWayAllyMask((int)playerId);
	}

	public byte[] GetAllyData() {
		return State.OneWayAllianceMasks.ToArray();
	}

	public void SetAllyData(ReadOnlySpan<byte> allyData) {
		if (allyData.Length != State.OneWayAllianceMasks.Length) {
			throw new ArgumentException("Ally data length does not match the expected player count.", nameof(allyData));
		}

		allyData.CopyTo(State.OneWayAllianceMasks);
	}

	public uint GetVisibilityMask(uint playerId) {
		ValidatePlayerId(playerId);
		return State.VisibilityMasks[playerId - 1];
	}

	public void SetVisibilityMask(uint playerId, uint visibilityMask) {
		ValidatePlayerId(playerId);
		State.VisibilityMasks[OneWayIndex(playerId)] = checked((byte)visibilityMask);
	}

	public void SetPlayerVisibility(uint playerId1, uint playerId2) {
		ValidatePlayerId(playerId1);
		ValidatePlayerId(playerId2);
		State.VisibilityMasks[OneWayIndex(playerId1)] |= (byte)(1 << OneWayIndex(playerId2));
		State.VisibilityMasks[OneWayIndex(playerId2)] |= (byte)(1 << OneWayIndex(playerId1));
	}

	public uint GetPlayerFromPartId(uint missionId) {
		return missionId & ConquestGlobalsConstants.PlayerIdMask;
	}

	public uint GetOwnerFromPartId(uint missionId) {
		return missionId & ~ConquestGlobalsConstants.SubordinateIdMask;
	}

	public uint GetColorId(uint playerId) {
		ValidatePlayerId(playerId);
		return State.ColorAssignments[PlayerIndex(playerId)];
	}

	public void SetColorId(uint playerId, byte colorId) {
		ValidatePlayerId(playerId);
		State.ColorAssignments[PlayerIndex(playerId)] = colorId;
	}

	public uint CreateNewPartId(uint playerId) {
		ValidatePlayerId(playerId);
		if (!IsHost() && State.GameActive) {
			throw new InvalidOperationException("CreateNewPartId cannot be called on a client while the game is active.");
		}

		State.LastPartNumber++;
		_lastIssuedPartId = (State.LastPartNumber << 4) | (playerId & ConquestGlobalsConstants.PlayerIdMask);
		return _lastIssuedPartId;
	}

	public uint CreateNewGroupPartId() {
		if (!IsHost() && State.GameActive) {
			throw new InvalidOperationException("CreateNewGroupPartId cannot be called on a client while the game is active.");
		}

		State.LastPartNumber++;
		_lastIssuedPartId = (State.LastPartNumber << 4) | ConquestGlobalsConstants.GroupId;
		return _lastIssuedPartId;
	}

	public uint CreateNewJumpgatePartId() {
		if (!IsHost() && State.GameActive) {
			throw new InvalidOperationException("CreateNewJumpgatePartId cannot be called on a client while the game is active.");
		}

		State.LastPartNumber++;
		_lastIssuedPartId = State.LastPartNumber << 4;
		return _lastIssuedPartId;
	}

	public uint CreateSubordinatePartId() {
		if (_lastIssuedPartId == 0) {
			throw new InvalidOperationException("CreateSubordinatePartId requires a previously tracked or created root part id.");
		}

		if (((_lastIssuedPartId & ConquestGlobalsConstants.SubordinateIdMask) >> 24) == 127) {
			throw new InvalidOperationException("No subordinate part ids remain for the current root part id.");
		}

		_lastIssuedPartId += 1u << 24;
		return _lastIssuedPartId;
	}

	public void TrackIssuedPartId(uint missionId) {
		if ((missionId & ConquestGlobalsConstants.SubordinateIdMask) == 0) {
			var number = (missionId & ~ConquestGlobalsConstants.AdmiralMask) >> 4;
			State.LastPartNumber = Math.Max(State.LastPartNumber, number);
			_lastIssuedPartId = missionId;
		}
	}

	public void SetCurrentTechLevel(uint playerId, TechNode techNode) {
		ValidatePlayerId(playerId);
		ArgumentNullException.ThrowIfNull(techNode);
		var playerIndex = PlayerIndex(playerId);
		State.CurrentTechLevels[playerIndex] = techNode.Clone();
		for (var raceIndex = 1; raceIndex < ConquestGlobalsConstants.NumRacesPlusOne; raceIndex++) {
			var raceMask = techNode.Races[raceIndex - 1];
			var target = State.PlayerUpgradeLevels[playerIndex][raceIndex];
			target.Engine = ToTechLevel(raceMask.Common & CommonEngineMask);
			target.Shields = ToTechLevel((raceMask.Common & CommonShieldsMask) >> 5);
			target.Hull = ToTechLevel((raceMask.Common & CommonHullMask) >> 10);
			target.Supplies = ToTechLevel((raceMask.Common & CommonSuppliesMask) >> 15);
			target.Damage = ToTechLevel((raceMask.Common & CommonDamageMask) >> 20);
			target.Fleet = ToTechLevel(raceMask.CommonExtra & CommonExtraFleetMask);
			target.Tanker = ToTechLevel((raceMask.CommonExtra & CommonExtraTankerMask) >> 5);
			target.Tender = ToTechLevel((raceMask.CommonExtra & CommonExtraTenderMask) >> 10);
			target.Sensors = ToTechLevel((raceMask.CommonExtra & CommonExtraSensorsMask) >> 15);
			target.Fighter = ToTechLevel((raceMask.CommonExtra & CommonExtraFighterMask) >> 20);
		}
	}

	public TechNode GetCurrentTechLevel(uint playerId) {
		ValidatePlayerId(playerId);
		return State.CurrentTechLevels[PlayerIndex(playerId)].Clone();
	}

	public void SetWorkingTechLevel(uint playerId, TechNode techNode) {
		ValidatePlayerId(playerId);
		ArgumentNullException.ThrowIfNull(techNode);
		State.WorkingTechLevels[PlayerIndex(playerId)] = techNode.Clone();
	}

	public TechNode GetWorkingTechLevel(uint playerId) {
		ValidatePlayerId(playerId);
		return State.WorkingTechLevels[PlayerIndex(playerId)].Clone();
	}

	public TechNode GetAvailableTechLevel() {
		return State.AvailableTechLevel.Clone();
	}

	public void SetAvailableTechLevel(TechNode techNode) {
		ArgumentNullException.ThrowIfNull(techNode);
		var clone = techNode.Clone();
		for (var index = 0; index < State.AvailableTechLevel.Races.Length; index++) {
			var source = clone.Races[index];
			var target = State.AvailableTechLevel.Races[index];
			target.Tech = source.Tech;
			target.Build = source.Build;
			target.Common = source.Common;
			target.CommonExtra = source.CommonExtra;
			target.Cq2Var1 = source.Cq2Var1;
			target.Cq2Var2 = source.Cq2Var2;
		}
	}

	public PlayerTechLevel GetUpgradeLevel(uint playerId, UpgradeQueryType upgradeType, MRace race) {
		ValidatePlayerId(playerId);
		ValidateRace(race);
		var levels = State.PlayerUpgradeLevels[PlayerIndex(playerId)][(int)race];
		return upgradeType switch {
			UpgradeQueryType.Fighter => levels.Fighter,
			UpgradeQueryType.Shields => levels.Shields,
			_ => throw new ArgumentOutOfRangeException(nameof(upgradeType)),
		};
	}

	public uint GetCurrentResource(uint playerId, MResourceType resourceType) {
		ValidatePlayerId(playerId);
		var resources = State.PlayerResources[PlayerIndex(playerId)];
		return resourceType switch {
			MResourceType.M_GAS => resources.Gas,
			MResourceType.M_METAL => resources.Metal,
			MResourceType.M_CREW => resources.Crew,
			MResourceType.M_COMMANDPTS => resources.TotalCommandPoints,
			_ => throw new ArgumentOutOfRangeException(nameof(resourceType)),
		};
	}

	public void SetCurrentGas(uint playerId, uint amount) {
		ValidatePlayerId(playerId);
		State.PlayerResources[PlayerIndex(playerId)].Gas = amount;
	}

	public void SetCurrentMetal(uint playerId, uint amount) {
		ValidatePlayerId(playerId);
		State.PlayerResources[PlayerIndex(playerId)].Metal = amount;
	}

	public void SetCurrentCrew(uint playerId, uint amount) {
		ValidatePlayerId(playerId);
		State.PlayerResources[PlayerIndex(playerId)].Crew = amount;
	}

	public void SetCurrentTotalCommandPoints(uint playerId, uint amount) {
		ValidatePlayerId(playerId);
		State.PlayerResources[PlayerIndex(playerId)].TotalCommandPoints = amount;
	}

	public uint GetCurrentUsedCommandPoints(uint playerId) {
		ValidatePlayerId(playerId);
		return State.PlayerResources[PlayerIndex(playerId)].UsedCommandPoints;
	}

	public void SetCurrentUsedCommandPoints(uint playerId, uint amount) {
		ValidatePlayerId(playerId);
		State.PlayerResources[PlayerIndex(playerId)].UsedCommandPoints = amount;
	}

	public void ResetResourceMax() {
		for (var playerId = 1u; playerId <= ConquestGlobalsConstants.MaxPlayers; playerId++) {
			var resources = State.PlayerResources[PlayerIndex(playerId)];
			resources.MaxGas = ConquestGlobalsConstants.BaseMaxGas;
			resources.MaxMetal = ConquestGlobalsConstants.BaseMaxMetal;
			resources.MaxCrew = ConquestGlobalsConstants.BaseMaxCrew;
		}
	}

	public uint GetMaxGas(uint playerId) {
		ValidatePlayerId(playerId);
		return State.PlayerResources[PlayerIndex(playerId)].MaxGas;
	}

	public void SetMaxGas(uint playerId, uint amount) {
		ValidatePlayerId(playerId);
		State.PlayerResources[PlayerIndex(playerId)].MaxGas = amount;
	}

	public uint GetMaxMetal(uint playerId) {
		ValidatePlayerId(playerId);
		return State.PlayerResources[PlayerIndex(playerId)].MaxMetal;
	}

	public void SetMaxMetal(uint playerId, uint amount) {
		ValidatePlayerId(playerId);
		State.PlayerResources[PlayerIndex(playerId)].MaxMetal = amount;
	}

	public uint GetMaxCrew(uint playerId) {
		ValidatePlayerId(playerId);
		return State.PlayerResources[PlayerIndex(playerId)].MaxCrew;
	}

	public void SetMaxCrew(uint playerId, uint amount) {
		ValidatePlayerId(playerId);
		State.PlayerResources[PlayerIndex(playerId)].MaxCrew = amount;
	}

	public uint GetMaxControlPoints(uint playerId) {
		ValidatePlayerId(playerId);
		return State.PlayerResources[PlayerIndex(playerId)].MaxCommandPoints;
	}

	public void SetMaxControlPoints(uint playerId, uint amount) {
		ValidatePlayerId(playerId);
		State.PlayerResources[PlayerIndex(playerId)].MaxCommandPoints = amount;
	}

	public void SetPlayerRace(uint playerId, MRace race) {
		ValidatePlayerId(playerId);
		State.PlayerRaces[PlayerIndex(playerId)] = race;
	}

	public MRace GetPlayerRace(uint playerId) {
		ValidatePlayerId(playerId);
		return State.PlayerRaces[PlayerIndex(playerId)];
	}

	public void SetMissionName(uint stringId) {
		State.Objectives.MissionNameStringId = stringId;
		ObjectivesChanged?.Invoke(this, EventArgs.Empty);
	}

	public uint GetMissionName() {
		return State.Objectives.MissionNameStringId;
	}

	public void SetMissionId(uint missionId) {
		State.MissionId = missionId;
	}

	public uint GetMissionId() {
		return State.MissionId;
	}

	public void SetMissionDescription(uint stringId) {
		State.Objectives.OverviewStringId = stringId;
		ObjectivesChanged?.Invoke(this, EventArgs.Empty);
	}

	public uint GetMissionDescription() {
		return State.Objectives.OverviewStringId;
	}

	public void AddObjective(uint stringId, bool secondary) {
		if (State.Objectives.Items.Count >= ConquestGlobalsConstants.MaxMissionObjectives) {
			return;
		}

		var objective = new MissionObjective {
			StringId = stringId,
			IsSecondary = secondary,
			State = ObjectiveState.Pending,
		};
		State.Objectives.Items.Add(objective);
		ObjectiveAdded?.Invoke(this, objective);
	}

	public void RemoveObjective(uint stringId) {
		var index = State.Objectives.Items.FindIndex(objective => objective.StringId == stringId);
		if (index < 0) {
			return;
		}

		State.Objectives.Items.RemoveAt(index);
		ObjectivesChanged?.Invoke(this, EventArgs.Empty);
	}

	public void MarkObjectiveCompleted(uint stringId) {
		var objective = State.Objectives.Items.FirstOrDefault(item => item.StringId == stringId);
		if (objective is null) {
			return;
		}

		objective.State = ObjectiveState.Complete;
		ObjectivesChanged?.Invoke(this, EventArgs.Empty);
	}

	public void MarkObjectiveFailed(uint stringId) {
		var objective = State.Objectives.Items.FirstOrDefault(item => item.StringId == stringId);
		if (objective is null) {
			return;
		}

		objective.State = ObjectiveState.Failed;
		ObjectivesChanged?.Invoke(this, EventArgs.Empty);
	}

	public bool IsObjectiveSecondary(uint stringId) {
		return State.Objectives.Items.FirstOrDefault(item => item.StringId == stringId)?.IsSecondary ?? false;
	}

	public bool IsObjectiveCompleted(uint stringId) {
		return State.Objectives.Items.FirstOrDefault(item => item.StringId == stringId)?.State == ObjectiveState.Complete;
	}

	public bool IsObjectiveFailed(uint stringId) {
		return State.Objectives.Items.FirstOrDefault(item => item.StringId == stringId)?.State == ObjectiveState.Failed;
	}

	public bool IsObjectiveInList(uint stringId) {
		return State.Objectives.Items.Any(item => item.StringId == stringId);
	}

	public uint GetNumberObjectives() {
		return (uint)State.Objectives.Items.Count;
	}

	public uint GetObjectiveStringId(uint index) {
		return index < State.Objectives.Items.Count ? State.Objectives.Items[(int)index].StringId : 0;
	}

	public bool HasPlayerResigned(uint slotId) {
		ValidateSlotId(slotId);
		return State.PlayerResignedBySlot[SlotIndex(slotId)];
	}

	public void SetPlayerResignedBySlot(uint slotId) {
		ValidateSlotId(slotId);
		State.PlayerResignedBySlot[SlotIndex(slotId)] = true;
	}

	public void SetScriptUiControl(bool enabled) {
		State.ScriptUiControlEnabled = enabled;
	}

	public bool GetScriptUiControl() {
		return State.ScriptUiControlEnabled;
	}

	public void SetGlobalLighting(bool enabled) {
		State.GlobalLightingEnabled = enabled;
	}

	public bool IsGlobalLightingEnabled() {
		return State.GlobalLightingEnabled;
	}

	public uint GetNumUnitsBuilt(uint playerId) {
		return GetStats(playerId).NumUnitsBuilt;
	}

	public void SetNumUnitsBuilt(uint playerId, uint value) {
		GetStats(playerId).NumUnitsBuilt = value;
	}

	public uint GetUnitsDestroyed(uint playerId) {
		return GetStats(playerId).NumUnitsDestroyed;
	}

	public void SetUnitsDestroyed(uint playerId, uint value) {
		GetStats(playerId).NumUnitsDestroyed = value;
	}

	public uint GetUnitsLost(uint playerId) {
		return GetStats(playerId).NumUnitsLost;
	}

	public void SetUnitsLost(uint playerId, uint value) {
		GetStats(playerId).NumUnitsLost = value;
	}

	public uint GetNumPlatformsBuilt(uint playerId) {
		return GetStats(playerId).NumPlatformsBuilt;
	}

	public void SetNumPlatformsBuilt(uint playerId, uint value) {
		GetStats(playerId).NumPlatformsBuilt = value;
	}

	public uint GetNumAdmiralsBuilt(uint playerId) {
		return GetStats(playerId).NumAdmiralsBuilt;
	}

	public void SetNumAdmiralsBuilt(uint playerId, uint value) {
		GetStats(playerId).NumAdmiralsBuilt = value;
	}

	public uint GetPlatformsDestroyed(uint playerId) {
		return GetStats(playerId).NumPlatformsDestroyed;
	}

	public void SetPlatformsDestroyed(uint playerId, uint value) {
		GetStats(playerId).NumPlatformsDestroyed = value;
	}

	public uint GetPlatformsLost(uint playerId) {
		return GetStats(playerId).NumPlatformsLost;
	}

	public void SetPlatformsLost(uint playerId, uint value) {
		GetStats(playerId).NumPlatformsLost = value;
	}

	public uint GetUnitsConverted(uint playerId) {
		return GetStats(playerId).NumUnitsConverted;
	}

	public void SetUnitsConverted(uint playerId, uint value) {
		GetStats(playerId).NumUnitsConverted = value;
	}

	public uint GetPlatformsConverted(uint playerId) {
		return GetStats(playerId).NumPlatformsConverted;
	}

	public void SetPlatformsConverted(uint playerId, uint value) {
		GetStats(playerId).NumPlatformsConverted = value;
	}

	public uint GetNumJumpgatesControlled(uint playerId) {
		return GetStats(playerId).NumJumpgatesControlled;
	}

	public void SetNumJumpgatesControlled(uint playerId, uint value) {
		GetStats(playerId).NumJumpgatesControlled = value;
	}

	public uint GetGasGained(uint playerId) {
		return GetStats(playerId).GasGained;
	}

	public void SetGasGained(uint playerId, uint value) {
		GetStats(playerId).GasGained = value;
	}

	public uint GetMetalGained(uint playerId) {
		return GetStats(playerId).MetalGained;
	}

	public void SetMetalGained(uint playerId, uint value) {
		GetStats(playerId).MetalGained = value;
	}

	public uint GetCrewGained(uint playerId) {
		return GetStats(playerId).CrewGained;
	}

	public void SetCrewGained(uint playerId, uint value) {
		GetStats(playerId).CrewGained = value;
	}

	public uint GetResearchCompleted(uint playerId) {
		return GetStats(playerId).NumResearchComplete;
	}

	public void SetResearchCompleted(uint playerId, uint value) {
		GetStats(playerId).NumResearchComplete = value;
	}

	public float GetExploredSystemsRatio(uint playerId) {
		return GetStats(playerId).PercentSystemsExplored / 255f;
	}

	public void SetExploredSystemsRatio(uint playerId, float value) {
		GetStats(playerId).PercentSystemsExplored = (byte)Math.Clamp((int)(value * 255f), 0, byte.MaxValue);
	}

	public PlayerGameStats GetGameStats(uint playerId) {
		ValidatePlayerId(playerId);
		return State.GameStats[PlayerIndex(playerId)].Clone();
	}

	public void SetGameStats(uint playerId, PlayerGameStats stats) {
		ValidatePlayerId(playerId);
		ArgumentNullException.ThrowIfNull(stats);
		State.GameStats[PlayerIndex(playerId)] = stats.Clone();
	}

	public byte[] GetGameScores() {
		return State.PlayerScores.Skip(1).Take(ConquestGlobalsConstants.MaxPlayers).ToArray();
	}

	public void SetGameScores(ReadOnlySpan<byte> scores) {
		if (scores.Length != ConquestGlobalsConstants.MaxPlayers) {
			throw new ArgumentException("Game score buffer must contain one score per player.", nameof(scores));
		}

		for (var index = 0; index < scores.Length; index++) {
			State.PlayerScores[index + 1] = scores[index];
		}
	}

	public void SetPlayerScore(uint playerId, uint score) {
		ValidatePlayerId(playerId);
		State.PlayerScores[PlayerIndex(playerId)] = score > byte.MaxValue ? byte.MaxValue : (byte)score;
	}

	public uint GetPlayerScore(uint playerId) {
		ValidatePlayerId(playerId);
		return State.PlayerScores[PlayerIndex(playerId)];
	}

	public CqGameSettings GetGameSettings() {
		return CloneGameSettings(State.GameSettings);
	}

	public void SetGameSettings(CqGameSettings settings) {
		ArgumentNullException.ThrowIfNull(settings);
		CopyGameSettings(settings, State.GameSettings);
	}

	public void SetRegenMode(bool enabled) {
		State.GameSettings.RegenOn = enabled;
	}

	public bool IsSinglePlayer() {
		return !string.IsNullOrEmpty(State.ScriptLibraryName);
	}

	public string GetScriptName() {
		return State.ScriptLibraryName;
	}

	public void SetScriptName(string scriptName) {
		State.ScriptLibraryName = scriptName ?? string.Empty;
	}

	public string GetTerrainFilename() {
		return State.TerrainFilename;
	}

	public void SetTerrainFilename(string terrainFilename) {
		State.TerrainFilename = terrainFilename ?? string.Empty;
	}

	private uint CalculateTwoWayAllyMask(int playerId) {
		var result = (uint)State.OneWayAllianceMasks[playerId - 1];
		for (var index = 0; index < ConquestGlobalsConstants.MaxPlayers; index++) {
			if ((result & (1u << index)) == 0) {
				continue;
			}

			if ((State.OneWayAllianceMasks[index] & (1u << (playerId - 1))) == 0) {
				result &= ~(1u << index);
			}
		}

		return result;
	}

	private static PlayerTechLevel ToTechLevel(uint bitfield) {
		var result = 0;
		while (bitfield > 0) {
			bitfield >>= 1;
			result++;
		}

		return (PlayerTechLevel)result;
	}

	private static void ValidatePlayerId(uint playerId) {
		if (playerId == 0 || playerId > ConquestGlobalsConstants.MaxPlayers) {
			throw new ArgumentOutOfRangeException(nameof(playerId));
		}
	}

	private static void ValidateSlotId(uint slotId) {
		if (slotId >= ConquestGlobalsConstants.MaxPlayers) {
			throw new ArgumentOutOfRangeException(nameof(slotId));
		}
	}

	private static void ValidateRace(MRace race) {
		if (race is <= MRace.M_NO_RACE or > MRace.M_VYRIUM) {
			throw new ArgumentOutOfRangeException(nameof(race));
		}
	}

	private static int PlayerIndex(uint playerId) {
		return checked((int)playerId);
	}

	private static int OneWayIndex(uint playerId) {
		return checked((int)playerId - 1);
	}

	private static int SlotIndex(uint slotId) {
		return checked((int)slotId);
	}

	private PlayerGameStats GetStats(uint playerId) {
		ValidatePlayerId(playerId);
		return State.GameStats[PlayerIndex(playerId)];
	}

	private void ResetGameSettings() {
		var defaults = new CqGameSettings();
		CopyGameSettings(defaults, State.GameSettings);
	}

	private static CqGameSettings CloneGameSettings(CqGameSettings source) {
		var clone = new CqGameSettings();
		CopyGameSettings(source, clone);
		return clone;
	}

	private static void CopyGameSettings(CqGameSettings source, CqGameSettings destination) {
		destination.Version = source.Version;
		destination.GameType = source.GameType;
		destination.GameSpeed = source.GameSpeed;
		destination.RegenOn = source.RegenOn;
		destination.SpectatorsOn = source.SpectatorsOn;
		destination.LockDiplomacyOn = source.LockDiplomacyOn;
		destination.NumSystems = source.NumSystems;
		destination.Money = source.Money;
		destination.MapType = source.MapType;
		destination.TemplateType = source.TemplateType;
		destination.MapSize = source.MapSize;
		destination.Terrain = source.Terrain;
		destination.Units = source.Units;
		destination.Visibility = source.Visibility;
		destination.CommandLimit = source.CommandLimit;
		destination.ActiveSlots = source.ActiveSlots;
		destination.HostBusy = source.HostBusy;
		destination.StartCountdown = source.StartCountdown;
		for (var index = 0; index < destination.Slots.Length; index++) {
			var src = source.Slots[index];
			var dst = destination.Slots[index];
			dst.Type = src.Type;
			dst.ComputerChallenge = src.ComputerChallenge;
			dst.State = src.State;
			dst.Race = src.Race;
			dst.Color = src.Color;
			dst.Team = src.Team;
			dst.ZoneSeat = src.ZoneSeat;
			dst.Dpid = src.Dpid;
		}
	}
}
