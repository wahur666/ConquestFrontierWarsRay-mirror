using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal sealed class GT_MESSAGEBOXTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_MESSAGEBOX", "field", [
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.StaticData("message"),
		UtfDbTypeLayouts.ButtonData("ok"),
		UtfDbTypeLayouts.ButtonData("cancel"),
		UtfDbTypeLayouts.ButtonData("ok_alone")
	]);

	public string TypeName => "GT_MESSAGEBOX";

	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_MESSAGEBOX ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_MESSAGEBOX {
			Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Message = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Ok = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			Cancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			OkAlone = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset)
		};
	}
}

internal sealed class GT_BRIEFINGTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_BRIEFING", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.ButtonData("start"),
		UtfDbTypeLayouts.ButtonData("replay"),
		UtfDbTypeLayouts.ButtonData("cancel"),
		UtfDbTypeLayouts.Rect("rc_teletype"),
		new RepeatSpec(UtfDbTypeLayouts.Rect("rc_comm"), 4),
		new RepeatSpec(UtfDbTypeLayouts.AnimateData("anim_fuzz"), 4)
	]);

	public string TypeName => "GT_BRIEFING";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_BRIEFING ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var screenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var start = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var replay = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var cancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var rcTeletype = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var rcComm = new RECT[4];
		for (var index = 0; index < rcComm.Length; index++) {
			rcComm[index] = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		}

		var animFuzz = new ANIMATE_DATA[4];
		for (var index = 0; index < animFuzz.Length; index++) {
			animFuzz[index] = UtfDbTypeParserHelpers.ParseAnimateData(rawData, ref offset);
		}

		return new GT_BRIEFING {
			ScreenRect = screenRect,
			Background = background,
			Title = title,
			Start = start,
			Replay = replay,
			Cancel = cancel,
			RcTeletype = rcTeletype,
			RcComm = rcComm,
			AnimFuzz = animFuzz
		};
	}
}

internal sealed class GT_CHATTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_CHAT", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("ask"),
		UtfDbTypeLayouts.EditData("chatbox")
	]);

	public string TypeName => "GT_CHAT";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_CHAT ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_CHAT {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Ask = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Chatbox = UtfDbTypeParserHelpers.ParseEditData(rawData, ref offset)
		};
	}
}

internal sealed class GT_CREDITSTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_CREDITS", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("static_background")
	]);

	public string TypeName => "GT_CREDITS";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_CREDITS ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_CREDITS {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			StaticBackground = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
		};
	}
}

internal sealed class GT_DIPLOMACYMENUTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_DIPLOMACYMENU", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("static_title"),
		UtfDbTypeLayouts.StaticData("static_name"),
		UtfDbTypeLayouts.StaticData("static_race"),
		UtfDbTypeLayouts.StaticData("static_allies"),
		UtfDbTypeLayouts.StaticData("static_metal_title"),
		UtfDbTypeLayouts.StaticData("static_gas_title"),
		UtfDbTypeLayouts.StaticData("static_crew_title"),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_names"), 7),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_races"), 7),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("button_crew"), 7),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("button_metal"), 7),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("button_gas"), 7),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("button_allies"), 7),
		UtfDbTypeLayouts.StaticData("static_crew"),
		UtfDbTypeLayouts.StaticData("static_metal"),
		UtfDbTypeLayouts.StaticData("static_gas"),
		UtfDbTypeLayouts.ButtonData("button_ok"),
		UtfDbTypeLayouts.ButtonData("button_reset"),
		UtfDbTypeLayouts.ButtonData("button_cancel"),
		UtfDbTypeLayouts.ButtonData("button_apply"),
		new RepeatSpec(UtfDbTypeLayouts.DiplomacyButtonData("diplomacy_buttons"), 7)
	]);

	public string TypeName => "GT_DIPLOMACYMENU";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_DIPLOMACYMENU ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var screenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticName = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticRace = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticAllies = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticMetalTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticGasTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticCrewTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticNames = new STATIC_DATA[7];
		var staticRaces = new STATIC_DATA[7];
		var buttonCrew = new BUTTON_DATA[7];
		var buttonMetal = new BUTTON_DATA[7];
		var buttonGas = new BUTTON_DATA[7];
		var buttonAllies = new BUTTON_DATA[7];
		var diplomacyButtons = new DIPLOMACYBUTTON_DATA[7];

		for (var index = 0; index < staticNames.Length; index++) staticNames[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		for (var index = 0; index < staticRaces.Length; index++) staticRaces[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		for (var index = 0; index < buttonCrew.Length; index++) buttonCrew[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		for (var index = 0; index < buttonMetal.Length; index++) buttonMetal[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		for (var index = 0; index < buttonGas.Length; index++) buttonGas[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		for (var index = 0; index < buttonAllies.Length; index++) buttonAllies[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);

		return new GT_DIPLOMACYMENU {
			ScreenRect = screenRect,
			Background = background,
			StaticTitle = staticTitle,
			StaticName = staticName,
			StaticRace = staticRace,
			StaticAllies = staticAllies,
			StaticMetalTitle = staticMetalTitle,
			StaticGasTitle = staticGasTitle,
			StaticCrewTitle = staticCrewTitle,
			StaticNames = staticNames,
			StaticRaces = staticRaces,
			ButtonCrew = buttonCrew,
			ButtonMetal = buttonMetal,
			ButtonGas = buttonGas,
			ButtonAllies = buttonAllies,
			StaticCrew = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticMetal = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticGas = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			ButtonOk = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonReset = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonCancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonApply = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			DiplomacyButtons = FillDiplomacyButtons(rawData, ref offset, diplomacyButtons)
		};
	}

	private static DIPLOMACYBUTTON_DATA[] FillDiplomacyButtons(ReadOnlySpan<byte> rawData, ref int offset, DIPLOMACYBUTTON_DATA[] diplomacyButtons) {
		for (var index = 0; index < diplomacyButtons.Length; index++) diplomacyButtons[index] = UtfDbTypeParserHelpers.ParseDiplomacyButtonData(rawData, ref offset);
		return diplomacyButtons;
	}
}

internal sealed class GT_IGOPTIONSTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_IGOPTIONS", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.ButtonData("button_save"),
		UtfDbTypeLayouts.ButtonData("button_load"),
		UtfDbTypeLayouts.ButtonData("button_options"),
		UtfDbTypeLayouts.ButtonData("button_restart"),
		UtfDbTypeLayouts.ButtonData("button_resign"),
		UtfDbTypeLayouts.ButtonData("button_abdicate"),
		UtfDbTypeLayouts.ButtonData("button_return")
	]);

	public string TypeName => "GT_IGOPTIONS";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_IGOPTIONS ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_IGOPTIONS {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			ButtonSave = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonLoad = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonOptions = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonRestart = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonResign = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonAbdicate = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonReturn = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset)
		};
	}
}

internal sealed class GT_LOADSAVETypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_LOADSAVE", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.Rect("screen_rect_2d"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("static_load"),
		UtfDbTypeLayouts.StaticData("static_save"),
		UtfDbTypeLayouts.StaticData("static_file"),
		UtfDbTypeLayouts.ButtonData("open"),
		UtfDbTypeLayouts.ButtonData("save"),
		UtfDbTypeLayouts.ButtonData("cancel"),
		UtfDbTypeLayouts.ButtonData("delete_file"),
		UtfDbTypeLayouts.EditData("edit_file"),
		UtfDbTypeLayouts.ListboxData("list")
	]);

	public string TypeName => "GT_LOADSAVE";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_LOADSAVE ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_LOADSAVE {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			ScreenRect2D = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticLoad = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticSave = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticFile = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Open = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			Save = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			Cancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			DeleteFile = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			EditFile = UtfDbTypeParserHelpers.ParseEditData(rawData, ref offset),
			List = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset)
		};
	}
}

internal sealed class GT_MAPSELECTTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_MAPSELECT", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("static_background"),
		UtfDbTypeLayouts.StaticData("static_title"),
		UtfDbTypeLayouts.StaticData("static_random"),
		UtfDbTypeLayouts.StaticData("static_supplied"),
		UtfDbTypeLayouts.StaticData("static_saved"),
		UtfDbTypeLayouts.ListboxData("list_random"),
		UtfDbTypeLayouts.ListboxData("list_supplied"),
		UtfDbTypeLayouts.ListboxData("list_saved"),
		UtfDbTypeLayouts.ButtonData("button_ok"),
		UtfDbTypeLayouts.ButtonData("button_cancel")
	]);

	public string TypeName => "GT_MAPSELECT";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_MAPSELECT ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_MAPSELECT {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			StaticBackground = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticRandom = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticSupplied = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticSaved = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			ListRandom = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset),
			ListSupplied = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset),
			ListSaved = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset),
			ButtonOk = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonCancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset)
		};
	}
}

internal sealed class GT_MENUOBJECTIVESTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_MENUOBJECTIVES", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("static_objectives"),
		UtfDbTypeLayouts.StaticData("static_name"),
		UtfDbTypeLayouts.Rect("rc_teletype"),
		UtfDbTypeLayouts.ButtonData("button_ok"),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("check_objectives"), 9),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_objective_array"), 9)
	]);

	public string TypeName => "GT_MENUOBJECTIVES";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_MENUOBJECTIVES ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var screenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticObjectives = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticName = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var rcTeletype = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var buttonOk = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var checkObjectives = new BUTTON_DATA[9];
		var staticObjectiveArray = new STATIC_DATA[9];
		for (var index = 0; index < checkObjectives.Length; index++) checkObjectives[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		for (var index = 0; index < staticObjectiveArray.Length; index++) staticObjectiveArray[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		return new GT_MENUOBJECTIVES {
			ScreenRect = screenRect,
			Background = background,
			StaticObjectives = staticObjectives,
			StaticName = staticName,
			RcTeletype = rcTeletype,
			ButtonOk = buttonOk,
			CheckObjectives = checkObjectives,
			StaticObjectiveArray = staticObjectiveArray
		};
	}
}

internal sealed class GT_NEWPLAYERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_NEWPLAYER", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.StaticData("static_heading"),
		UtfDbTypeLayouts.EditData("edit"),
		UtfDbTypeLayouts.ButtonData("ok"),
		UtfDbTypeLayouts.ButtonData("cancel")
	]);

	public string TypeName => "GT_NEWPLAYER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_NEWPLAYER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_NEWPLAYER {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticHeading = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Edit = UtfDbTypeParserHelpers.ParseEditData(rawData, ref offset),
			Ok = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			Cancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset)
		};
	}
}

internal sealed class GT_OPTIONSTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_OPTIONS", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("title"),
		UtfDbTypeLayouts.StaticData("static_name"),
		UtfDbTypeLayouts.ListboxData("list_names"),
		UtfDbTypeLayouts.ButtonData("button_new"),
		UtfDbTypeLayouts.ButtonData("button_change"),
		UtfDbTypeLayouts.ButtonData("button_delete"),
		UtfDbTypeLayouts.StaticData("static_sound"),
		UtfDbTypeLayouts.StaticData("static_music"),
		UtfDbTypeLayouts.StaticData("static_comm"),
		UtfDbTypeLayouts.StaticData("static_chat"),
		UtfDbTypeLayouts.StaticData("static_speed"),
		UtfDbTypeLayouts.StaticData("static_scroll"),
		UtfDbTypeLayouts.StaticData("static_mouse"),
		new RepeatSpec(UtfDbTypeLayouts.SliderData("sliders"), 7),
		UtfDbTypeLayouts.ButtonData("push_sound"),
		UtfDbTypeLayouts.ButtonData("push_music"),
		UtfDbTypeLayouts.ButtonData("push_comm"),
		UtfDbTypeLayouts.ButtonData("push_chat"),
		UtfDbTypeLayouts.StaticData("static_dinput"),
		UtfDbTypeLayouts.ButtonData("push_dinput"),
		UtfDbTypeLayouts.StaticData("static_status"),
		UtfDbTypeLayouts.StaticData("static_rollover"),
		UtfDbTypeLayouts.StaticData("static_sector_map"),
		UtfDbTypeLayouts.StaticData("static_right_click"),
		UtfDbTypeLayouts.StaticData("static_subtitles"),
		UtfDbTypeLayouts.ButtonData("push_status"),
		UtfDbTypeLayouts.ButtonData("push_rollover"),
		UtfDbTypeLayouts.ButtonData("push_sector_map"),
		UtfDbTypeLayouts.ButtonData("push_right_click"),
		UtfDbTypeLayouts.ButtonData("push_subtitles"),
		UtfDbTypeLayouts.StaticData("static_gamma"),
		UtfDbTypeLayouts.StaticData("static_resolution"),
		UtfDbTypeLayouts.DropdownData("drop_resolution"),
		UtfDbTypeLayouts.SliderData("slider_gamma"),
		UtfDbTypeLayouts.StaticData("static_ships_3d"),
		UtfDbTypeLayouts.StaticData("static_trails"),
		UtfDbTypeLayouts.StaticData("static_emissive"),
		UtfDbTypeLayouts.StaticData("static_detail"),
		UtfDbTypeLayouts.StaticData("static_draw_back"),
		UtfDbTypeLayouts.ButtonData("push_trails"),
		UtfDbTypeLayouts.ButtonData("push_emissive"),
		UtfDbTypeLayouts.ButtonData("push_detail"),
		UtfDbTypeLayouts.SliderData("slide_draw_back"),
		UtfDbTypeLayouts.SliderData("slider_ships_3d"),
		UtfDbTypeLayouts.StaticData("static_device"),
		UtfDbTypeLayouts.DropdownData("drop_device"),
		UtfDbTypeLayouts.StaticData("static_3d_hardware"),
		UtfDbTypeLayouts.ButtonData("push_3d_hardware"),
		UtfDbTypeLayouts.TabcontrolData("tab"),
		UtfDbTypeLayouts.ButtonData("button_ok"),
		UtfDbTypeLayouts.ButtonData("button_cancel")
	]);

	public string TypeName => "GT_OPTIONS";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_OPTIONS ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var screenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var title = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticName = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var listNames = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset);
		var buttonNew = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var buttonChange = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var buttonDelete = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var staticFields = new STATIC_DATA[13];
		var sliders = new SLIDER_DATA[7];
		var pushButtons = new BUTTON_DATA[10];
		var graphicsStatics = new STATIC_DATA[5];
		var graphicsPushButtons = new BUTTON_DATA[3];
		for (var index = 0; index < 7; index++) staticFields[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		for (var index = 0; index < sliders.Length; index++) sliders[index] = UtfDbTypeParserHelpers.ParseSliderData(rawData, ref offset);
		for (var index = 0; index < 4; index++) pushButtons[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		staticFields[7] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		pushButtons[4] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		for (var index = 8; index < staticFields.Length; index++) staticFields[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		for (var index = 5; index < pushButtons.Length; index++) pushButtons[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		var staticGamma = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticResolution = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var dropResolution = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset);
		var sliderGamma = UtfDbTypeParserHelpers.ParseSliderData(rawData, ref offset);
		for (var index = 0; index < graphicsStatics.Length; index++) graphicsStatics[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		for (var index = 0; index < graphicsPushButtons.Length; index++) graphicsPushButtons[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		return new GT_OPTIONS {
			ScreenRect = screenRect,
			Background = background,
			Title = title,
			StaticName = staticName,
			ListNames = listNames,
			ButtonNew = buttonNew,
			ButtonChange = buttonChange,
			ButtonDelete = buttonDelete,
			StaticFields = staticFields,
			Sliders = sliders,
			PushButtons = pushButtons,
			StaticGamma = staticGamma,
			StaticResolution = staticResolution,
			DropResolution = dropResolution,
			SliderGamma = sliderGamma,
			GraphicsStatics = graphicsStatics,
			GraphicsPushButtons = graphicsPushButtons,
			SlideDrawBack = UtfDbTypeParserHelpers.ParseSliderData(rawData, ref offset),
			SliderShips3D = UtfDbTypeParserHelpers.ParseSliderData(rawData, ref offset),
			StaticDevice = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			DropDevice = UtfDbTypeParserHelpers.ParseDropdownData(rawData, ref offset),
			Static3DHardware = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Push3DHardware = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			Tab = UtfDbTypeParserHelpers.ParseTabcontrolData(rawData, ref offset),
			ButtonOk = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonCancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset)
		};
	}
}

internal sealed class GT_PAUSETypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_PAUSE", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("static_title"),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_description"), 8)
	]);

	public string TypeName => "GT_PAUSE";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_PAUSE ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var screenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var staticTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticDescription = new STATIC_DATA[8];
		for (var index = 0; index < staticDescription.Length; index++) staticDescription[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		return new GT_PAUSE {
			ScreenRect = screenRect,
			StaticTitle = staticTitle,
			StaticDescription = staticDescription
		};
	}
}

internal sealed class GT_PLAYERCHATMENUTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_PLAYERCHATMENU", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_names"), 7),
		new RepeatSpec(UtfDbTypeLayouts.StaticData("static_races"), 7),
		new RepeatSpec(UtfDbTypeLayouts.ButtonData("check_names"), 7),
		UtfDbTypeLayouts.ButtonData("button_allies"),
		UtfDbTypeLayouts.ButtonData("button_enemies"),
		UtfDbTypeLayouts.ButtonData("button_everyone"),
		UtfDbTypeLayouts.ListboxData("list_chat"),
		UtfDbTypeLayouts.EditData("edit_chat"),
		UtfDbTypeLayouts.ButtonData("button_close"),
		UtfDbTypeLayouts.StaticData("static_title"),
		UtfDbTypeLayouts.StaticData("static_chat")
	]);

	public string TypeName => "GT_PLAYERCHATMENU";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_PLAYERCHATMENU ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var screenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset);
		var background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		var staticNames = new STATIC_DATA[7];
		var staticRaces = new STATIC_DATA[7];
		var checkNames = new BUTTON_DATA[7];
		for (var index = 0; index < staticNames.Length; index++) staticNames[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		for (var index = 0; index < staticRaces.Length; index++) staticRaces[index] = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset);
		for (var index = 0; index < checkNames.Length; index++) checkNames[index] = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset);
		return new GT_PLAYERCHATMENU {
			ScreenRect = screenRect,
			Background = background,
			StaticNames = staticNames,
			StaticRaces = staticRaces,
			CheckNames = checkNames,
			ButtonAllies = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonEnemies = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ButtonEveryone = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			ListChat = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset),
			EditChat = UtfDbTypeParserHelpers.ParseEditData(rawData, ref offset),
			ButtonClose = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			StaticTitle = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticChat = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset)
		};
	}
}

internal sealed class GT_SPECIALABILITIESTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_SPECIALABILITIES", "field", [
		new RepeatSpec(UtfDbTypeLayouts.ArtifactButtonInfo("abilities"), 14)
	]);

	public string TypeName => "GT_SPECIALABILITIES";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_SPECIALABILITIES ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var count = rawData.Length / 16;
		var abilities = new ARTIFACT_BUTTON_INFO[count];
		for (var index = 0; index < abilities.Length; index++) abilities[index] = UtfDbTypeParserHelpers.ParseArtifactButtonInfo(rawData, ref offset);
		return new GT_SPECIALABILITIES {
			Abilities = abilities
		};
	}
}

internal sealed class GT_SYSTEM_KITTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_SYSTEM_KIT", "field", [
		new ScalarSpec("file_name", ScalarKind.Ascii32),
		new ScalarSpec("num_light", ScalarKind.U4),
		new RepeatSpec(UtfDbTypeLayouts.SystemKitLightInfo("light_info"), 10)
	]);

	public string TypeName => "GT_SYSTEM_KIT";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_SYSTEM_KIT ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var fileName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		var numLight = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		var lightInfo = new GT_SYSTEM_KIT_LIGHT_INFO[10];
		for (var index = 0; index < lightInfo.Length; index++) lightInfo[index] = UtfDbTypeParserHelpers.ParseSystemKitLightInfo(rawData, ref offset);
		return new GT_SYSTEM_KIT {
			FileName = fileName,
			NumLight = numLight,
			LightInfo = lightInfo
		};
	}
}

internal sealed class GT_SYSTEM_KIT_SAVELOADTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_SYSTEM_KIT_SAVELOAD", "field", [
		UtfDbTypeLayouts.Rect("screen_rect"),
		UtfDbTypeLayouts.StaticData("background"),
		UtfDbTypeLayouts.StaticData("static_load"),
		UtfDbTypeLayouts.StaticData("static_save"),
		UtfDbTypeLayouts.StaticData("static_file"),
		UtfDbTypeLayouts.ButtonData("open"),
		UtfDbTypeLayouts.ButtonData("save"),
		UtfDbTypeLayouts.ButtonData("cancel"),
		UtfDbTypeLayouts.EditData("edit_file"),
		UtfDbTypeLayouts.ListboxData("list")
	]);

	public string TypeName => "GT_SYSTEM_KIT_SAVELOAD";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_SYSTEM_KIT_SAVELOAD ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_SYSTEM_KIT_SAVELOAD {
			ScreenRect = UtfDbTypeParserHelpers.ParseRect(rawData, ref offset),
			Background = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticLoad = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticSave = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			StaticFile = UtfDbTypeParserHelpers.ParseStaticData(rawData, ref offset),
			Open = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			Save = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			Cancel = UtfDbTypeParserHelpers.ParseButtonData(rawData, ref offset),
			EditFile = UtfDbTypeParserHelpers.ParseEditData(rawData, ref offset),
			List = UtfDbTypeParserHelpers.ParseListboxData(rawData, ref offset)
		};
	}
}

internal sealed class GT_GLOBAL_SOUNDSTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_GLOBAL_SOUNDS", "field", [
		new ScalarSpec("default_button", ScalarKind.U4),
		new ScalarSpec("system_select", ScalarKind.U4),
		new ScalarSpec("build_confirm", ScalarKind.U4),
		new ScalarSpec("move_confirm", ScalarKind.U4),
		new ScalarSpec("start_construction", ScalarKind.U4),
		new ScalarSpec("end_construction", ScalarKind.U4),
		new ScalarSpec("zero_money", ScalarKind.U4),
		new ScalarSpec("light_industry_button", ScalarKind.U4),
		new ScalarSpec("heavy_industry_button", ScalarKind.U4),
		new ScalarSpec("hitech_industry_button", ScalarKind.U4),
		new ScalarSpec("hq_button", ScalarKind.U4),
		new ScalarSpec("research_button", ScalarKind.U4),
		new ScalarSpec("planet_depleted", ScalarKind.U4),
		new ScalarSpec("harvest_redeploy", ScalarKind.U4),
		new ScalarSpec("research_completed", ScalarKind.U4)
	]);

	public string TypeName => "GT_GLOBAL_SOUNDS";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_GLOBAL_SOUNDS ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		return new GT_GLOBAL_SOUNDS {
			DefaultButton = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			SystemSelect = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			BuildConfirm = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			MoveConfirm = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			StartConstruction = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			EndConstruction = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			ZeroMoney = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			LightIndustryButton = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			HeavyIndustryButton = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			HitechIndustryButton = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			HqButton = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			ResearchButton = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			PlanetDepleted = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			HarvestRedeploy = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			ResearchCompleted = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset)
		};
	}
}

internal sealed class GT_GLOBAL_VALUESTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("GT_GLOBAL_VALUES", "field", [
		new ScalarSpec("targeting_values.move_penalty_self", ScalarKind.F4),
		new ScalarSpec("targeting_values.move_penalty_target", ScalarKind.F4),
		new ScalarSpec("targeting_values.min_accuracy", ScalarKind.F4),
		new RepeatSpec(new ScalarSpec("individual_kill_chart", ScalarKind.U4), 6),
		new RepeatSpec(new ScalarSpec("admiral_kill_chart", ScalarKind.U4), 6),
		new RepeatSpec(new ScalarSpec("race_bonuses", ScalarKind.F4), 5),
		new RepeatSpec(new ScalarSpec("tech_upgrades", ScalarKind.F4), 390)
	]);

	public string TypeName => "GT_GLOBAL_VALUES";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue);

	private static GT_GLOBAL_VALUES ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var movePenaltySelf = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset);
		var movePenaltyTarget = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset);
		var minAccuracy = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset);
		var individualKillChart = new uint[6];
		var admiralKillChart = new uint[6];
		var raceBonuses = new float[5];
		for (var index = 0; index < individualKillChart.Length; index++) individualKillChart[index] = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		for (var index = 0; index < admiralKillChart.Length; index++) admiralKillChart[index] = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		for (var index = 0; index < raceBonuses.Length; index++) raceBonuses[index] = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset);
		var techUpgrades = new float[5][][];
		for (var race = 0; race < 5; race++) {
			techUpgrades[race] = new float[13][];
			for (var group = 0; group < 13; group++) {
				techUpgrades[race][group] = new float[6];
				for (var index = 0; index < 6; index++) techUpgrades[race][group][index] = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset);
			}
		}

		return new GT_GLOBAL_VALUES {
			MovePenaltySelf = movePenaltySelf,
			MovePenaltyTarget = movePenaltyTarget,
			MinAccuracy = minAccuracy,
			IndividualKillChart = individualKillChart,
			AdmiralKillChart = admiralKillChart,
			RaceBonuses = raceBonuses,
			TechUpgrades = techUpgrades
		};
	}
}
