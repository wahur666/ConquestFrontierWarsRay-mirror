using ConquestFrontierWarsRay.Data.Models;

namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed class GT_MESSAGEBOX {
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public STATIC_DATA Message { get; init; } = new();
	public BUTTON_DATA Ok { get; init; } = new();
	public BUTTON_DATA Cancel { get; init; } = new();
	public BUTTON_DATA OkAlone { get; init; } = new();
}

public sealed class GT_BRIEFING {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public BUTTON_DATA Start { get; init; } = new();
	public BUTTON_DATA Replay { get; init; } = new();
	public BUTTON_DATA Cancel { get; init; } = new();
	public RECT RcTeletype { get; init; } = new();
	public RECT[] RcComm { get; init; } = [];
	public ANIMATE_DATA[] AnimFuzz { get; init; } = [];
}

public sealed class GT_CHAT {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Ask { get; init; } = new();
	public EDIT_DATA Chatbox { get; init; } = new();
}

public sealed class GT_CREDITS {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA StaticBackground { get; init; } = new();
}

public sealed class GT_DIPLOMACYMENU {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA StaticTitle { get; init; } = new();
	public STATIC_DATA StaticName { get; init; } = new();
	public STATIC_DATA StaticRace { get; init; } = new();
	public STATIC_DATA StaticAllies { get; init; } = new();
	public STATIC_DATA StaticMetalTitle { get; init; } = new();
	public STATIC_DATA StaticGasTitle { get; init; } = new();
	public STATIC_DATA StaticCrewTitle { get; init; } = new();
	public STATIC_DATA[] StaticNames { get; init; } = [];
	public STATIC_DATA[] StaticRaces { get; init; } = [];
	public BUTTON_DATA[] ButtonCrew { get; init; } = [];
	public BUTTON_DATA[] ButtonMetal { get; init; } = [];
	public BUTTON_DATA[] ButtonGas { get; init; } = [];
	public BUTTON_DATA[] ButtonAllies { get; init; } = [];
	public STATIC_DATA StaticCrew { get; init; } = new();
	public STATIC_DATA StaticMetal { get; init; } = new();
	public STATIC_DATA StaticGas { get; init; } = new();
	public BUTTON_DATA ButtonOk { get; init; } = new();
	public BUTTON_DATA ButtonReset { get; init; } = new();
	public BUTTON_DATA ButtonCancel { get; init; } = new();
	public BUTTON_DATA ButtonApply { get; init; } = new();
	public DIPLOMACYBUTTON_DATA[] DiplomacyButtons { get; init; } = [];
}

public sealed class GT_IGOPTIONS {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public BUTTON_DATA ButtonSave { get; init; } = new();
	public BUTTON_DATA ButtonLoad { get; init; } = new();
	public BUTTON_DATA ButtonOptions { get; init; } = new();
	public BUTTON_DATA ButtonRestart { get; init; } = new();
	public BUTTON_DATA ButtonResign { get; init; } = new();
	public BUTTON_DATA ButtonAbdicate { get; init; } = new();
	public BUTTON_DATA ButtonReturn { get; init; } = new();
}

public sealed class GT_LOADSAVE {
	public RECT ScreenRect { get; init; } = new();
	public RECT ScreenRect2D { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA StaticLoad { get; init; } = new();
	public STATIC_DATA StaticSave { get; init; } = new();
	public STATIC_DATA StaticFile { get; init; } = new();
	public BUTTON_DATA Open { get; init; } = new();
	public BUTTON_DATA Save { get; init; } = new();
	public BUTTON_DATA Cancel { get; init; } = new();
	public BUTTON_DATA DeleteFile { get; init; } = new();
	public EDIT_DATA EditFile { get; init; } = new();
	public LISTBOX_DATA List { get; init; } = new();
}

public sealed class GT_MAPSELECT {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA StaticBackground { get; init; } = new();
	public STATIC_DATA StaticTitle { get; init; } = new();
	public STATIC_DATA StaticRandom { get; init; } = new();
	public STATIC_DATA StaticSupplied { get; init; } = new();
	public STATIC_DATA StaticSaved { get; init; } = new();
	public LISTBOX_DATA ListRandom { get; init; } = new();
	public LISTBOX_DATA ListSupplied { get; init; } = new();
	public LISTBOX_DATA ListSaved { get; init; } = new();
	public BUTTON_DATA ButtonOk { get; init; } = new();
	public BUTTON_DATA ButtonCancel { get; init; } = new();
}

public sealed class GT_MENUOBJECTIVES {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA StaticObjectives { get; init; } = new();
	public STATIC_DATA StaticName { get; init; } = new();
	public RECT RcTeletype { get; init; } = new();
	public BUTTON_DATA ButtonOk { get; init; } = new();
	public BUTTON_DATA[] CheckObjectives { get; init; } = [];
	public STATIC_DATA[] StaticObjectiveArray { get; init; } = [];
}

public sealed class GT_NEWPLAYER {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public STATIC_DATA StaticHeading { get; init; } = new();
	public EDIT_DATA Edit { get; init; } = new();
	public BUTTON_DATA Ok { get; init; } = new();
	public BUTTON_DATA Cancel { get; init; } = new();
}

public sealed class GT_OPTIONS {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA Title { get; init; } = new();
	public STATIC_DATA StaticName { get; init; } = new();
	public LISTBOX_DATA ListNames { get; init; } = new();
	public BUTTON_DATA ButtonNew { get; init; } = new();
	public BUTTON_DATA ButtonChange { get; init; } = new();
	public BUTTON_DATA ButtonDelete { get; init; } = new();
	public STATIC_DATA[] StaticFields { get; init; } = [];
	public SLIDER_DATA[] Sliders { get; init; } = [];
	public BUTTON_DATA[] PushButtons { get; init; } = [];
	public STATIC_DATA StaticGamma { get; init; } = new();
	public STATIC_DATA StaticResolution { get; init; } = new();
	public DROPDOWN_DATA DropResolution { get; init; } = new();
	public SLIDER_DATA SliderGamma { get; init; } = new();
	public STATIC_DATA[] GraphicsStatics { get; init; } = [];
	public BUTTON_DATA[] GraphicsPushButtons { get; init; } = [];
	public SLIDER_DATA SlideDrawBack { get; init; } = new();
	public SLIDER_DATA SliderShips3D { get; init; } = new();
	public STATIC_DATA StaticDevice { get; init; } = new();
	public DROPDOWN_DATA DropDevice { get; init; } = new();
	public STATIC_DATA Static3DHardware { get; init; } = new();
	public BUTTON_DATA Push3DHardware { get; init; } = new();
	public TABCONTROL_DATA Tab { get; init; } = new();
	public BUTTON_DATA ButtonOk { get; init; } = new();
	public BUTTON_DATA ButtonCancel { get; init; } = new();
}

public sealed class GT_PAUSE {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA StaticTitle { get; init; } = new();
	public STATIC_DATA[] StaticDescription { get; init; } = [];
}

public sealed class GT_PLAYERCHATMENU {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA[] StaticNames { get; init; } = [];
	public STATIC_DATA[] StaticRaces { get; init; } = [];
	public BUTTON_DATA[] CheckNames { get; init; } = [];
	public BUTTON_DATA ButtonAllies { get; init; } = new();
	public BUTTON_DATA ButtonEnemies { get; init; } = new();
	public BUTTON_DATA ButtonEveryone { get; init; } = new();
	public LISTBOX_DATA ListChat { get; init; } = new();
	public EDIT_DATA EditChat { get; init; } = new();
	public BUTTON_DATA ButtonClose { get; init; } = new();
	public STATIC_DATA StaticTitle { get; init; } = new();
	public STATIC_DATA StaticChat { get; init; } = new();
}

public sealed class GT_SPECIALABILITIES {
	public ARTIFACT_BUTTON_INFO[] Abilities { get; init; } = [];
}

public sealed class GT_SYSTEM_KIT {
	public string FileName { get; init; } = string.Empty;
	public uint NumLight { get; init; }
	public GT_SYSTEM_KIT_LIGHT_INFO[] LightInfo { get; init; } = [];
}

public sealed class GT_SYSTEM_KIT_SAVELOAD {
	public RECT ScreenRect { get; init; } = new();
	public STATIC_DATA Background { get; init; } = new();
	public STATIC_DATA StaticLoad { get; init; } = new();
	public STATIC_DATA StaticSave { get; init; } = new();
	public STATIC_DATA StaticFile { get; init; } = new();
	public BUTTON_DATA Open { get; init; } = new();
	public BUTTON_DATA Save { get; init; } = new();
	public BUTTON_DATA Cancel { get; init; } = new();
	public EDIT_DATA EditFile { get; init; } = new();
	public LISTBOX_DATA List { get; init; } = new();
}

public sealed class GT_GLOBAL_SOUNDS {
	public uint DefaultButton { get; init; }
	public uint SystemSelect { get; init; }
	public uint BuildConfirm { get; init; }
	public uint MoveConfirm { get; init; }
	public uint StartConstruction { get; init; }
	public uint EndConstruction { get; init; }
	public uint ZeroMoney { get; init; }
	public uint LightIndustryButton { get; init; }
	public uint HeavyIndustryButton { get; init; }
	public uint HitechIndustryButton { get; init; }
	public uint HqButton { get; init; }
	public uint ResearchButton { get; init; }
	public uint PlanetDepleted { get; init; }
	public uint HarvestRedeploy { get; init; }
	public uint ResearchCompleted { get; init; }
}

public sealed class GT_GLOBAL_VALUES {
	public float MovePenaltySelf { get; init; }
	public float MovePenaltyTarget { get; init; }
	public float MinAccuracy { get; init; }
	public uint[] IndividualKillChart { get; init; } = [];
	public uint[] AdmiralKillChart { get; init; } = [];
	public float[] RaceBonuses { get; init; } = [];
	public float[][][] TechUpgrades { get; init; } = [];
}
