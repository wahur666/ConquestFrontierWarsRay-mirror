using Xunit;

namespace ConquestFrontierWarsRay.Globals.Tests;

public sealed class ConquestGlobalsServiceTests {
	[Fact]
	public void Reset_InitializesDefaultPlayerState() {
		var service = new ConquestGlobalsService();

		Assert.Equal((uint)1, service.State.CurrentPlayerId);
		Assert.Equal((uint)1, service.State.HostPlayerId);
		Assert.Equal((uint)ConquestGlobalsConstants.BaseMaxGas, service.GetCurrentResource(1, MResourceType.M_GAS));
		Assert.Equal((uint)ConquestGlobalsConstants.BaseMaxMetal, service.GetCurrentResource(1, MResourceType.M_METAL));
		Assert.Equal((uint)ConquestGlobalsConstants.BaseMaxCrew, service.GetCurrentResource(1, MResourceType.M_CREW));
		Assert.Equal((uint)ConquestGlobalsConstants.DefaultMaxControlPointsPerPlayer, service.GetMaxControlPoints(1));
		Assert.True(service.IsGlobalLightingEnabled());
	}

	[Fact]
	public void AllianceRequiresMutualConsent() {
		var service = new ConquestGlobalsService();

		service.SetAlly(1, 2, ally: true);

		Assert.False(service.AreAllies(1, 2));

		service.SetAlly(2, 1, ally: true);

		Assert.True(service.AreAllies(1, 2));
		Assert.Equal(0b0000_0011u, service.GetAllyMask(1));
		Assert.Equal(0b0000_0011u, service.GetAllyMask(2));
	}

	[Fact]
	public void CreatePartIds_FollowsLegacyBitLayout() {
		var service = new ConquestGlobalsService();

		var partId = service.CreateNewPartId(3);
		var subordinateId = service.CreateSubordinatePartId();

		Assert.Equal(3u, service.GetPlayerFromPartId(partId));
		Assert.Equal(partId, service.GetOwnerFromPartId(subordinateId));
		Assert.NotEqual(partId, subordinateId);
		Assert.Equal(3u, service.GetPlayerFromPartId(subordinateId));
	}

	[Fact]
	public void SetCurrentTechLevel_ComputesUpgradeBandsFromBitfields() {
		var service = new ConquestGlobalsService();
		var techNode = new TechNode();
		var terran = techNode.GetRace(MRace.M_TERRAN);
		terran.Common = 0x00000007 | 0x000001E0 | 0x00000C00 | 0x00018000 | 0x00300000;
		terran.CommonExtra = 0x00000003 | 0x000000E0 | 0x00001C00 | 0x00038000 | 0x00700000;

		service.SetCurrentTechLevel(1, techNode);

		Assert.Equal(PlayerTechLevel.Level3, service.State.PlayerUpgradeLevels[1][(int)MRace.M_TERRAN].Engine);
		Assert.Equal(PlayerTechLevel.Level4, service.State.PlayerUpgradeLevels[1][(int)MRace.M_TERRAN].Shields);
		Assert.Equal(PlayerTechLevel.Level2, service.State.PlayerUpgradeLevels[1][(int)MRace.M_TERRAN].Fleet);
		Assert.Equal(PlayerTechLevel.Level3, service.State.PlayerUpgradeLevels[1][(int)MRace.M_TERRAN].Tanker);
	}

	[Fact]
	public void ObjectivesMaintainCompactOrderedList() {
		var service = new ConquestGlobalsService();

		service.AddObjective(1001, secondary: false);
		service.AddObjective(1002, secondary: true);
		service.AddObjective(1003, secondary: false);
		service.MarkObjectiveCompleted(1002);
		service.RemoveObjective(1001);

		Assert.Equal((uint)2, service.GetNumberObjectives());
		Assert.Equal(1002u, service.GetObjectiveStringId(0));
		Assert.Equal(1003u, service.GetObjectiveStringId(1));
		Assert.True(service.IsObjectiveCompleted(1002));
		Assert.True(service.IsObjectiveSecondary(1002));
		Assert.False(service.IsObjectiveInList(1001));
	}

	[Fact]
	public void ResetResourceMax_RestoresBaseCaps() {
		var service = new ConquestGlobalsService();

		service.SetMaxGas(1, 999);
		service.SetMaxMetal(1, 888);
		service.SetMaxCrew(1, 777);

		service.ResetResourceMax();

		Assert.Equal((uint)ConquestGlobalsConstants.BaseMaxGas, service.GetMaxGas(1));
		Assert.Equal((uint)ConquestGlobalsConstants.BaseMaxMetal, service.GetMaxMetal(1));
		Assert.Equal((uint)ConquestGlobalsConstants.BaseMaxCrew, service.GetMaxCrew(1));
	}

	[Fact]
	public void GameStatsAndScoresRoundTripAsManagedState() {
		var service = new ConquestGlobalsService();
		var stats = new PlayerGameStats {
			NumUnitsBuilt = 7,
			NumUnitsDestroyed = 5,
			NumPlatformsBuilt = 2,
			GasGained = 123,
			PercentSystemsExplored = 200,
		};

		service.SetGameStats(1, stats);
		service.SetResearchCompleted(1, 9);
		service.SetExploredSystemsRatio(1, 0.5f);
		service.SetGameScores([10, 20, 30, 40, 50, 60, 70, 80]);
		service.SetPlayerScore(1, 42);
		service.SetPlayerScore(2, 999);

		var roundTrip = service.GetGameStats(1);

		Assert.Equal((uint)7, service.GetNumUnitsBuilt(1));
		Assert.Equal((uint)5, service.GetUnitsDestroyed(1));
		Assert.Equal((uint)2, service.GetNumPlatformsBuilt(1));
		Assert.Equal((uint)123, service.GetGasGained(1));
		Assert.Equal((uint)9, service.GetResearchCompleted(1));
		Assert.InRange(service.GetExploredSystemsRatio(1), 0.49f, 0.51f);
		Assert.Equal((uint)42, service.GetPlayerScore(1));
		Assert.Equal((uint)255, service.GetPlayerScore(2));
		Assert.Equal((uint)7, roundTrip.NumUnitsBuilt);
		Assert.Equal(8, service.GetGameScores().Length);
	}

	[Fact]
	public void GameSettingsAndSinglePlayerFlagUseManagedState() {
		var service = new ConquestGlobalsService();
		var settings = new CqGameSettings {
			RegenOn = true,
			GameType = CqGameType.MissionDefined,
			ActiveSlots = 3,
		};
		settings.Slots[0].Type = CqSlotType.Human;
		settings.Slots[1].Type = CqSlotType.Computer;

		service.SetGameSettings(settings);
		service.SetScriptName("mission_script");

		var copy = service.GetGameSettings();

		Assert.True(copy.RegenOn);
		Assert.Equal((byte)3, copy.ActiveSlots);
		Assert.Equal(CqSlotType.Computer, copy.Slots[1].Type);
		Assert.True(service.IsSinglePlayer());
	}
}
