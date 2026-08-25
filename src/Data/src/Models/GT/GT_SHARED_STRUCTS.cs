using System.Numerics;
using ConquestFrontierWarsRay.Data.Models;

namespace ConquestFrontierWarsRay.Data.Models.GT;

public sealed record BUTTON_DATA {
	public string ButtonType { get; init; } = string.Empty;
	public uint ButtonText { get; init; }
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public RECT ButtonArea { get; init; } = new();
}

public sealed record STATIC_DATA {
	public string StaticType { get; init; } = string.Empty;
	public uint StaticText { get; init; }
	public uint StaticTooltip { get; init; }
	public uint StaticHintbox { get; init; }
	public uint Alignment { get; init; }
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public int Width { get; init; }
	public int Height { get; init; }
}

public sealed class ANIMATE_DATA {
	public string AnimateType { get; init; } = string.Empty;
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public uint Timer { get; init; }
	public bool FuzzEffect { get; init; }
}

public sealed class EDIT_DATA {
	public string EditType { get; init; } = string.Empty;
	public uint EditText { get; init; }
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
}

public sealed class LISTBOX_DATA {
	public string ListboxType { get; init; } = string.Empty;
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public RECT TextArea { get; init; } = new();
	public uint LeadingHeight { get; init; }
	public uint Flags { get; init; }
}

public sealed class DIPLOMACYBUTTON_DATA {
	public string ButtonType { get; init; } = string.Empty;
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
}

public sealed class SLIDER_DATA {
	public string SliderType { get; init; } = string.Empty;
	public RECT ScreenRect { get; init; } = new();
	public Vector2 Origin { get; init; } = Vector2.Zero;
}

public sealed class DROPDOWN_DATA {
	public string DropdownType { get; init; } = string.Empty;
	public RECT ScreenRect { get; init; } = new();
	public BUTTON_DATA ButtonData { get; init; } = new();
	public LISTBOX_DATA ListboxData { get; init; } = new();
}

public sealed class COMBOBOX_DATA {
	public string ComboboxType { get; init; } = string.Empty;
	public RECT ScreenRect { get; init; } = new();
	public EDIT_DATA EditData { get; init; } = new();
	public BUTTON_DATA ButtonData { get; init; } = new();
	public LISTBOX_DATA ListboxData { get; init; } = new();
}

public sealed class TABCONTROL_DATA {
	public string TabControlType { get; init; } = string.Empty;
	public string HotButtonType { get; init; } = string.Empty;
	public int BaseImage { get; init; }
	public int NumTabs { get; init; }
	public uint[] TextIds { get; init; } = [];
	public uint UpperTabs { get; init; }
	public int XPos { get; init; }
	public int YPos { get; init; }
}

public sealed class HOTSTATIC_DATA {
	public uint BaseImage { get; init; }
	public uint NumTechLevels { get; init; }
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public int Width { get; init; }
	public int Height { get; init; }
	public int BarStartX { get; init; }
	public uint BarSpacing { get; init; }
	public uint Text { get; init; }
	public GT_COLOR TextColor { get; init; } = new();
}

public sealed class HOTBUTTON_DATA {
	public uint BaseImage { get; init; }
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public uint ButtonText { get; init; }
	public uint ButtonInfo { get; init; }
	public uint ButtonHint { get; init; }
	public uint Hotkey { get; init; }
	public uint Disabled { get; init; }
}

public sealed class ICON_DATA {
	public uint BaseImage { get; init; }
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public uint Tooltip { get; init; }
}

public sealed class QUEUECONTROL_DATA {
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public int Width { get; init; }
	public int Height { get; init; }
}

public sealed class MULTIHOTBUTTON_DATA {
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public uint Hotkey { get; init; }
	public byte SingleShape { get; init; }
	public byte Disabled { get; init; }
}

public sealed class SINGLE_TECHNODE {
	public uint RaceId { get; init; }
	public uint Tech { get; init; }
	public uint Build { get; init; }
	public uint Common { get; init; }
	public uint CommonExtra { get; init; }
	public uint Cq2Vars1 { get; init; }
	public uint Cq2Vars2 { get; init; }
}

public sealed class BUILDBUTTON_DATA {
	public uint BaseImage { get; init; }
	public uint NoMoneyImage { get; init; }
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public string RtArchetype { get; init; } = string.Empty;
	public SINGLE_TECHNODE TechDependency { get; init; } = new();
	public SINGLE_TECHNODE TechGreyed { get; init; } = new();
	public uint GreyedTooltip { get; init; }
	public uint BuildInfo { get; init; }
	public uint Hotkey { get; init; }
	public byte Disabled { get; init; }
}

public sealed class RESEARCHBUTTON_DATA {
	public uint BaseImage { get; init; }
	public uint NoMoneyImage { get; init; }
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
	public string RtArchetype { get; init; } = string.Empty;
	public uint Tooltip { get; init; }
	public uint ResearchInfo { get; init; }
	public uint Hotkey { get; init; }
	public byte Disabled { get; init; }
}

public sealed class SHIPSILBUTTON_DATA {
	public int XOrigin { get; init; }
	public int YOrigin { get; init; }
}

public sealed class ARTIFACT_BUTTON_INFO {
	public uint BaseButton { get; init; }
	public uint Tooltip { get; init; }
	public uint HelpBox { get; init; }
	public uint HintBox { get; init; }
}

public sealed class GT_SYSTEM_KIT_LIGHT_INFO {
	public GT_COLOR Color { get; init; } = new();
	public int Range { get; init; }
	public GT_VECTOR Position { get; init; } = new();
	public GT_VECTOR Direction { get; init; } = new();
	public float Cutoff { get; init; }
	public bool Infinite { get; init; }
	public string Name { get; init; } = string.Empty;
	public bool Ambient { get; init; }
}
