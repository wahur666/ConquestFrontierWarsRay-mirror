using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_ENDGAMETypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_ENDGAME", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("banner"),
		UtfDbTypeLayouts.ButtonData("cont"),
		UtfDbTypeLayouts.ListboxData("list"),
		UtfDbTypeLayouts.StaticData("static_menu"),
		UtfDbTypeLayouts.TabcontrolData("tab"),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("overview_tab.static_overview_titles"), 4),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("overview_tab.static_overview_units"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("overview_tab.static_overview_buildings"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("overview_tab.static_overview_resources"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("overview_tab.static_overview_totals"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("units_tab.static_units_titles"), 6),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("units_tab.static_units_made"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("units_tab.static_units_lost"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("units_tab.static_units_kills"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("units_tab.static_units_converted"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("units_tab.static_units_admirals"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("units_tab.static_units_totals"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("buildings_tab.static_buildings_titles"), 6),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("buildings_tab.static_buildings_made"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("buildings_tab.static_buildings_lost"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("buildings_tab.static_buildings_destroyed"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("buildings_tab.static_buildings_converted"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("buildings_tab.static_buildings_research"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("buildings_tab.static_buildings_totals"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("resource_tab.static_resources_titles"), 4),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("resource_tab.static_resources_crew"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("resource_tab.static_resources_ore"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("resource_tab.static_resources_gas"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("resource_tab.static_resources_totals"), 8),
		UtfDbTypeLayouts.StaticData("static_player"),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_player_array"), 8),
		UtfDbTypeLayouts.StaticData("static_time"),
		UtfDbTypeLayouts.StaticData("static_description")
	]);

	public string TypeName => "GT_ENDGAME";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_ENDGAME ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_ENDGAME {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Banner = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Cont = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			List = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset),
			StaticMenu = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Tab = UtfDbTypeParserHelpers.ParseTabcontrolData(rawData, ref offset),
			OverviewTab = new GT_ENDGAME_OVERVIEW_TAB {
				StaticOverviewTitles = ReadStaticArray(rawData, ref offset, 4),
				StaticOverviewUnits = ReadStaticArray(rawData, ref offset, 8),
				StaticOverviewBuildings = ReadStaticArray(rawData, ref offset, 8),
				StaticOverviewResources = ReadStaticArray(rawData, ref offset, 8),
				StaticOverviewTotals = ReadStaticArray(rawData, ref offset, 8)
			},
			UnitsTab = new GT_ENDGAME_UNITS_TAB {
				StaticUnitsTitles = ReadStaticArray(rawData, ref offset, 6),
				StaticUnitsMade = ReadStaticArray(rawData, ref offset, 8),
				StaticUnitsLost = ReadStaticArray(rawData, ref offset, 8),
				StaticUnitsKills = ReadStaticArray(rawData, ref offset, 8),
				StaticUnitsConverted = ReadStaticArray(rawData, ref offset, 8),
				StaticUnitsAdmirals = ReadStaticArray(rawData, ref offset, 8),
				StaticUnitsTotals = ReadStaticArray(rawData, ref offset, 8)
			},
			BuildingsTab = new GT_ENDGAME_BUILDINGS_TAB {
				StaticBuildingsTitles = ReadStaticArray(rawData, ref offset, 6),
				StaticBuildingsMade = ReadStaticArray(rawData, ref offset, 8),
				StaticBuildingsLost = ReadStaticArray(rawData, ref offset, 8),
				StaticBuildingsDestroyed = ReadStaticArray(rawData, ref offset, 8),
				StaticBuildingsConverted = ReadStaticArray(rawData, ref offset, 8),
				StaticBuildingsResearch = ReadStaticArray(rawData, ref offset, 8),
				StaticBuildingsTotals = ReadStaticArray(rawData, ref offset, 8)
			},
			ResourceTab = new GT_ENDGAME_RESOURCE_TAB {
				StaticResourcesTitles = ReadStaticArray(rawData, ref offset, 4),
				StaticResourcesCrew = ReadStaticArray(rawData, ref offset, 8),
				StaticResourcesOre = ReadStaticArray(rawData, ref offset, 8),
				StaticResourcesGas = ReadStaticArray(rawData, ref offset, 8),
				StaticResourcesTotals = ReadStaticArray(rawData, ref offset, 8)
			},
			StaticPlayer = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticPlayerArray = ReadStaticArray(rawData, ref offset, 8),
			StaticTime = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticDescription = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
		};
	}

	private static STATIC_DATA[] ReadStaticArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new STATIC_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		return values;
	}
}

internal sealed class GT_MENU1TypeParser : IUtfDbTypeParser {
	private static readonly IReadOnlyList<LayoutSpec> Menu1Layout = CreateMenu1Layout();
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_MENU1", "field", Menu1Layout);

	public string TypeName => "GT_MENU1";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static IReadOnlyList<LayoutSpec> CreateMenu1Layout() {
		return [
			Menu1Opening("opening"),
			Menu1Singleplayer("single_player_menu"),
			Menu1SelectCampaign("select_campaign"),
			Menu1SelectMission("select_mission"),
			Menu1NetConnections("net_connections"),
			Menu1IpAddress("ip_address"),
			Menu1GameZone("game_zone"),
			Menu1NetSessions2("net_sessions2"),
			Menu1Mshell("mshell"),
			Menu1Map("map"),
			Menu1Slots("slots"),
			Menu1Final("final"),
			Menu1HelpMenu("help_menu"),
			Menu1DeviceMenu("device_menu")
		];
	}

	private static GroupSpec Menu1Opening(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("buttons"), 6),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_labels"), 5),
		new RepeatSpec(UtfDbTypeLayouts.AnimateData("animations"), 5),
		UtfDbTypeLayouts.StaticData("static_legal")
	]);

	private static GroupSpec Menu1Singleplayer(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("static_single"),
		UtfDbTypeLayouts.StaticData("static_name"),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("buttons"), 5)
	]);

	private static GroupSpec Menu1SelectCampaign(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.StaticData("static_name"),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("buttons"), 4)
	]);

	private static GroupSpec Menu1SelectMission(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.ListboxData("list"),
		UtfDbTypeLayouts.ButtonData("start"),
		UtfDbTypeLayouts.ButtonData("ok"),
		UtfDbTypeLayouts.ButtonData("cancel"),
		UtfDbTypeLayouts.ButtonData("button_unlock"),
		UtfDbTypeLayouts.StaticData("static_mission"),
		UtfDbTypeLayouts.StaticData("static_holder"),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("button_missions"), 16),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("button_movies"), 6),
		UtfDbTypeLayouts.ButtonData("button_back"),
		UtfDbTypeLayouts.StaticData("static_movie"),
		UtfDbTypeLayouts.StaticData("static_mission_title"),
		UtfDbTypeLayouts.AnimateData("anim_system"),
		new RepeatSpec(new ScalarSpec("n_line_from", ScalarKind.S4), 16),
		new RepeatSpec(new ScalarSpec("n_movie_before_mission", ScalarKind.S4), 6)
	]);

	private static GroupSpec Menu1NetConnections(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("description"),
		UtfDbTypeLayouts.ListboxData("list"),
		UtfDbTypeLayouts.ButtonData("next"),
		UtfDbTypeLayouts.ButtonData("back"),
		UtfDbTypeLayouts.StaticData("static_title"),
		UtfDbTypeLayouts.ButtonData("button_zone"),
		UtfDbTypeLayouts.ButtonData("button_web")
	]);

	private static GroupSpec Menu1IpAddress(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("description"),
		UtfDbTypeLayouts.StaticData("enter_ip"),
		UtfDbTypeLayouts.StaticData("enter_name"),
		UtfDbTypeLayouts.StaticData("static_name"),
		UtfDbTypeLayouts.StaticData("static_join"),
		UtfDbTypeLayouts.StaticData("static_create"),
		UtfDbTypeLayouts.ButtonData("check_join"),
		UtfDbTypeLayouts.ButtonData("check_create"),
		UtfDbTypeLayouts.ButtonData("next"),
		UtfDbTypeLayouts.ButtonData("back"),
		UtfDbTypeLayouts.ComboboxData("combobox_ip")
	]);

	private static GroupSpec Menu1GameZone(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("description")
	]);

	private static GroupSpec Menu1NetSessions2(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("description"),
		UtfDbTypeLayouts.StaticData("version"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.ListboxData("list"),
		UtfDbTypeLayouts.ButtonData("next"),
		UtfDbTypeLayouts.ButtonData("back"),
		UtfDbTypeLayouts.StaticData("static_game"),
		UtfDbTypeLayouts.StaticData("static_players"),
		UtfDbTypeLayouts.StaticData("static_speed"),
		UtfDbTypeLayouts.StaticData("static_map"),
		UtfDbTypeLayouts.StaticData("static_resources")
	]);

	private static GroupSpec Menu1Mshell(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.StaticData("enter_chat"),
		UtfDbTypeLayouts.StaticData("ipaddress"),
		UtfDbTypeLayouts.ListboxData("list_chat"),
		UtfDbTypeLayouts.EditData("edit_chat")
	]);

	private static GroupSpec Menu1Map(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.ButtonData("map_type"),
		UtfDbTypeLayouts.StaticData("static_game_type"),
		UtfDbTypeLayouts.StaticData("static_speed"),
		UtfDbTypeLayouts.StaticData("static_money"),
		UtfDbTypeLayouts.StaticData("static_units"),
		UtfDbTypeLayouts.StaticData("static_map_type"),
		UtfDbTypeLayouts.StaticData("static_size"),
		UtfDbTypeLayouts.StaticData("static_terrain"),
		UtfDbTypeLayouts.StaticData("static_visibility"),
		UtfDbTypeLayouts.StaticData("static_bandwidth"),
		UtfDbTypeLayouts.DropdownData("drop_game_type"),
		UtfDbTypeLayouts.DropdownData("drop_speed"),
		UtfDbTypeLayouts.DropdownData("drop_money"),
		UtfDbTypeLayouts.DropdownData("drop_size"),
		UtfDbTypeLayouts.DropdownData("drop_terrain"),
		UtfDbTypeLayouts.DropdownData("drop_units"),
		UtfDbTypeLayouts.DropdownData("drop_visibility"),
		UtfDbTypeLayouts.DropdownData("band_width_option"),
		UtfDbTypeLayouts.SliderData("slider_speed"),
		UtfDbTypeLayouts.SliderData("slider_cmd_points"),
		UtfDbTypeLayouts.StaticData("static_spectator"),
		UtfDbTypeLayouts.StaticData("static_diplomacy"),
		UtfDbTypeLayouts.ButtonData("push_spectator"),
		UtfDbTypeLayouts.ButtonData("push_diplomacy"),
		UtfDbTypeLayouts.StaticData("static_difficulty"),
		UtfDbTypeLayouts.StaticData("static_easy"),
		UtfDbTypeLayouts.StaticData("static_average"),
		UtfDbTypeLayouts.StaticData("static_hard"),
		UtfDbTypeLayouts.ButtonData("push_easy"),
		UtfDbTypeLayouts.ButtonData("push_average"),
		UtfDbTypeLayouts.ButtonData("push_hard"),
		UtfDbTypeLayouts.StaticData("static_lock_settings"),
		UtfDbTypeLayouts.ButtonData("push_lock_settings"),
		UtfDbTypeLayouts.StaticData("static_systems"),
		UtfDbTypeLayouts.DropdownData("drop_systems"),
		UtfDbTypeLayouts.StaticData("static_cmd_points"),
		UtfDbTypeLayouts.StaticData("static_cmd_points_display")
	]);

	private static GroupSpec Menu1Slots(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		new RepeatSpec(UtfDbTypeLayouts.DropdownData("drop_slots"), 8),
		new RepeatSpec(UtfDbTypeLayouts.DropdownData("drop_teams"), 8),
		new RepeatSpec(UtfDbTypeLayouts.DropdownData("drop_races"), 8),
		new RepeatSpec(UtfDbTypeLayouts.DropdownData("drop_players"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_names"), 8),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_pings"), 8),
		new RepeatSpec(new ScalarSpec("terran_computer_names", ScalarKind.Utf16_64), 8),
		new RepeatSpec(new ScalarSpec("mantis_computer_names", ScalarKind.Utf16_64), 8),
		new RepeatSpec(new ScalarSpec("solarian_computer_names", ScalarKind.Utf16_64), 8)
	]);

	private static GroupSpec Menu1Final(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("static_state"),
		UtfDbTypeLayouts.StaticData("static_name"),
		UtfDbTypeLayouts.StaticData("static_color"),
		UtfDbTypeLayouts.StaticData("static_race"),
		UtfDbTypeLayouts.StaticData("static_team"),
		UtfDbTypeLayouts.StaticData("static_ping"),
		UtfDbTypeLayouts.StaticData("description"),
		UtfDbTypeLayouts.StaticData("static_accept"),
		UtfDbTypeLayouts.ButtonData("accept"),
		UtfDbTypeLayouts.ButtonData("start"),
		UtfDbTypeLayouts.ButtonData("cancel"),
		UtfDbTypeLayouts.StaticData("static_countdown")
	]);

	private static GroupSpec Menu1HelpMenu(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.StaticData("static_conquest"),
		UtfDbTypeLayouts.StaticData("static_version"),
		UtfDbTypeLayouts.StaticData("static_number"),
		UtfDbTypeLayouts.ButtonData("button_ok"),
		UtfDbTypeLayouts.StaticData("static_product_id"),
		UtfDbTypeLayouts.StaticData("static_product_number"),
		UtfDbTypeLayouts.StaticData("static_legal"),
		UtfDbTypeLayouts.ButtonData("button_credits")
	]);

	private static GroupSpec Menu1DeviceMenu(string label) => new(label, [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("static_background"),
		UtfDbTypeLayouts.StaticData("static_title"),
		UtfDbTypeLayouts.StaticData("static_pick_device"),
		UtfDbTypeLayouts.ListboxData("list_devices"),
		UtfDbTypeLayouts.ButtonData("button_ok")
	]);

	private static GT_MENU1 ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_MENU1 {
			Opening = new GT_MENU1_OPENING {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Buttons = ReadButtonArray(rawData, ref offset, 6),
				StaticLabels = ReadStaticArray(rawData, ref offset, 5),
				Animations = ReadAnimateArray(rawData, ref offset, 5),
				StaticLegal = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
			},
			SinglePlayerMenu = new GT_MENU1_SINGLEPLAYER_MENU {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticSingle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticName = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Buttons = ReadButtonArray(rawData, ref offset, 5)
			},
			SelectCampaign = new GT_MENU1_SELECT_CAMPAIGN {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticName = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Buttons = ReadButtonArray(rawData, ref offset, 4)
			},
			SelectMission = ParseMenu1SelectMission(rawData, ref offset),
			NetConnections = new GT_MENU1_NET_CONNECTIONS {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Description = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				List = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset),
				Next = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				Back = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				StaticTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				ButtonZone = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				ButtonWeb = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset)
			},
			IpAddress = new GT_MENU1_IP_ADDRESS {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Description = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				EnterIp = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				EnterName = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticName = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticJoin = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticCreate = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				CheckJoin = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				CheckCreate = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				Next = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				Back = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				ComboboxIp = UtfDbTypeParserHelpers.ParseComboboxData(rawData, ref offset)
			},
			GameZone = new GT_MENU1_GAME_ZONE {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				Description = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
			},
			NetSessions2 = new GT_MENU1_NET_SESSIONS2 {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Description = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Version = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				List = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset),
				Next = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				Back = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				StaticGame = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticPlayers = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticSpeed = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticMap = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticResources = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
			},
			MShell = new GT_MENU1_MSHELL {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				EnterChat = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Ipaddress = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				ListChat = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset),
				EditChat = UtfDbTypeParserHelpers.ParseEditData(rawData, ref offset)
			},
			Map = ParseMenu1Map(rawData, ref offset),
			Slots = ParseMenu1Slots(rawData, ref offset),
			Final = new GT_MENU1_FINAL {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				StaticState = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticName = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticColor = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticRace = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticTeam = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticPing = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Description = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticAccept = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Accept = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				Start = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				Cancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				StaticCountdown = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
			},
			HelpMenu = new GT_MENU1_HELPMENU {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticConquest = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticVersion = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticNumber = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				ButtonOk = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
				StaticProductId = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticProductNumber = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticLegal = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				ButtonCredits = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset)
			},
			DeviceMenu = new GT_MENU1_DEVICEMENU {
				ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
				StaticBackground = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				StaticPickDevice = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				ListDevices = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset),
				ButtonOk = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset)
			}
		};
	}

	private static GT_MENU1_SELECT_MISSION ParseMenu1SelectMission(ReadOnlySpan<byte> rawData, ref int offset) {
		var nLineFrom = new int[16];
		var nMovieBeforeMission = new int[6];
		var screenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var list = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset);
		var start = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var ok = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var cancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var buttonUnlock = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var staticMission = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticHolder = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var buttonMissions = ReadButtonArray(rawData, ref offset, 16);
		var buttonMovies = ReadButtonArray(rawData, ref offset, 6);
		var buttonBack = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var staticMovie = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticMissionTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var animSystem = UtfDbTypeParserHelpers.ParseAnimateData(rawData, ref offset);
		var value = new GT_MENU1_SELECT_MISSION {
			ScreenRect = screenRect,
			Background = background,
			Title = title,
			List = list,
			Start = start,
			Ok = ok,
			Cancel = cancel,
			ButtonUnlock = buttonUnlock,
			StaticMission = staticMission,
			StaticHolder = staticHolder,
			ButtonMissions = buttonMissions,
			ButtonMovies = buttonMovies,
			ButtonBack = buttonBack,
			StaticMovie = staticMovie,
			StaticMissionTitle = staticMissionTitle,
			AnimSystem = animSystem,
			NLineFrom = nLineFrom,
			NMovieBeforeMission = nMovieBeforeMission
		};

		for (var index = 0; index < nLineFrom.Length; index++) nLineFrom[index] = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset);
		for (var index = 0; index < nMovieBeforeMission.Length; index++) nMovieBeforeMission[index] = UtfDbTypeParserHelpers.ReadInt32(rawData, ref offset);
		return value;
	}

	private static GT_MENU1_MAP ParseMenu1Map(ReadOnlySpan<byte> rawData, ref int offset) {
		return new GT_MENU1_MAP {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			MapType = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			StaticGameType = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticSpeed = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticMoney = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticUnits = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticMapType = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticSize = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticTerrain = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticVisibility = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticBandwidth = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			DropGameType = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			DropSpeed = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			DropMoney = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			DropSize = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			DropTerrain = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			DropUnits = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			DropVisibility = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			BandWidthOption = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			SliderSpeed = UtfDbTypeParserHelpers.ParseSliderData(rawData, ref offset),
			SliderCmdPoints = UtfDbTypeParserHelpers.ParseSliderData(rawData, ref offset),
			StaticSpectator = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticDiplomacy = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			PushSpectator = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			PushDiplomacy = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			StaticDifficulty = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticEasy = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticAverage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticHard = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			PushEasy = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			PushAverage = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			PushHard = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			StaticLockSettings = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			PushLockSettings = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			StaticSystems = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			DropSystems = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			StaticCmdPoints = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticCmdPointsDisplay = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
		};
	}

	private static GT_MENU1_SLOTS ParseMenu1Slots(ReadOnlySpan<byte> rawData, ref int offset) {
		return new GT_MENU1_SLOTS {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			DropSlots = ReadDropdownArray(rawData, ref offset, 8),
			DropTeams = ReadDropdownArray(rawData, ref offset, 8),
			DropRaces = ReadDropdownArray(rawData, ref offset, 8),
			DropPlayers = ReadDropdownArray(rawData, ref offset, 8),
			StaticNames = ReadStaticArray(rawData, ref offset, 8),
			StaticPings = ReadStaticArray(rawData, ref offset, 8),
			TerranComputerNames = ReadUtf16StringArray(rawData, ref offset, 8),
			MantisComputerNames = ReadUtf16StringArray(rawData, ref offset, 8),
			SolarianComputerNames = ReadUtf16StringArray(rawData, ref offset, 8)
		};
	}

	private static BUTTON_DATA[] ReadButtonArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new BUTTON_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		return values;
	}

	private static STATIC_DATA[] ReadStaticArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new STATIC_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		return values;
	}

	private static ANIMATE_DATA[] ReadAnimateArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new ANIMATE_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseAnimateData(rawData, ref offset);
		return values;
	}

	private static DROPDOWN_DATA[] ReadDropdownArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new DROPDOWN_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset);
		return values;
	}

	private static string[] ReadUtf16StringArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new string[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ReadUtf16String64(rawData, ref offset);
		return values;
	}
}

internal sealed class GT_TOOLBARTypeParser : IUtfDbTypeParser {
	private static readonly IReadOnlyList<LayoutSpec> ToolbarLayout = CreateToolbarLayout();
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_TOOLBAR", "field", ToolbarLayout);

	public string TypeName => "GT_TOOLBAR";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static IReadOnlyList<LayoutSpec> CreateToolbarLayout() {
		return [
			new ScalarSpec("vfx_shape_type", ScalarKind.Ascii32),
			new RepeatSpec(new ScalarSpec("vfx_tool_bar", ScalarKind.Ascii32), 4),
			UtfDbTypeLayouts.Rect("context_rect"),
			new RepeatSpec(UtfDbTypeLayouts.Rect("sysmap_rect"), 4),
			new RepeatSpec(UtfDbTypeLayouts.Rect("sector_map_rect"), 4),
			new ScalarSpec("top_bar_x", ScalarKind.U4),
			new ScalarSpec("top_bar_y", ScalarKind.U4),
			new RepeatSpec(ToolbarCommon("common"), 4),
			ToolbarNone("none"),
			new RepeatSpec(ToolbarFabricator("fabricators"), 4),
			new RepeatSpec(ToolbarLIndustrial("lindustrials"), 12),
			ToolbarGeneric("turret"),
			new RepeatSpec(ToolbarResearch("researches"), 30),
			new RepeatSpec(ToolbarBuildRes("build_res"), 15),
			new RepeatSpec(ToolbarFleet("fleets"), 4),
			new RepeatSpec(ToolbarWarTurret("war_turrets"), 4),
			new RepeatSpec(ToolbarIndividual("individuals"), 4),
			new RepeatSpec(ToolbarGroup("groups"), 4)
		];
	}

	private static GroupSpec ToolbarCommon(string label) => new(label, [
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("buttons"), 16),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("resources"), 5),
		new RepeatSpec(UtfDbTypeLayouts.IconData("supply_icons"), 2)
	]);

	private static GroupSpec ToolbarNone(string label) => new(label, [
		UtfDbTypeLayouts.StaticData("nothing")
	]);

	private static GroupSpec ToolbarFabricator(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("buttons"), 3),
		UtfDbTypeLayouts.EditData("shipname"),
		UtfDbTypeLayouts.StaticData("hull"),
		UtfDbTypeLayouts.TabcontrolData("fab_tab"),
		new RepeatSpec(new GroupSpec("build_tabs", [new RepeatSpec(UtfDbTypeLayouts.BuildbuttonData("builds"), 16)]), 4),
		new RepeatSpec(UtfDbTypeLayouts.HotstaticData("stat_tab"), 4)
	]);

	private static GroupSpec ToolbarLIndustrial(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		new RepeatSpec(UtfDbTypeLayouts.IconData("supply_icons"), 2),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("buttons"), 2),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("stats"), 6),
		new RepeatSpec(UtfDbTypeLayouts.BuildbuttonData("builds"), 10),
		UtfDbTypeLayouts.QueuecontrolData("build_queue")
	]);

	private static GroupSpec ToolbarGeneric(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		UtfDbTypeLayouts.IconData("in_supply"),
		UtfDbTypeLayouts.IconData("not_in_supply"),
		UtfDbTypeLayouts.StaticData("hull"),
		UtfDbTypeLayouts.StaticData("supplies"),
		UtfDbTypeLayouts.StaticData("location"),
		UtfDbTypeLayouts.StaticData("disabled_text")
	]);

	private static GroupSpec ToolbarResearch(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		new RepeatSpec(UtfDbTypeLayouts.IconData("supply_icons"), 2),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("buttons"), 1),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("stats"), 7),
		new RepeatSpec(UtfDbTypeLayouts.ResearchbuttonData("research"), 10),
		UtfDbTypeLayouts.QueuecontrolData("build_queue")
	]);

	private static GroupSpec ToolbarBuildRes(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		new RepeatSpec(UtfDbTypeLayouts.IconData("supply_icons"), 2),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("buttons"), 5),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("stats"), 6),
		new RepeatSpec(UtfDbTypeLayouts.ResearchbuttonData("research"), 16),
		new RepeatSpec(UtfDbTypeLayouts.BuildbuttonData("builds"), 6),
		UtfDbTypeLayouts.QueuecontrolData("build_queue")
	]);

	private static GroupSpec ToolbarFleet(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("command_buttons"), 4),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("tactic_buttons"), 4),
		new RepeatSpec(UtfDbTypeLayouts.MultihotbuttonData("formations"), 6),
		UtfDbTypeLayouts.TabcontrolData("fab_tab"),
		new GroupSpec("order_tab", [
			UtfDbTypeLayouts.MultihotbuttonData("admiral"),
			new RepeatSpec(UtfDbTypeLayouts.StaticData("stats"), 3),
			new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("orders"), 6),
			new RepeatSpec(UtfDbTypeLayouts.MultihotbuttonData("specials"), 12)
		]),
		new GroupSpec("sil_tab", [new RepeatSpec(UtfDbTypeLayouts.ShipsilbuttonData("ships"), 22)]),
		new GroupSpec("stat_tab", [
			new RepeatSpec(UtfDbTypeLayouts.StaticData("stats"), 3),
			new RepeatSpec(UtfDbTypeLayouts.HotstaticData("techs"), 7)
		]),
		new GroupSpec("kit_tab", [
			new RepeatSpec(UtfDbTypeLayouts.StaticData("stats"), 3),
			new RepeatSpec(UtfDbTypeLayouts.MultihotbuttonData("kits"), 14),
			new RepeatSpec(UtfDbTypeLayouts.MultihotbuttonData("kit_display"), 2),
			UtfDbTypeLayouts.QueuecontrolData("kit_queue")
		])
	]);

	private static GroupSpec ToolbarWarTurret(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		new RepeatSpec(UtfDbTypeLayouts.IconData("supply_icons"), 2),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("stats"), 3),
		UtfDbTypeLayouts.MultihotbuttonData("special"),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("stance"), 2),
		new RepeatSpec(UtfDbTypeLayouts.HotstaticData("techs"), 6)
	]);

	private static GroupSpec ToolbarIndividual(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("buttons"), 15),
		new RepeatSpec(UtfDbTypeLayouts.MultihotbuttonData("special"), 4),
		UtfDbTypeLayouts.EditData("namearea"),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("stats"), 5),
		new RepeatSpec(UtfDbTypeLayouts.HotstaticData("techs"), 7)
	]);

	private static GroupSpec ToolbarGroup(string label) => new(label, [
		new ScalarSpec("type", ScalarKind.U4),
		new RepeatSpec(UtfDbTypeLayouts.MultihotbuttonData("special"), 3),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("command_buttons"), 5),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("stance_buttons"), 7),
		new RepeatSpec(UtfDbTypeLayouts.HotbuttonData("fighter_stance"), 2),
		new RepeatSpec(UtfDbTypeLayouts.ShipsilbuttonData("ships"), 22)
	]);

	private static GT_TOOLBAR ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var vfxShapeType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var vfxToolBar = ReadAscii32Array(rawData, ref offset, 4);
		var contextRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var sysmapRect = ReadRectArray(rawData, ref offset, 4);
		var sectorMapRect = ReadRectArray(rawData, ref offset, 4);
		var topBarX = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		var topBarY = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		var common = ReadToolbarCommonArray(rawData, ref offset, 4);
		var none = new GT_TOOLBAR_NONE {
			Nothing = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
		};
		var fabricators = ReadToolbarFabricators(rawData, ref offset, 4);
		var lIndustrials = ReadToolbarLIndustrials(rawData, ref offset, 12);
		var turret = new GT_TOOLBAR_GENERIC {
			Type = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			InSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
			NotInSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
			Hull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Supplies = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Location = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			DisabledText = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
		};
		var researches = ReadToolbarResearches(rawData, ref offset, 30);
		var buildRes = ReadToolbarBuildRes(rawData, ref offset, 15);
		var toolbar = new GT_TOOLBAR {
			VfxShapeType = vfxShapeType,
			VfxToolBar = vfxToolBar,
			ContextRect = contextRect,
			SysmapRect = sysmapRect,
			SectorMapRect = sectorMapRect,
			TopBarX = topBarX,
			TopBarY = topBarY,
			Common = common,
			None = none,
			Fabricator = fabricators[0],
			MWeaver = fabricators[1],
			SForger = fabricators[2],
			VShaper = fabricators[3],
			LIndustrial = lIndustrials[0],
			Hq = lIndustrials[1],
			HIndustrial = lIndustrials[2],
			MCocoon = lIndustrials[3],
			MNiad = lIndustrials[4],
			SAcropolis = lIndustrials[5],
			SPavilion = lIndustrials[6],
			SSanctum = lIndustrials[7],
			SGreaterPavilion = lIndustrials[8],
			VLocus = lIndustrials[9],
			VCompiler = lIndustrials[10],
			VFormulator = lIndustrials[11],
			Turret = turret,
			MPlantation = researches[0],
			Proplab = researches[1],
			Ballistics = researches[2],
			AdvHull = researches[3],
			AwsLab = researches[4],
			Lrsensor = researches[5],
			Hanger = researches[6],
			Weapons = researches[7],
			Displacement = researches[8],
			MBlastFurnace = researches[9],
			MExplosivesRange = researches[10],
			MCarrionRoost = researches[11],
			MBioForge = researches[12],
			MFusionMill = researches[13],
			MCarpacePlant = researches[14],
			MHybridCenter = researches[15],
			MPlasmaSpitter = researches[16],
			SHelionVeil = researches[17],
			SXenoChamber = researches[18],
			SAnvil = researches[19],
			SMunitionsAnnex = researches[20],
			MEyeStock = researches[21],
			MMutationColony = researches[22],
			STurbineDock = researches[23],
			SBunker = researches[24],
			VHatchery = researches[25],
			VCochleaDish = researches[26],
			VClawOfVyrie = researches[27],
			VHammerOfVyrie = researches[28],
			VEyeOfVyrie = researches[29],
			MThripid = buildRes[0],
			Academy = buildRes[1],
			Refinery = buildRes[2],
			MCollector = buildRes[3],
			MGreaterCollector = buildRes[4],
			MWarlordTraining = buildRes[5],
			SSentinalTower = buildRes[6],
			SCitidel = buildRes[7],
			THeavyRefinery = buildRes[8],
			TSuperHeavyRefinery = buildRes[9],
			SOxidator = buildRes[10],
			VCoalescer = buildRes[11],
			VGudgeon = buildRes[12],
			VTempleOfVyrie = buildRes[13],
			Outpost = buildRes[14],
			Fleets = ReadToolbarFleets(rawData, ref offset, 4),
			WarTurrets = ReadToolbarWarTurrets(rawData, ref offset, 4),
			Individuals = ReadToolbarIndividuals(rawData, ref offset, 4),
			Groups = ReadToolbarGroups(rawData, ref offset, 4)
		};

		return toolbar;
	}

	private static string[] ReadAscii32Array(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new string[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		return values;
	}

	private static RECT[] ReadRectArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new RECT[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		return values;
	}

	private static STATIC_DATA[] ReadStaticArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new STATIC_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		return values;
	}

	private static GT_TOOLBAR_COMMON[] ReadToolbarCommonArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new GT_TOOLBAR_COMMON[count];
		for (var index = 0; index < count; index++) {
			values[index] = new GT_TOOLBAR_COMMON {
				RotateLeft = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				ReturnDefaultView = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				RotateRight = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Minimize = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Restore = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Options = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Chat = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Go = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				StarMap = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				ExitSysMap = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				HpDiplomacy = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				HpResearch = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				HpFleetOfficer = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				HpIndustrial = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				HpIdleCivilian = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				MissionObjectives = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Gas = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Metal = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Crew = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				CommandPts = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Shipclass = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				InSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
				NotInSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset)
			};
		}
		return values;
	}

	private static GT_TOOLBAR_FABRICATOR[] ReadToolbarFabricators(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new GT_TOOLBAR_FABRICATOR[count];
		for (var index = 0; index < count; index++) {
			values[index] = new GT_TOOLBAR_FABRICATOR {
				Type = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
				Sell = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Stop = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Repair = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Shipname = UtfDbTypeParserHelpers.ParseEditData(rawData, ref offset),
				Hull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				FabTab = UtfDbTypeParserHelpers.ParseTabcontrolData(rawData, ref offset),
				BasicTab = ParseToolbarFabricatorTab1(rawData, ref offset),
				AdvancedTab = ParseToolbarFabricatorTab2(rawData, ref offset),
				DefenceTab = ParseToolbarFabricatorTab4(rawData, ref offset),
				MoonTab = ParseToolbarFabricatorTab5(rawData, ref offset),
				StatisticsTab = ParseToolbarFabricatorTab3(rawData, ref offset)
			};
		}
		return values;
	}

	private static GT_TOOLBAR_LINDUSTRIAL[] ReadToolbarLIndustrials(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new GT_TOOLBAR_LINDUSTRIAL[count];
		for (var index = 0; index < count; index++) {
			values[index] = new GT_TOOLBAR_LINDUSTRIAL {
				Type = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
				InSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
				NotInSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
				Rally = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Stop = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Hull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				MetalStorage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				GasStorage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				CrewStorage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Location = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				DisabledText = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Build0 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build1 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build2 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build3 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build4 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build5 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build6 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build7 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build8 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build9 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				BuildQueue = UtfDbTypeParserHelpers.ParseQueuecontrolData(rawData, ref offset)
			};
		}
		return values;
	}

	private static GT_TOOLBAR_RESEARCH[] ReadToolbarResearches(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new GT_TOOLBAR_RESEARCH[count];
		for (var index = 0; index < count; index++) {
			values[index] = new GT_TOOLBAR_RESEARCH {
				Type = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
				InSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
				NotInSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
				Stop = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Hull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Supplies = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				MetalStorage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				GasStorage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				CrewStorage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Location = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				DisabledText = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Research0 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research1 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research2 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research3 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research4 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research5 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research6 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research7 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research8 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research9 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				BuildQueue = UtfDbTypeParserHelpers.ParseQueuecontrolData(rawData, ref offset)
			};
		}
		return values;
	}

	private static GT_TOOLBAR_BUILD_RES[] ReadToolbarBuildRes(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new GT_TOOLBAR_BUILD_RES[count];
		for (var index = 0; index < count; index++) {
			values[index] = new GT_TOOLBAR_BUILD_RES {
				Type = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
				InSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
				NotInSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
				Stop = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Rally = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				NoAuto = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				AutoOre = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				AutoGas = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Hull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				MetalStorage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				GasStorage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				CrewStorage = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Location = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				DisabledText = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Research0 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research1 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research2 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research3 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research4 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research5 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research6 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research7 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research8 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research9 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research10 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research11 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research12 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research13 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research14 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Research15 = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset),
				Build0 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build1 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build2 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build3 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build4 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				Build5 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
				BuildQueue = UtfDbTypeParserHelpers.ParseQueuecontrolData(rawData, ref offset)
			};
		}
		return values;
	}

	private static GT_TOOLBAR_FLEET[] ReadToolbarFleets(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new GT_TOOLBAR_FLEET[count];
		for (var index = 0; index < count; index++) {
			values[index] = new GT_TOOLBAR_FLEET {
				Type = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
				Escort = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Patrol = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Stop = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				AttackPosition = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				TacticPeace = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				TacticStandGround = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				TacticDefend = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				TacticSeek = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Formation1 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Formation2 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Formation3 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Formation4 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Formation5 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Formation6 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				FabTab = UtfDbTypeParserHelpers.ParseTabcontrolData(rawData, ref offset),
				OrderTab = ParseToolbarFleetOrderTab(rawData, ref offset),
				SilTab = ParseToolbarFleetSilTab(rawData, ref offset),
				StatTab = ParseToolbarFleetStatTab(rawData, ref offset),
				KitTab = ParseToolbarFleetKitTab(rawData, ref offset)
			};
		}
		return values;
	}

	private static GT_TOOLBAR_WARTURRET[] ReadToolbarWarTurrets(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new GT_TOOLBAR_WARTURRET[count];
		for (var index = 0; index < count; index++) {
			values[index] = new GT_TOOLBAR_WARTURRET {
				Type = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
				InSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
				NotInSupply = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset),
				Hull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Supplies = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Location = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Specialweapon = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				FighterStanceNormal = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				FighterStancePatrol = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Techarmor = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techsupply = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techsheild = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techweapon = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techsensors = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techspecial = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset)
			};
		}
		return values;
	}

	private static GT_TOOLBAR_INDIVIDUAL[] ReadToolbarIndividuals(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new GT_TOOLBAR_INDIVIDUAL[count];
		for (var index = 0; index < count; index++) {
			values[index] = new GT_TOOLBAR_INDIVIDUAL {
				Type = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
				Patrol = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Stop = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Escort = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				StanceAttack = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				StanceDefend = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				StanceStand = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				StanceStop = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				SupplyStanceAuto = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				SupplyStanceNoAuto = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				SupplyStanceResupplyOnly = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Cloak = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				AttackPosition = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				FighterStanceNormal = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				FighterStancePatrol = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				EjectArtifact = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Specialweapon = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Specialweapon1 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Specialweapon2 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Artifact = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Namearea = UtfDbTypeParserHelpers.ParseEditData(rawData, ref offset),
				Hull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Supplies = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Kills = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Metal = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Gas = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
				Techarmor = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techsupply = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techengine = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techsheild = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techweapon = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techsensors = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
				Techspecial = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset)
			};
		}
		return values;
	}

	private static GT_TOOLBAR_GROUP[] ReadToolbarGroups(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new GT_TOOLBAR_GROUP[count];
		for (var index = 0; index < count; index++) {
			values[index] = new GT_TOOLBAR_GROUP {
				Type = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
				Specialweapon = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Specialweapon1 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Specialweapon2 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
				Escort = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Patrol = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Stop = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Cloak = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				AttackPosition = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				StanceAttack = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				StanceDefend = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				StanceStand = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				StanceStop = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				SupplyStanceAuto = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				SupplyStanceNoAuto = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				SupplyStanceResupplyOnly = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				FighterStanceNormal = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				FighterStancePatrol = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
				Ship0 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship1 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship2 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship3 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship4 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship5 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship6 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship7 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship8 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship9 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship10 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship11 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship12 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship13 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship14 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship15 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship16 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship17 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship18 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship19 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship20 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
				Ship21 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset)
			};
		}
		return values;
	}

	private static GT_TOOLBAR_FABRICATOR_TAB1 ParseToolbarFabricatorTab1(ReadOnlySpan<byte> rawData, ref int offset) => new() {
		Plat0 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat1 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat2 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat3 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat4 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat5 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat6 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat7 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat8 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat9 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat10 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat11 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat12 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat13 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat14 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat15 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset)
	};

	private static GT_TOOLBAR_FABRICATOR_TAB2 ParseToolbarFabricatorTab2(ReadOnlySpan<byte> rawData, ref int offset) => new() {
		Plat16 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat17 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat18 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat19 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat20 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat21 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat22 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat23 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat24 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat25 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat26 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat27 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat28 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat29 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat30 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat31 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset)
	};

	private static GT_TOOLBAR_FABRICATOR_TAB4 ParseToolbarFabricatorTab4(ReadOnlySpan<byte> rawData, ref int offset) => new() {
		Plat32 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat33 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat34 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat35 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat36 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat37 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat38 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat39 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat40 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat41 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat42 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat43 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat44 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat45 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat46 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat47 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset)
	};

	private static GT_TOOLBAR_FABRICATOR_TAB5 ParseToolbarFabricatorTab5(ReadOnlySpan<byte> rawData, ref int offset) => new() {
		Plat48 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat49 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat50 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat51 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat52 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat53 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat54 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat55 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat56 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat57 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat58 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat59 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat60 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat61 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat62 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset),
		Plat63 = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset)
	};

	private static GT_TOOLBAR_FABRICATOR_TAB3 ParseToolbarFabricatorTab3(ReadOnlySpan<byte> rawData, ref int offset) => new() {
		Techarmor = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
		Techengine = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
		Techsheild = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
		Techsensors = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset)
	};

	private static GT_TOOLBAR_FLEET_ORDER_TAB ParseToolbarFleetOrderTab(ReadOnlySpan<byte> rawData, ref int offset) => new() {
		AdmiralHead = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		ONamearea = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
		OKills = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
		OHull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
		Order1 = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
		Order2 = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
		Order3 = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
		Order4 = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
		Order5 = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
		Order6 = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset),
		SpecialOrders0 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders1 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders2 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders3 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders4 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders5 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders6 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders7 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders8 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders9 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders10 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		SpecialOrders11 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset)
	};

	private static GT_TOOLBAR_FLEET_SIL_TAB ParseToolbarFleetSilTab(ReadOnlySpan<byte> rawData, ref int offset) => new() {
		Ship0 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship1 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship2 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship3 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship4 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship5 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship6 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship7 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship8 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship9 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship10 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship11 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship12 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship13 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship14 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship15 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship16 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship17 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship18 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship19 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship20 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset),
		Ship21 = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset)
	};

	private static GT_TOOLBAR_FLEET_STAT_TAB ParseToolbarFleetStatTab(ReadOnlySpan<byte> rawData, ref int offset) => new() {
		Namearea = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
		Hull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
		Kills = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
		Techarmor = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
		Techsupply = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
		Techengine = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
		Techsheild = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
		Techweapon = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
		Techsensors = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset),
		Techspecial = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset)
	};

	private static GT_TOOLBAR_FLEET_KIT_TAB ParseToolbarFleetKitTab(ReadOnlySpan<byte> rawData, ref int offset) => new() {
		KNamearea = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
		KHull = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
		KKills = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
		Kit0 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit1 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit2 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit3 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit4 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit5 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit6 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit7 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit8 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit9 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit10 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit11 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit12 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		Kit13 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		KitDisplay0 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		KitDisplay1 = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset),
		KitQueue = UtfDbTypeParserHelpers.ParseQueuecontrolData(rawData, ref offset)
	};

	private static HOTBUTTON_DATA[] ReadHotButtonArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new HOTBUTTON_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseHotbuttonData(rawData, ref offset);
		return values;
	}

	private static HOTSTATIC_DATA[] ReadHotStaticArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new HOTSTATIC_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseHotstaticData(rawData, ref offset);
		return values;
	}

	private static ICON_DATA[] ReadIconArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new ICON_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseIconData(rawData, ref offset);
		return values;
	}

	private static MULTIHOTBUTTON_DATA[] ReadMultiHotButtonArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new MULTIHOTBUTTON_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseMultihotbuttonData(rawData, ref offset);
		return values;
	}

	private static BUILDBUTTON_DATA[] ReadBuildButtonArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new BUILDBUTTON_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseBuildbuttonData(rawData, ref offset);
		return values;
	}

	private static RESEARCHBUTTON_DATA[] ReadResearchButtonArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new RESEARCHBUTTON_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseResearchbuttonData(rawData, ref offset);
		return values;
	}

	private static SHIPSILBUTTON_DATA[] ReadShipsilButtonArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new SHIPSILBUTTON_DATA[count];
		for (var index = 0; index < count; index++) values[index] = UtfDbTypeParserHelpers.ParseShipsilbuttonData(rawData, ref offset);
		return values;
	}
}
