using ConquestFrontierWarsRay.Data.Models;

namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_ENDGAME {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Banner { get; init; } = new();
	public BUTTON_DATA Cont { get; init; } = new();
	public LISTBOX_DATA List { get; init; } = new();
	public STATIC_DATA StaticMenu { get; init; } = new();
	public TABCONTROL_DATA Tab { get; init; } = new();
	public GT_ENDGAME_OVERVIEW_TAB OverviewTab { get; init; } = new();
	public GT_ENDGAME_UNITS_TAB UnitsTab { get; init; } = new();
	public GT_ENDGAME_BUILDINGS_TAB BuildingsTab { get; init; } = new();
	public GT_ENDGAME_RESOURCE_TAB ResourceTab { get; init; } = new();
	public STATIC_DATA StaticPlayer { get; init; } = new();
	public STATIC_DATA[] StaticPlayerArray { get; init; } = [];
	public STATIC_DATA StaticTime { get; init; } = new();
	public STATIC_DATA StaticDescription { get; init; } = new();
}

public sealed class GT_ENDGAME_OVERVIEW_TAB {
	public STATIC_DATA[] StaticOverviewTitles { get; init; } = [];
	public STATIC_DATA[] StaticOverviewUnits { get; init; } = [];
	public STATIC_DATA[] StaticOverviewBuildings { get; init; } = [];
	public STATIC_DATA[] StaticOverviewResources { get; init; } = [];
	public STATIC_DATA[] StaticOverviewTotals { get; init; } = [];
}

public sealed class GT_ENDGAME_UNITS_TAB {
	public STATIC_DATA[] StaticUnitsTitles { get; init; } = [];
	public STATIC_DATA[] StaticUnitsMade { get; init; } = [];
	public STATIC_DATA[] StaticUnitsLost { get; init; } = [];
	public STATIC_DATA[] StaticUnitsKills { get; init; } = [];
	public STATIC_DATA[] StaticUnitsConverted { get; init; } = [];
	public STATIC_DATA[] StaticUnitsAdmirals { get; init; } = [];
	public STATIC_DATA[] StaticUnitsTotals { get; init; } = [];
}

public sealed class GT_ENDGAME_BUILDINGS_TAB {
	public STATIC_DATA[] StaticBuildingsTitles { get; init; } = [];
	public STATIC_DATA[] StaticBuildingsMade { get; init; } = [];
	public STATIC_DATA[] StaticBuildingsLost { get; init; } = [];
	public STATIC_DATA[] StaticBuildingsDestroyed { get; init; } = [];
	public STATIC_DATA[] StaticBuildingsConverted { get; init; } = [];
	public STATIC_DATA[] StaticBuildingsResearch { get; init; } = [];
	public STATIC_DATA[] StaticBuildingsTotals { get; init; } = [];
}

public sealed class GT_ENDGAME_RESOURCE_TAB {
	public STATIC_DATA[] StaticResourcesTitles { get; init; } = [];
	public STATIC_DATA[] StaticResourcesCrew { get; init; } = [];
	public STATIC_DATA[] StaticResourcesOre { get; init; } = [];
	public STATIC_DATA[] StaticResourcesGas { get; init; } = [];
	public STATIC_DATA[] StaticResourcesTotals { get; init; } = [];
}

public sealed class GT_MENU1 {
	public GT_MENU1_OPENING Opening { get; init; } = new();
	public GT_MENU1_SINGLEPLAYER_MENU SinglePlayerMenu { get; init; } = new();
	public GT_MENU1_SELECT_CAMPAIGN SelectCampaign { get; init; } = new();
	public GT_MENU1_SELECT_MISSION SelectMission { get; init; } = new();
	public GT_MENU1_NET_CONNECTIONS NetConnections { get; init; } = new();
	public GT_MENU1_IP_ADDRESS IpAddress { get; init; } = new();
	public GT_MENU1_GAME_ZONE GameZone { get; init; } = new();
	public GT_MENU1_NET_SESSIONS2 NetSessions2 { get; init; } = new();
	public GT_MENU1_MSHELL MShell { get; init; } = new();
	public GT_MENU1_MAP Map { get; init; } = new();
	public GT_MENU1_SLOTS Slots { get; init; } = new();
	public GT_MENU1_FINAL Final { get; init; } = new();
	public GT_MENU1_HELPMENU HelpMenu { get; init; } = new();
	public GT_MENU1_DEVICEMENU DeviceMenu { get; init; } = new();
}

public sealed class GT_MENU1_OPENING {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public BUTTON_DATA[] Buttons { get; init; } = [];
	public STATIC_DATA[] StaticLabels { get; init; } = [];
	public ANIMATE_DATA[] Animations { get; init; } = [];
	public STATIC_DATA StaticLegal { get; init; } = new();
}

public sealed class GT_MENU1_SINGLEPLAYER_MENU {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA StaticSingle { get; init; } = new();
	public STATIC_DATA StaticName { get; init; } = new();
	public BUTTON_DATA[] Buttons { get; init; } = [];
}

public sealed class GT_MENU1_SELECT_CAMPAIGN {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public STATIC_DATA StaticName { get; init; } = new();
	public BUTTON_DATA[] Buttons { get; init; } = [];
}

public sealed class GT_MENU1_SELECT_MISSION {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public LISTBOX_DATA List { get; init; } = new();
	public BUTTON_DATA Start { get; init; } = new();
	public BUTTON_DATA Ok { get; init; } = new();
	public BUTTON_DATA Cancel { get; init; } = new();
	public BUTTON_DATA ButtonUnlock { get; init; } = new();
	public STATIC_DATA StaticMission { get; init; } = new();
	public STATIC_DATA StaticHolder { get; init; } = new();
	public BUTTON_DATA[] ButtonMissions { get; init; } = [];
	public BUTTON_DATA[] ButtonMovies { get; init; } = [];
	public BUTTON_DATA ButtonBack { get; init; } = new();
	public STATIC_DATA StaticMovie { get; init; } = new();
	public STATIC_DATA StaticMissionTitle { get; init; } = new();
	public ANIMATE_DATA AnimSystem { get; init; } = new();
	public int[] NLineFrom { get; init; } = [];
	public int[] NMovieBeforeMission { get; init; } = [];
}

public sealed class GT_MENU1_NET_CONNECTIONS {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Description { get; init; } = new();
	public LISTBOX_DATA List { get; init; } = new();
	public BUTTON_DATA Next { get; init; } = new();
	public BUTTON_DATA Back { get; init; } = new();
	public STATIC_DATA StaticTitle { get; init; } = new();
	public BUTTON_DATA ButtonZone { get; init; } = new();
	public BUTTON_DATA ButtonWeb { get; init; } = new();
}

public sealed class GT_MENU1_IP_ADDRESS {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Description { get; init; } = new();
	public STATIC_DATA EnterIp { get; init; } = new();
	public STATIC_DATA EnterName { get; init; } = new();
	public STATIC_DATA StaticName { get; init; } = new();
	public STATIC_DATA StaticJoin { get; init; } = new();
	public STATIC_DATA StaticCreate { get; init; } = new();
	public BUTTON_DATA CheckJoin { get; init; } = new();
	public BUTTON_DATA CheckCreate { get; init; } = new();
	public BUTTON_DATA Next { get; init; } = new();
	public BUTTON_DATA Back { get; init; } = new();
	public COMBOBOX_DATA ComboboxIp { get; init; } = new();
}

public sealed class GT_MENU1_GAME_ZONE {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Description { get; init; } = new();
}

public sealed class GT_MENU1_NET_SESSIONS2 {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Description { get; init; } = new();
	public STATIC_DATA Version { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public LISTBOX_DATA List { get; init; } = new();
	public BUTTON_DATA Next { get; init; } = new();
	public BUTTON_DATA Back { get; init; } = new();
	public STATIC_DATA StaticGame { get; init; } = new();
	public STATIC_DATA StaticPlayers { get; init; } = new();
	public STATIC_DATA StaticSpeed { get; init; } = new();
	public STATIC_DATA StaticMap { get; init; } = new();
	public STATIC_DATA StaticResources { get; init; } = new();
}

public sealed class GT_MENU1_MSHELL {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public STATIC_DATA EnterChat { get; init; } = new();
	public STATIC_DATA Ipaddress { get; init; } = new();
	public LISTBOX_DATA ListChat { get; init; } = new();
	public EDIT_DATA EditChat { get; init; } = new();
}

public sealed class GT_MENU1_MAP {
	public RECT ScreenRect { get; init; } = new();
	public BUTTON_DATA MapType { get; init; } = new();
	public STATIC_DATA StaticGameType { get; init; } = new();
	public STATIC_DATA StaticSpeed { get; init; } = new();
	public STATIC_DATA StaticMoney { get; init; } = new();
	public STATIC_DATA StaticUnits { get; init; } = new();
	public STATIC_DATA StaticMapType { get; init; } = new();
	public STATIC_DATA StaticSize { get; init; } = new();
	public STATIC_DATA StaticTerrain { get; init; } = new();
	public STATIC_DATA StaticVisibility { get; init; } = new();
	public STATIC_DATA StaticBandwidth { get; init; } = new();
	public DROPDOWN_DATA DropGameType { get; init; } = new();
	public DROPDOWN_DATA DropSpeed { get; init; } = new();
	public DROPDOWN_DATA DropMoney { get; init; } = new();
	public DROPDOWN_DATA DropSize { get; init; } = new();
	public DROPDOWN_DATA DropTerrain { get; init; } = new();
	public DROPDOWN_DATA DropUnits { get; init; } = new();
	public DROPDOWN_DATA DropVisibility { get; init; } = new();
	public DROPDOWN_DATA BandWidthOption { get; init; } = new();
	public SLIDER_DATA SliderSpeed { get; init; } = new();
	public SLIDER_DATA SliderCmdPoints { get; init; } = new();
	public STATIC_DATA StaticSpectator { get; init; } = new();
	public STATIC_DATA StaticDiplomacy { get; init; } = new();
	public BUTTON_DATA PushSpectator { get; init; } = new();
	public BUTTON_DATA PushDiplomacy { get; init; } = new();
	public STATIC_DATA StaticDifficulty { get; init; } = new();
	public STATIC_DATA StaticEasy { get; init; } = new();
	public STATIC_DATA StaticAverage { get; init; } = new();
	public STATIC_DATA StaticHard { get; init; } = new();
	public BUTTON_DATA PushEasy { get; init; } = new();
	public BUTTON_DATA PushAverage { get; init; } = new();
	public BUTTON_DATA PushHard { get; init; } = new();
	public STATIC_DATA StaticLockSettings { get; init; } = new();
	public BUTTON_DATA PushLockSettings { get; init; } = new();
	public STATIC_DATA StaticSystems { get; init; } = new();
	public DROPDOWN_DATA DropSystems { get; init; } = new();
	public STATIC_DATA StaticCmdPoints { get; init; } = new();
	public STATIC_DATA StaticCmdPointsDisplay { get; init; } = new();
}

public sealed class GT_MENU1_SLOTS {
	public RECT ScreenRect { get; init; } = new();
	public DROPDOWN_DATA[] DropSlots { get; init; } = [];
	public DROPDOWN_DATA[] DropTeams { get; init; } = [];
	public DROPDOWN_DATA[] DropRaces { get; init; } = [];
	public DROPDOWN_DATA[] DropPlayers { get; init; } = [];
	public STATIC_DATA[] StaticNames { get; init; } = [];
	public STATIC_DATA[] StaticPings { get; init; } = [];
	public string[] TerranComputerNames { get; init; } = [];
	public string[] MantisComputerNames { get; init; } = [];
	public string[] SolarianComputerNames { get; init; } = [];
}

public sealed class GT_MENU1_FINAL {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA StaticState { get; init; } = new();
	public STATIC_DATA StaticName { get; init; } = new();
	public STATIC_DATA StaticColor { get; init; } = new();
	public STATIC_DATA StaticRace { get; init; } = new();
	public STATIC_DATA StaticTeam { get; init; } = new();
	public STATIC_DATA StaticPing { get; init; } = new();
	public STATIC_DATA Description { get; init; } = new();
	public STATIC_DATA StaticAccept { get; init; } = new();
	public BUTTON_DATA Accept { get; init; } = new();
	public BUTTON_DATA Start { get; init; } = new();
	public BUTTON_DATA Cancel { get; init; } = new();
	public STATIC_DATA StaticCountdown { get; init; } = new();
}

public sealed class GT_MENU1_HELPMENU {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public STATIC_DATA StaticConquest { get; init; } = new();
	public STATIC_DATA StaticVersion { get; init; } = new();
	public STATIC_DATA StaticNumber { get; init; } = new();
	public BUTTON_DATA ButtonOk { get; init; } = new();
	public STATIC_DATA StaticProductId { get; init; } = new();
	public STATIC_DATA StaticProductNumber { get; init; } = new();
	public STATIC_DATA StaticLegal { get; init; } = new();
	public BUTTON_DATA ButtonCredits { get; init; } = new();
}

public sealed class GT_MENU1_DEVICEMENU {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA StaticBackground { get; init; } = new();
	public STATIC_DATA StaticTitle { get; init; } = new();
	public STATIC_DATA StaticPickDevice { get; init; } = new();
	public LISTBOX_DATA ListDevices { get; init; } = new();
	public BUTTON_DATA ButtonOk { get; init; } = new();
}

public sealed class GT_TOOLBAR {
	public string VfxShapeType { get; init; } = string.Empty;
	public string[] VfxToolBar { get; init; } = [];
	public RECT ContextRect { get; init; } = new();
	public RECT[] SysmapRect { get; init; } = [];
	public RECT[] SectorMapRect { get; init; } = [];
	public uint TopBarX { get; init; }
	public uint TopBarY { get; init; }
	public GT_TOOLBAR_COMMON[] Common { get; init; } = [];
	public GT_TOOLBAR_NONE None { get; init; } = new();
	public GT_TOOLBAR_FABRICATOR Fabricator { get; init; } = new();
	public GT_TOOLBAR_FABRICATOR MWeaver { get; init; } = new();
	public GT_TOOLBAR_FABRICATOR SForger { get; init; } = new();
	public GT_TOOLBAR_FABRICATOR VShaper { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL LIndustrial { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL Hq { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL HIndustrial { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL MCocoon { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL MNiad { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL SAcropolis { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL SPavilion { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL SSanctum { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL SGreaterPavilion { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL VLocus { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL VCompiler { get; init; } = new();
	public GT_TOOLBAR_LINDUSTRIAL VFormulator { get; init; } = new();
	public GT_TOOLBAR_GENERIC Turret { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MPlantation { get; init; } = new();
	public GT_TOOLBAR_RESEARCH Proplab { get; init; } = new();
	public GT_TOOLBAR_RESEARCH Ballistics { get; init; } = new();
	public GT_TOOLBAR_RESEARCH AdvHull { get; init; } = new();
	public GT_TOOLBAR_RESEARCH AwsLab { get; init; } = new();
	public GT_TOOLBAR_RESEARCH Lrsensor { get; init; } = new();
	public GT_TOOLBAR_RESEARCH Hanger { get; init; } = new();
	public GT_TOOLBAR_RESEARCH Weapons { get; init; } = new();
	public GT_TOOLBAR_RESEARCH Displacement { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MBlastFurnace { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MExplosivesRange { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MCarrionRoost { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MBioForge { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MFusionMill { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MCarpacePlant { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MHybridCenter { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MPlasmaSpitter { get; init; } = new();
	public GT_TOOLBAR_RESEARCH SHelionVeil { get; init; } = new();
	public GT_TOOLBAR_RESEARCH SXenoChamber { get; init; } = new();
	public GT_TOOLBAR_RESEARCH SAnvil { get; init; } = new();
	public GT_TOOLBAR_RESEARCH SMunitionsAnnex { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MEyeStock { get; init; } = new();
	public GT_TOOLBAR_RESEARCH MMutationColony { get; init; } = new();
	public GT_TOOLBAR_RESEARCH STurbineDock { get; init; } = new();
	public GT_TOOLBAR_RESEARCH SBunker { get; init; } = new();
	public GT_TOOLBAR_RESEARCH VHatchery { get; init; } = new();
	public GT_TOOLBAR_RESEARCH VCochleaDish { get; init; } = new();
	public GT_TOOLBAR_RESEARCH VClawOfVyrie { get; init; } = new();
	public GT_TOOLBAR_RESEARCH VHammerOfVyrie { get; init; } = new();
	public GT_TOOLBAR_RESEARCH VEyeOfVyrie { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES MThripid { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES Academy { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES Refinery { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES MCollector { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES MGreaterCollector { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES MWarlordTraining { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES SSentinalTower { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES SCitidel { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES THeavyRefinery { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES TSuperHeavyRefinery { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES SOxidator { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES VCoalescer { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES VGudgeon { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES VTempleOfVyrie { get; init; } = new();
	public GT_TOOLBAR_BUILD_RES Outpost { get; init; } = new();
	public GT_TOOLBAR_FLEET[] Fleets { get; init; } = [];
	public GT_TOOLBAR_WARTURRET[] WarTurrets { get; init; } = [];
	public GT_TOOLBAR_INDIVIDUAL[] Individuals { get; init; } = [];
	public GT_TOOLBAR_GROUP[] Groups { get; init; } = [];
}

public sealed class GT_TOOLBAR_COMMON {
	public HOTBUTTON_DATA RotateLeft { get; init; } = new();
	public HOTBUTTON_DATA ReturnDefaultView { get; init; } = new();
	public HOTBUTTON_DATA RotateRight { get; init; } = new();
	public HOTBUTTON_DATA Minimize { get; init; } = new();
	public HOTBUTTON_DATA Restore { get; init; } = new();
	public HOTBUTTON_DATA Options { get; init; } = new();
	public HOTBUTTON_DATA Chat { get; init; } = new();
	public HOTBUTTON_DATA Go { get; init; } = new();
	public HOTBUTTON_DATA StarMap { get; init; } = new();
	public HOTBUTTON_DATA ExitSysMap { get; init; } = new();
	public HOTBUTTON_DATA HpDiplomacy { get; init; } = new();
	public HOTBUTTON_DATA HpResearch { get; init; } = new();
	public HOTBUTTON_DATA HpFleetOfficer { get; init; } = new();
	public HOTBUTTON_DATA HpIndustrial { get; init; } = new();
	public HOTBUTTON_DATA HpIdleCivilian { get; init; } = new();
	public HOTBUTTON_DATA MissionObjectives { get; init; } = new();
	public STATIC_DATA Gas { get; init; } = new();
	public STATIC_DATA Metal { get; init; } = new();
	public STATIC_DATA Crew { get; init; } = new();
	public STATIC_DATA CommandPts { get; init; } = new();
	public STATIC_DATA Shipclass { get; init; } = new();
	public ICON_DATA InSupply { get; init; } = new();
	public ICON_DATA NotInSupply { get; init; } = new();
}

public sealed class GT_TOOLBAR_NONE {
	public STATIC_DATA Nothing { get; init; } = new();
}

public sealed class GT_TOOLBAR_FABRICATOR {
	public uint Type { get; init; }
	public HOTBUTTON_DATA Sell { get; init; } = new();
	public HOTBUTTON_DATA Stop { get; init; } = new();
	public HOTBUTTON_DATA Repair { get; init; } = new();
	public EDIT_DATA Shipname { get; init; } = new();
	public STATIC_DATA Hull { get; init; } = new();
	public TABCONTROL_DATA FabTab { get; init; } = new();
	public GT_TOOLBAR_FABRICATOR_TAB1 BasicTab { get; init; } = new();
	public GT_TOOLBAR_FABRICATOR_TAB2 AdvancedTab { get; init; } = new();
	public GT_TOOLBAR_FABRICATOR_TAB4 DefenceTab { get; init; } = new();
	public GT_TOOLBAR_FABRICATOR_TAB5 MoonTab { get; init; } = new();
	public GT_TOOLBAR_FABRICATOR_TAB3 StatisticsTab { get; init; } = new();
}

public sealed class GT_TOOLBAR_LINDUSTRIAL {
	public uint Type { get; init; }
	public ICON_DATA InSupply { get; init; } = new();
	public ICON_DATA NotInSupply { get; init; } = new();
	public HOTBUTTON_DATA Rally { get; init; } = new();
	public HOTBUTTON_DATA Stop { get; init; } = new();
	public STATIC_DATA Hull { get; init; } = new();
	public STATIC_DATA MetalStorage { get; init; } = new();
	public STATIC_DATA GasStorage { get; init; } = new();
	public STATIC_DATA CrewStorage { get; init; } = new();
	public STATIC_DATA Location { get; init; } = new();
	public STATIC_DATA DisabledText { get; init; } = new();
	public BUILDBUTTON_DATA Build0 { get; init; } = new();
	public BUILDBUTTON_DATA Build1 { get; init; } = new();
	public BUILDBUTTON_DATA Build2 { get; init; } = new();
	public BUILDBUTTON_DATA Build3 { get; init; } = new();
	public BUILDBUTTON_DATA Build4 { get; init; } = new();
	public BUILDBUTTON_DATA Build5 { get; init; } = new();
	public BUILDBUTTON_DATA Build6 { get; init; } = new();
	public BUILDBUTTON_DATA Build7 { get; init; } = new();
	public BUILDBUTTON_DATA Build8 { get; init; } = new();
	public BUILDBUTTON_DATA Build9 { get; init; } = new();
	public QUEUECONTROL_DATA BuildQueue { get; init; } = new();
}

public sealed class GT_TOOLBAR_GENERIC {
	public uint Type { get; init; }
	public ICON_DATA InSupply { get; init; } = new();
	public ICON_DATA NotInSupply { get; init; } = new();
	public STATIC_DATA Hull { get; init; } = new();
	public STATIC_DATA Supplies { get; init; } = new();
	public STATIC_DATA Location { get; init; } = new();
	public STATIC_DATA DisabledText { get; init; } = new();
}

public sealed class GT_TOOLBAR_RESEARCH {
	public uint Type { get; init; }
	public ICON_DATA InSupply { get; init; } = new();
	public ICON_DATA NotInSupply { get; init; } = new();
	public HOTBUTTON_DATA Stop { get; init; } = new();
	public STATIC_DATA Hull { get; init; } = new();
	public STATIC_DATA Supplies { get; init; } = new();
	public STATIC_DATA MetalStorage { get; init; } = new();
	public STATIC_DATA GasStorage { get; init; } = new();
	public STATIC_DATA CrewStorage { get; init; } = new();
	public STATIC_DATA Location { get; init; } = new();
	public STATIC_DATA DisabledText { get; init; } = new();
	public RESEARCHBUTTON_DATA Research0 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research1 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research2 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research3 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research4 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research5 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research6 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research7 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research8 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research9 { get; init; } = new();
	public QUEUECONTROL_DATA BuildQueue { get; init; } = new();
}

public sealed class GT_TOOLBAR_BUILD_RES {
	public uint Type { get; init; }
	public ICON_DATA InSupply { get; init; } = new();
	public ICON_DATA NotInSupply { get; init; } = new();
	public HOTBUTTON_DATA Stop { get; init; } = new();
	public HOTBUTTON_DATA Rally { get; init; } = new();
	public HOTBUTTON_DATA NoAuto { get; init; } = new();
	public HOTBUTTON_DATA AutoOre { get; init; } = new();
	public HOTBUTTON_DATA AutoGas { get; init; } = new();
	public STATIC_DATA Hull { get; init; } = new();
	public STATIC_DATA MetalStorage { get; init; } = new();
	public STATIC_DATA GasStorage { get; init; } = new();
	public STATIC_DATA CrewStorage { get; init; } = new();
	public STATIC_DATA Location { get; init; } = new();
	public STATIC_DATA DisabledText { get; init; } = new();
	public RESEARCHBUTTON_DATA Research0 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research1 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research2 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research3 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research4 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research5 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research6 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research7 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research8 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research9 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research10 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research11 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research12 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research13 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research14 { get; init; } = new();
	public RESEARCHBUTTON_DATA Research15 { get; init; } = new();
	public BUILDBUTTON_DATA Build0 { get; init; } = new();
	public BUILDBUTTON_DATA Build1 { get; init; } = new();
	public BUILDBUTTON_DATA Build2 { get; init; } = new();
	public BUILDBUTTON_DATA Build3 { get; init; } = new();
	public BUILDBUTTON_DATA Build4 { get; init; } = new();
	public BUILDBUTTON_DATA Build5 { get; init; } = new();
	public QUEUECONTROL_DATA BuildQueue { get; init; } = new();
}

public sealed class GT_TOOLBAR_FLEET {
	public uint Type { get; init; }
	public HOTBUTTON_DATA Escort { get; init; } = new();
	public HOTBUTTON_DATA Patrol { get; init; } = new();
	public HOTBUTTON_DATA Stop { get; init; } = new();
	public HOTBUTTON_DATA AttackPosition { get; init; } = new();
	public HOTBUTTON_DATA TacticPeace { get; init; } = new();
	public HOTBUTTON_DATA TacticStandGround { get; init; } = new();
	public HOTBUTTON_DATA TacticDefend { get; init; } = new();
	public HOTBUTTON_DATA TacticSeek { get; init; } = new();
	public MULTIHOTBUTTON_DATA Formation1 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Formation2 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Formation3 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Formation4 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Formation5 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Formation6 { get; init; } = new();
	public TABCONTROL_DATA FabTab { get; init; } = new();
	public GT_TOOLBAR_FLEET_ORDER_TAB OrderTab { get; init; } = new();
	public GT_TOOLBAR_FLEET_SIL_TAB SilTab { get; init; } = new();
	public GT_TOOLBAR_FLEET_STAT_TAB StatTab { get; init; } = new();
	public GT_TOOLBAR_FLEET_KIT_TAB KitTab { get; init; } = new();
}

public sealed class GT_TOOLBAR_FLEET_ORDER_TAB {
	public MULTIHOTBUTTON_DATA AdmiralHead { get; init; } = new();
	public STATIC_DATA ONamearea { get; init; } = new();
	public STATIC_DATA OKills { get; init; } = new();
	public STATIC_DATA OHull { get; init; } = new();
	public HOTBUTTON_DATA Order1 { get; init; } = new();
	public HOTBUTTON_DATA Order2 { get; init; } = new();
	public HOTBUTTON_DATA Order3 { get; init; } = new();
	public HOTBUTTON_DATA Order4 { get; init; } = new();
	public HOTBUTTON_DATA Order5 { get; init; } = new();
	public HOTBUTTON_DATA Order6 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders0 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders1 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders2 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders3 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders4 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders5 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders6 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders7 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders8 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders9 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders10 { get; init; } = new();
	public MULTIHOTBUTTON_DATA SpecialOrders11 { get; init; } = new();
}

public sealed class GT_TOOLBAR_FLEET_SIL_TAB {
	public SHIPSILBUTTON_DATA Ship0 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship1 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship2 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship3 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship4 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship5 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship6 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship7 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship8 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship9 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship10 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship11 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship12 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship13 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship14 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship15 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship16 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship17 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship18 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship19 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship20 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship21 { get; init; } = new();
}

public sealed class GT_TOOLBAR_FLEET_STAT_TAB {
	public STATIC_DATA Namearea { get; init; } = new();
	public STATIC_DATA Hull { get; init; } = new();
	public STATIC_DATA Kills { get; init; } = new();
	public HOTSTATIC_DATA Techarmor { get; init; } = new();
	public HOTSTATIC_DATA Techsupply { get; init; } = new();
	public HOTSTATIC_DATA Techengine { get; init; } = new();
	public HOTSTATIC_DATA Techsheild { get; init; } = new();
	public HOTSTATIC_DATA Techweapon { get; init; } = new();
	public HOTSTATIC_DATA Techsensors { get; init; } = new();
	public HOTSTATIC_DATA Techspecial { get; init; } = new();
}

public sealed class GT_TOOLBAR_FLEET_KIT_TAB {
	public STATIC_DATA KNamearea { get; init; } = new();
	public STATIC_DATA KHull { get; init; } = new();
	public STATIC_DATA KKills { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit0 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit1 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit2 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit3 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit4 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit5 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit6 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit7 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit8 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit9 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit10 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit11 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit12 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Kit13 { get; init; } = new();
	public MULTIHOTBUTTON_DATA KitDisplay0 { get; init; } = new();
	public MULTIHOTBUTTON_DATA KitDisplay1 { get; init; } = new();
	public QUEUECONTROL_DATA KitQueue { get; init; } = new();
}

public sealed class GT_TOOLBAR_WARTURRET {
	public uint Type { get; init; }
	public ICON_DATA InSupply { get; init; } = new();
	public ICON_DATA NotInSupply { get; init; } = new();
	public STATIC_DATA Hull { get; init; } = new();
	public STATIC_DATA Supplies { get; init; } = new();
	public STATIC_DATA Location { get; init; } = new();
	public MULTIHOTBUTTON_DATA Specialweapon { get; init; } = new();
	public HOTBUTTON_DATA FighterStanceNormal { get; init; } = new();
	public HOTBUTTON_DATA FighterStancePatrol { get; init; } = new();
	public HOTSTATIC_DATA Techarmor { get; init; } = new();
	public HOTSTATIC_DATA Techsupply { get; init; } = new();
	public HOTSTATIC_DATA Techsheild { get; init; } = new();
	public HOTSTATIC_DATA Techweapon { get; init; } = new();
	public HOTSTATIC_DATA Techsensors { get; init; } = new();
	public HOTSTATIC_DATA Techspecial { get; init; } = new();
}

public sealed class GT_TOOLBAR_INDIVIDUAL {
	public uint Type { get; init; }
	public HOTBUTTON_DATA Patrol { get; init; } = new();
	public HOTBUTTON_DATA Stop { get; init; } = new();
	public HOTBUTTON_DATA Escort { get; init; } = new();
	public HOTBUTTON_DATA StanceAttack { get; init; } = new();
	public HOTBUTTON_DATA StanceDefend { get; init; } = new();
	public HOTBUTTON_DATA StanceStand { get; init; } = new();
	public HOTBUTTON_DATA StanceStop { get; init; } = new();
	public HOTBUTTON_DATA SupplyStanceAuto { get; init; } = new();
	public HOTBUTTON_DATA SupplyStanceNoAuto { get; init; } = new();
	public HOTBUTTON_DATA SupplyStanceResupplyOnly { get; init; } = new();
	public HOTBUTTON_DATA Cloak { get; init; } = new();
	public HOTBUTTON_DATA AttackPosition { get; init; } = new();
	public HOTBUTTON_DATA FighterStanceNormal { get; init; } = new();
	public HOTBUTTON_DATA FighterStancePatrol { get; init; } = new();
	public HOTBUTTON_DATA EjectArtifact { get; init; } = new();
	public MULTIHOTBUTTON_DATA Specialweapon { get; init; } = new();
	public MULTIHOTBUTTON_DATA Specialweapon1 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Specialweapon2 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Artifact { get; init; } = new();
	public EDIT_DATA Namearea { get; init; } = new();
	public STATIC_DATA Hull { get; init; } = new();
	public STATIC_DATA Supplies { get; init; } = new();
	public STATIC_DATA Kills { get; init; } = new();
	public STATIC_DATA Metal { get; init; } = new();
	public STATIC_DATA Gas { get; init; } = new();
	public HOTSTATIC_DATA Techarmor { get; init; } = new();
	public HOTSTATIC_DATA Techsupply { get; init; } = new();
	public HOTSTATIC_DATA Techengine { get; init; } = new();
	public HOTSTATIC_DATA Techsheild { get; init; } = new();
	public HOTSTATIC_DATA Techweapon { get; init; } = new();
	public HOTSTATIC_DATA Techsensors { get; init; } = new();
	public HOTSTATIC_DATA Techspecial { get; init; } = new();
}

public sealed class GT_TOOLBAR_GROUP {
	public uint Type { get; init; }
	public MULTIHOTBUTTON_DATA Specialweapon { get; init; } = new();
	public MULTIHOTBUTTON_DATA Specialweapon1 { get; init; } = new();
	public MULTIHOTBUTTON_DATA Specialweapon2 { get; init; } = new();
	public HOTBUTTON_DATA Escort { get; init; } = new();
	public HOTBUTTON_DATA Patrol { get; init; } = new();
	public HOTBUTTON_DATA Stop { get; init; } = new();
	public HOTBUTTON_DATA Cloak { get; init; } = new();
	public HOTBUTTON_DATA AttackPosition { get; init; } = new();
	public HOTBUTTON_DATA StanceAttack { get; init; } = new();
	public HOTBUTTON_DATA StanceDefend { get; init; } = new();
	public HOTBUTTON_DATA StanceStand { get; init; } = new();
	public HOTBUTTON_DATA StanceStop { get; init; } = new();
	public HOTBUTTON_DATA SupplyStanceAuto { get; init; } = new();
	public HOTBUTTON_DATA SupplyStanceNoAuto { get; init; } = new();
	public HOTBUTTON_DATA SupplyStanceResupplyOnly { get; init; } = new();
	public HOTBUTTON_DATA FighterStanceNormal { get; init; } = new();
	public HOTBUTTON_DATA FighterStancePatrol { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship0 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship1 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship2 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship3 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship4 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship5 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship6 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship7 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship8 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship9 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship10 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship11 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship12 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship13 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship14 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship15 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship16 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship17 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship18 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship19 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship20 { get; init; } = new();
	public SHIPSILBUTTON_DATA Ship21 { get; init; } = new();
}

public sealed class GT_TOOLBAR_FABRICATOR_TAB1 {
	public BUILDBUTTON_DATA Plat0 { get; init; } = new();
	public BUILDBUTTON_DATA Plat1 { get; init; } = new();
	public BUILDBUTTON_DATA Plat2 { get; init; } = new();
	public BUILDBUTTON_DATA Plat3 { get; init; } = new();
	public BUILDBUTTON_DATA Plat4 { get; init; } = new();
	public BUILDBUTTON_DATA Plat5 { get; init; } = new();
	public BUILDBUTTON_DATA Plat6 { get; init; } = new();
	public BUILDBUTTON_DATA Plat7 { get; init; } = new();
	public BUILDBUTTON_DATA Plat8 { get; init; } = new();
	public BUILDBUTTON_DATA Plat9 { get; init; } = new();
	public BUILDBUTTON_DATA Plat10 { get; init; } = new();
	public BUILDBUTTON_DATA Plat11 { get; init; } = new();
	public BUILDBUTTON_DATA Plat12 { get; init; } = new();
	public BUILDBUTTON_DATA Plat13 { get; init; } = new();
	public BUILDBUTTON_DATA Plat14 { get; init; } = new();
	public BUILDBUTTON_DATA Plat15 { get; init; } = new();
}

public sealed class GT_TOOLBAR_FABRICATOR_TAB2 {
	public BUILDBUTTON_DATA Plat16 { get; init; } = new();
	public BUILDBUTTON_DATA Plat17 { get; init; } = new();
	public BUILDBUTTON_DATA Plat18 { get; init; } = new();
	public BUILDBUTTON_DATA Plat19 { get; init; } = new();
	public BUILDBUTTON_DATA Plat20 { get; init; } = new();
	public BUILDBUTTON_DATA Plat21 { get; init; } = new();
	public BUILDBUTTON_DATA Plat22 { get; init; } = new();
	public BUILDBUTTON_DATA Plat23 { get; init; } = new();
	public BUILDBUTTON_DATA Plat24 { get; init; } = new();
	public BUILDBUTTON_DATA Plat25 { get; init; } = new();
	public BUILDBUTTON_DATA Plat26 { get; init; } = new();
	public BUILDBUTTON_DATA Plat27 { get; init; } = new();
	public BUILDBUTTON_DATA Plat28 { get; init; } = new();
	public BUILDBUTTON_DATA Plat29 { get; init; } = new();
	public BUILDBUTTON_DATA Plat30 { get; init; } = new();
	public BUILDBUTTON_DATA Plat31 { get; init; } = new();
}

public sealed class GT_TOOLBAR_FABRICATOR_TAB4 {
	public BUILDBUTTON_DATA Plat32 { get; init; } = new();
	public BUILDBUTTON_DATA Plat33 { get; init; } = new();
	public BUILDBUTTON_DATA Plat34 { get; init; } = new();
	public BUILDBUTTON_DATA Plat35 { get; init; } = new();
	public BUILDBUTTON_DATA Plat36 { get; init; } = new();
	public BUILDBUTTON_DATA Plat37 { get; init; } = new();
	public BUILDBUTTON_DATA Plat38 { get; init; } = new();
	public BUILDBUTTON_DATA Plat39 { get; init; } = new();
	public BUILDBUTTON_DATA Plat40 { get; init; } = new();
	public BUILDBUTTON_DATA Plat41 { get; init; } = new();
	public BUILDBUTTON_DATA Plat42 { get; init; } = new();
	public BUILDBUTTON_DATA Plat43 { get; init; } = new();
	public BUILDBUTTON_DATA Plat44 { get; init; } = new();
	public BUILDBUTTON_DATA Plat45 { get; init; } = new();
	public BUILDBUTTON_DATA Plat46 { get; init; } = new();
	public BUILDBUTTON_DATA Plat47 { get; init; } = new();
}

public sealed class GT_TOOLBAR_FABRICATOR_TAB5 {
	public BUILDBUTTON_DATA Plat48 { get; init; } = new();
	public BUILDBUTTON_DATA Plat49 { get; init; } = new();
	public BUILDBUTTON_DATA Plat50 { get; init; } = new();
	public BUILDBUTTON_DATA Plat51 { get; init; } = new();
	public BUILDBUTTON_DATA Plat52 { get; init; } = new();
	public BUILDBUTTON_DATA Plat53 { get; init; } = new();
	public BUILDBUTTON_DATA Plat54 { get; init; } = new();
	public BUILDBUTTON_DATA Plat55 { get; init; } = new();
	public BUILDBUTTON_DATA Plat56 { get; init; } = new();
	public BUILDBUTTON_DATA Plat57 { get; init; } = new();
	public BUILDBUTTON_DATA Plat58 { get; init; } = new();
	public BUILDBUTTON_DATA Plat59 { get; init; } = new();
	public BUILDBUTTON_DATA Plat60 { get; init; } = new();
	public BUILDBUTTON_DATA Plat61 { get; init; } = new();
	public BUILDBUTTON_DATA Plat62 { get; init; } = new();
	public BUILDBUTTON_DATA Plat63 { get; init; } = new();
}

public sealed class GT_TOOLBAR_FABRICATOR_TAB3 {
	public HOTSTATIC_DATA Techarmor { get; init; } = new();
	public HOTSTATIC_DATA Techengine { get; init; } = new();
	public HOTSTATIC_DATA Techsheild { get; init; } = new();
	public HOTSTATIC_DATA Techsensors { get; init; } = new();
}
