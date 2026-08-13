using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal static class BTBatch4TypeParserSupport {
	public static UtfDbField Annotate(UtfDbField field) {
		if (field.Label.EndsWith(".relative_to", StringComparison.Ordinal)) return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((RELATIVE_TYPE)uint.Parse(field.Value)) };
		if (field.Label.EndsWith(".relation", StringComparison.Ordinal)) return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((RELATION)uint.Parse(field.Value)) };
		if (field.Label.EndsWith(".ai_type", StringComparison.Ordinal)) return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((AI_TYPE)uint.Parse(field.Value)) };
		if (field.Label.EndsWith(".advanced_placement.placement_type", StringComparison.Ordinal)) return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((PLACEMENT_TYPE)uint.Parse(field.Value)) };
		if (field.Label == "research.base_data.type") return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((RESEARCH_TYPE)uint.Parse(field.Value)) };
		if (field.Label == "research.base_data.base.obj_class") return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((OBJCLASS)uint.Parse(field.Value)) };
		if (field.Label == "research.base_data.base.b_edit_dropable") return field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0) };
		return field.Label switch {
			"res_finish_subtitle" or "research.res_finish_subtitle" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((RESEARCH_SUBTITLE)uint.Parse(field.Value)) },
			"base_data.moon_platform" => field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0) },
			"free_form" or "admiral_attack_control" or "sys_map_active" => field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0) },
			"cloak_usage" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((FORMATION_CLOAK_USAGE)uint.Parse(field.Value)) },
			"formation_specials" => field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean((uint.Parse(field.Value) & 1) != 0) },
			"build_sound" or "begin_build_sfx" or "sfx_pod_release" or "ambient_sound" or "sfx" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((SFX_ID)uint.Parse(field.Value)) },
			"bo_class" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((BUILDOBJCLASS)uint.Parse(field.Value)) },
			"trigger_type" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((TRIGGER_TYPE)uint.Parse(field.Value)) },
			"fx_class" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((EFFECTCLASS)uint.Parse(field.Value)) },
			"type.type" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((LAUNCHCLASS)uint.Parse(field.Value)) },
			"wpn_class.wpn_class" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((WPNCLASS)uint.Parse(field.Value)) },
			_ => BTBatch3TypeParserSupport.Annotate(field)
		};
	}

	public static string[] ReadAscii64Array(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new string[count];
		for (var i = 0; i < count; i++) values[i] = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref offset);
		return values;
	}

	public static BT_DRONE_RELEASE[] ReadDroneReleaseArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new BT_DRONE_RELEASE[count];
		for (var i = 0; i < count; i++) values[i] = UtfDbTypeParserHelpers.ParseBtDroneRelease(rawData, ref offset);
		return values;
	}

	public static BT_FLEET_GROUP_DEF[] ReadFleetGroupArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new BT_FLEET_GROUP_DEF[count];
		for (var i = 0; i < count; i++) values[i] = UtfDbTypeParserHelpers.ParseBtFleetGroupDef(rawData, ref offset);
		return values;
	}
}

internal sealed class BT_RESEARCHTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_RESEARCH", "field", [UtfDbTypeLayouts.BaseResearchData("base_data"), UtfDbTypeLayouts.SingleTechnode("research_tech"), UtfDbTypeLayouts.SingleTechnode("dependancy"), new ScalarSpec("res_finished_sound", ScalarKind.Ascii32), new ScalarSpec("res_finish_subtitle", ScalarKind.U4)]);
	public string TypeName => "BT_RESEARCH";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_RESEARCH ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBaseResearchData(rawData, ref o); return new BT_RESEARCH { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, ResearchTech = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref o), Dependancy = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref o), ResFinishedSound = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), ResFinishSubtitle = (RESEARCH_SUBTITLE)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; }
}

internal sealed class BT_UPGRADETypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_UPGRADE", "field", [UtfDbTypeLayouts.BaseResearchData("base_data"), UtfDbTypeLayouts.SingleTechnode("dependancy"), new ScalarSpec("extension_id", ScalarKind.U4), new ScalarSpec("res_finished_sound", ScalarKind.Ascii32), new ScalarSpec("res_finish_subtitle", ScalarKind.U4)]);
	public string TypeName => "BT_UPGRADE";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_UPGRADE ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBaseResearchData(rawData, ref o); return new BT_UPGRADE { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, Dependancy = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref o), ExtensionId = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), ResFinishedSound = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), ResFinishSubtitle = (RESEARCH_SUBTITLE)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; }
}

internal sealed class BT_ADMIRAL_RESTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_ADMIRAL_RES", "field", [UtfDbTypeLayouts.BaseResearchData("research.base_data"), UtfDbTypeLayouts.SingleTechnode("research.research_tech"), UtfDbTypeLayouts.SingleTechnode("research.dependancy"), new ScalarSpec("research.res_finished_sound", ScalarKind.Ascii32), new ScalarSpec("research.res_finish_subtitle", ScalarKind.U4), new ScalarSpec("flagship_type", ScalarKind.Ascii32)]);
	public string TypeName => "BT_ADMIRAL_RES";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_ADMIRAL_RES ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBaseResearchData(rawData, ref o); var research = new BT_RESEARCH { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, ResearchTech = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref o), Dependancy = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref o), ResFinishedSound = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), ResFinishSubtitle = (RESEARCH_SUBTITLE)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; return new BT_ADMIRAL_RES { ObjClass = research.ObjClass, BEditDropable = research.BEditDropable, Research = research, FlagshipType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o) }; }
}

internal sealed class BT_COMMAND_KITTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_COMMAND_KIT", "field", [UtfDbTypeLayouts.BaseResearchData("base_data")]);
	public string TypeName => "BT_COMMAND_KIT";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_COMMAND_KIT ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBaseResearchData(rawData, ref o); return new BT_COMMAND_KIT { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, KitBonuses = UtfDbTypeParserHelpers.ParseBtAdmiralBonuses(rawData, ref o), ButtonInfo = UtfDbTypeParserHelpers.ParseBtArtifactButtonInfo(rawData, ref o), Dependancy = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref o), Formations = BTBatch2TypeParserSupport.ReadAscii32Array(rawData, ref o, 3) }; }
}

internal sealed class BT_FABRICATOR_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_FABRICATOR_DATA", "field", [UtfDbTypeLayouts.BaseSpaceshipData("base_data"), new RepeatSpec(UtfDbTypeLayouts.DroneRelease("drone_release"), 2), new PaddingSpec("padding", 2), new ScalarSpec("repair_rate", ScalarKind.F4), new ScalarSpec("max_queue_size", ScalarKind.U4), new ScalarSpec("build_sound", ScalarKind.U4), new ScalarSpec("begin_build_sfx", ScalarKind.U4)]);
	public string TypeName => "BT_FABRICATOR_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_FABRICATOR_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBaseSpaceshipData(rawData, ref o); var value = new BT_FABRICATOR_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, DroneRelease = BTBatch4TypeParserSupport.ReadDroneReleaseArray(rawData, ref o, 2) }; o += 2; value.RepairRate = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o); value.MaxQueueSize = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o); value.BuildSound = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o); value.BeginBuildSfx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o); return value; }
}

internal sealed class BT_FIGHTER_WINGTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_FIGHTER_WING", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseLauncher("type"), new ScalarSpec("base_air_accuracy", ScalarKind.F4), new ScalarSpec("base_ground_accuracy", ScalarKind.F4), new ScalarSpec("max_fighters", ScalarKind.U4), new ScalarSpec("max_cap_fighters", ScalarKind.U4), new ScalarSpec("max_wing_fighters", ScalarKind.U4), new ScalarSpec("min_launch_period", ScalarKind.F4), new ScalarSpec("cost_of_new_fighter", ScalarKind.U4), new ScalarSpec("cost_of_refueling", ScalarKind.U4), new ScalarSpec("special_weapon", ScalarKind.U4), UtfDbTypeLayouts.SingleTechnode("needed_tech"), new ScalarSpec("animation", ScalarKind.Ascii32), new ScalarSpec("hardpoint", ScalarKind.Ascii64)]);
	public string TypeName => "BT_FIGHTER_WING";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_FIGHTER_WING ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_FIGHTER_WING { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref o), BaseAirAccuracy = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), BaseGroundAccuracy = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), MaxFighters = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), MaxCapFighters = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), MaxWingFighters = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), MinLaunchPeriod = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), CostOfNewFighter = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), CostOfRefueling = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), SpecialWeapon = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), NeededTech = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref o), Animation = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), Hardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o) }; }
}

internal sealed class BT_FLAGSHIP_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_FLAGSHIP_DATA", "field", [UtfDbTypeLayouts.BaseSpaceshipData("base_data")]);
	public string TypeName => "BT_FLAGSHIP_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_FLAGSHIP_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBaseSpaceshipData(rawData, ref o); return new BT_FLAGSHIP_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, AttackRadius = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), AdmiralBonuses = UtfDbTypeParserHelpers.ParseBtAdmiralBonuses(rawData, ref o), CommandKits = BTBatch2TypeParserSupport.ReadAscii32Array(rawData, ref o, 14), StartingFormations = BTBatch2TypeParserSupport.ReadAscii32Array(rawData, ref o, 2), ToolbarInfo = UtfDbTypeParserHelpers.ParseBtArtifactButtonInfo(rawData, ref o), MaxQueueSize = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; }
}

internal sealed class BT_FORMATIONTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_FORMATION", "field", [UtfDbTypeLayouts.BasicData()]);
	public string TypeName => "BT_FORMATION";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_FORMATION ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_FORMATION { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, ButtonInfo = UtfDbTypeParserHelpers.ParseBtArtifactButtonInfo(rawData, ref o), Groups = BTBatch4TypeParserSupport.ReadFleetGroupArray(rawData, ref o, 8), FreeForm = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) != 0, MoveTargetRange = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), SpotterRange = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), CloakUsage = (FORMATION_CLOAK_USAGE)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), AdmiralAttackControl = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) != 0, FormationBonuses = UtfDbTypeParserHelpers.ParseBtAdmiralBonuses(rawData, ref o), FormationSpecials = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; }
}

internal sealed class BT_PLAT_GUNTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_PLAT_GUN", "field", [UtfDbTypeLayouts.BasePlatformData("base_data"), new ScalarSpec("outer_weapon_range", ScalarKind.F4), new ScalarSpec("no_line_of_sight", ScalarKind.U1), new RepeatSpec(new ScalarSpec("launcher_type", ScalarKind.Ascii32), 5), new ScalarSpec("special_launcher_type", ScalarKind.Ascii32)]);
	public string TypeName => "BT_PLAT_GUN";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_PLAT_GUN ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBasePlatformData(rawData, ref o); return new BT_PLAT_GUN { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, OuterWeaponRange = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), NoLineOfSight = UtfDbTypeParserHelpers.ReadBooleanU1(rawData, ref o), LauncherType = BTBatch2TypeParserSupport.ReadAscii32Array(rawData, ref o, 5), SpecialLauncherType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o) }; }
}

internal sealed class BT_PLAT_BUILD_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_PLAT_BUILD_DATA", "field", [UtfDbTypeLayouts.BasePlatformData("base_data"), new ScalarSpec("ship_hardpoint", ScalarKind.Ascii64), new RepeatSpec(UtfDbTypeLayouts.DroneRelease("drone_release"), 2), new PaddingSpec("padding", 2), new ScalarSpec("build_rate", ScalarKind.S4), new ScalarSpec("max_queue_size", ScalarKind.U4)]);
	public string TypeName => "BT_PLAT_BUILD_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_PLAT_BUILD_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBasePlatformData(rawData, ref o); var value = new BT_PLAT_BUILD_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, ShipHardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o), DroneRelease = BTBatch4TypeParserSupport.ReadDroneReleaseArray(rawData, ref o, 2) }; o += 2; value.BuildRate = UtfDbTypeParserHelpers.ReadInt32(rawData, ref o); value.MaxQueueSize = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o); return value; }
}

internal sealed class BT_PLAT_BUILDSUP_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_PLAT_BUILDSUP_DATA", "field", [UtfDbTypeLayouts.BasePlatformData("base_data"), new ScalarSpec("ship_hardpoint", ScalarKind.Ascii64), new RepeatSpec(UtfDbTypeLayouts.DroneRelease("drone_release"), 2), new PaddingSpec("padding", 2), new ScalarSpec("build_rate", ScalarKind.S4), new ScalarSpec("max_queue_size", ScalarKind.U4), new ScalarSpec("supply_radius", ScalarKind.F4), new ScalarSpec("supply_per_second", ScalarKind.F4)]);
	public string TypeName => "BT_PLAT_BUILDSUP_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_PLAT_BUILDSUP_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBasePlatformData(rawData, ref o); var value = new BT_PLAT_BUILDSUP_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, ShipHardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o), DroneRelease = BTBatch4TypeParserSupport.ReadDroneReleaseArray(rawData, ref o, 2) }; o += 2; value.BuildRate = UtfDbTypeParserHelpers.ReadInt32(rawData, ref o); value.MaxQueueSize = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o); value.SupplyRadius = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o); value.SupplyPerSecond = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o); return value; }
}

internal sealed class BT_PLAT_REFINE_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_PLAT_REFINE_DATA", "field", [UtfDbTypeLayouts.BasePlatformData("base_data"), new ScalarSpec("ship_hardpoint", ScalarKind.Ascii64), new ScalarSpec("dock_hardpoint", ScalarKind.Ascii64), new RepeatSpec(UtfDbTypeLayouts.DroneRelease("drone_release"), 2), new PaddingSpec("padding", 2), new ScalarSpec("build_rate", ScalarKind.S4), new ScalarSpec("max_queue_size", ScalarKind.U4), new ScalarSpec("harvester_archetype", ScalarKind.Ascii32), new RepeatSpec(new ScalarSpec("gas_rate", ScalarKind.F4), 4), new RepeatSpec(new ScalarSpec("metal_rate", ScalarKind.F4), 4), new RepeatSpec(new ScalarSpec("crew_rate", ScalarKind.F4), 4)]);
	public string TypeName => "BT_PLAT_REFINE_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_PLAT_REFINE_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBasePlatformData(rawData, ref o); var value = new BT_PLAT_REFINE_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, ShipHardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o), DockHardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o), DroneRelease = BTBatch4TypeParserSupport.ReadDroneReleaseArray(rawData, ref o, 2) }; o += 2; value.BuildRate = UtfDbTypeParserHelpers.ReadInt32(rawData, ref o); value.MaxQueueSize = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o); value.HarvesterArchetype = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o); value.GasRate = BTBatch3TypeParserSupport.ReadSingleArray(rawData, ref o, 4); value.MetalRate = BTBatch3TypeParserSupport.ReadSingleArray(rawData, ref o, 4); value.CrewRate = BTBatch3TypeParserSupport.ReadSingleArray(rawData, ref o, 4); return value; }
}

internal sealed class BT_PLAT_REPAIR_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_PLAT_REPAIR_DATA", "field", [UtfDbTypeLayouts.BasePlatformData("base_data"), new ScalarSpec("supply_per_second", ScalarKind.F4), new ScalarSpec("repair_rate", ScalarKind.U4), new ScalarSpec("supply_rate", ScalarKind.U4), new ScalarSpec("supply_range", ScalarKind.F4), new ScalarSpec("repair_drone_type", ScalarKind.Ascii32), new ScalarSpec("repair_drone_hardpoint", ScalarKind.Ascii32)]);
	public string TypeName => "BT_PLAT_REPAIR_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_PLAT_REPAIR_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBasePlatformData(rawData, ref o); return new BT_PLAT_REPAIR_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, SupplyPerSecond = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), RepairRate = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), SupplyRate = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), SupplyRange = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), RepairDroneType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), RepairDroneHardpoint = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o) }; }
}

internal sealed class BT_PLAT_GENERAL_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_PLAT_GENERAL_DATA", "field", [UtfDbTypeLayouts.BasePlatformData("base_data")]);
	public string TypeName => "BT_PLAT_GENERAL_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_PLAT_GENERAL_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBasePlatformData(rawData, ref o); return new BT_PLAT_GENERAL_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd }; }
}

internal sealed class BT_PLAT_SELL_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_PLAT_SELL_DATA", "field", [UtfDbTypeLayouts.BasePlatformData("base_data"), new ScalarSpec("ship_hardpoint", ScalarKind.Ascii64), new ScalarSpec("max_queue_size", ScalarKind.U4)]);
	public string TypeName => "BT_PLAT_SELL_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_PLAT_SELL_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBasePlatformData(rawData, ref o); return new BT_PLAT_SELL_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, ShipHardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o), MaxQueueSize = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; }
}

internal sealed class BT_PLAT_JUMP_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_PLAT_JUMP_DATA", "field", [UtfDbTypeLayouts.BasePlatformData("base_data")]);
	public string TypeName => "BT_PLAT_JUMP_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_PLAT_JUMP_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBasePlatformData(rawData, ref o); return new BT_PLAT_JUMP_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd }; }
}

internal sealed class BT_SCRIPTOBJECTTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_SCRIPTOBJECT", "field", [UtfDbTypeLayouts.BasicData(), new ScalarSpec("file_name", ScalarKind.Ascii32), UtfDbTypeLayouts.MissionData("mission_data"), new ScalarSpec("ambient_sound", ScalarKind.U4), UtfDbTypeLayouts.BlinkerData("blinkers"), new ScalarSpec("ambient_animation", ScalarKind.Ascii32), new ScalarSpec("sys_map_active", ScalarKind.U4)]);
	public string TypeName => "BT_SCRIPTOBJECT";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_SCRIPTOBJECT ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_SCRIPTOBJECT { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, FileName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), MissionData = UtfDbTypeParserHelpers.ParseBtMissionData(rawData, ref o), AmbientSound = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), Blinkers = UtfDbTypeParserHelpers.ParseBtBlinkerData(rawData, ref o), AmbientAnimation = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), SysMapActive = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) != 0 }; }
}

internal sealed class BT_SHIPLAUNCHTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_SHIPLAUNCH", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseLauncher("type"), new ScalarSpec("hardpoint", ScalarKind.Ascii64), new ScalarSpec("animation", ScalarKind.Ascii32)]);
	public string TypeName => "BT_SHIPLAUNCH";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_SHIPLAUNCH ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_SHIPLAUNCH { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref o), Hardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o), Animation = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o) }; }
}

internal sealed class BT_SOLARIAN_BUILDTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_SOLARIAN_BUILD", "field", [UtfDbTypeLayouts.BasicData(), new ScalarSpec("bo_class", ScalarKind.U4), new ScalarSpec("cocoon_texture_name", ScalarKind.Ascii32)]);
	public string TypeName => "BT_SOLARIAN_BUILD";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_SOLARIAN_BUILD ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_SOLARIAN_BUILD { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, BoClass = (BUILDOBJCLASS)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), CocoonTextureName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o) }; }
}

internal sealed class BT_TRACTOR_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_TRACTOR_DATA", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseWeaponData("wpn_class"), new ScalarSpec("file_name", ScalarKind.Ascii32), new ScalarSpec("hardpoint", ScalarKind.Ascii64), new ScalarSpec("contact_blast_type", ScalarKind.Ascii32), new ScalarSpec("launch_sfx", ScalarKind.U4), new ScalarSpec("duration", ScalarKind.F4), new ScalarSpec("damage_per_second", ScalarKind.F4), UtfDbTypeLayouts.SingleTechnode("needed_tech"), new ScalarSpec("supply_cost", ScalarKind.S4), new ScalarSpec("refire_period", ScalarKind.F4)]);
	public string TypeName => "BT_TRACTOR_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_TRACTOR_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_TRACTOR_DATA { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, WpnClass = UtfDbTypeParserHelpers.ParseBaseWeaponData(rawData, ref o), FileName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), Hardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o), ContactBlastType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), LaunchSfx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), Duration = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), DamagePerSecond = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), NeededTech = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref o), SupplyCost = UtfDbTypeParserHelpers.ReadInt32(rawData, ref o), RefirePeriod = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o) }; }
}

internal sealed class BT_TRIGGERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_TRIGGER", "field", [UtfDbTypeLayouts.BasicData(), new ScalarSpec("file_name", ScalarKind.Ascii32), UtfDbTypeLayouts.MissionData("mission_data"), new ScalarSpec("trigger_type", ScalarKind.U4), new ScalarSpec("size", ScalarKind.F4)]);
	public string TypeName => "BT_TRIGGER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_TRIGGER ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_TRIGGER { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, FileName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), MissionData = UtfDbTypeParserHelpers.ParseBtMissionData(rawData, ref o), TriggerType = (TRIGGER_TYPE)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), Size = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o) }; }
}

internal sealed class BT_TROOPPOD_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_TROOPPOD_DATA", "field", [UtfDbTypeLayouts.BasicData(), new ScalarSpec("fx_class", ScalarKind.U4), new ScalarSpec("pod_type", ScalarKind.Ascii32), new RepeatSpec(new ScalarSpec("pod_hardpoints", ScalarKind.Ascii64), 3)]);
	public string TypeName => "BT_TROOPPOD_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_TROOPPOD_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_TROOPPOD_DATA { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, FxClass = (EFFECTCLASS)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), PodType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), PodHardpoints = BTBatch4TypeParserSupport.ReadAscii64Array(rawData, ref o, 3) }; }
}

internal sealed class BT_TROOPSHIP_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_TROOPSHIP_DATA", "field", [UtfDbTypeLayouts.BaseSpaceshipData("base_data"), new ScalarSpec("damage_potential", ScalarKind.U4), new ScalarSpec("assault_range", ScalarKind.F4), new ScalarSpec("sfx_pod_release", ScalarKind.U4)]);
	public string TypeName => "BT_TROOPSHIP_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_TROOPSHIP_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBaseSpaceshipData(rawData, ref o); return new BT_TROOPSHIP_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, DamagePotential = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), AssaultRange = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), SfxPodRelease = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; }
}

internal sealed class BT_TURRETTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_TURRET", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseLauncher("type"), new ScalarSpec("info", ScalarKind.F4), new ScalarSpec("animation", ScalarKind.Ascii32), new ScalarSpec("hardpoint", ScalarKind.Ascii64), new ScalarSpec("joint", ScalarKind.Ascii64), new ScalarSpec("anim_muzzle_flash", ScalarKind.Ascii32), new ScalarSpec("muzzle_flash_width", ScalarKind.U4), new ScalarSpec("muzzle_flash_time", ScalarKind.F4), UtfDbTypeLayouts.Color("color_mod"), new PaddingSpec("padding", 1), new GroupSpec("warm_up_blast", [new ScalarSpec("blast_type", ScalarKind.Ascii32), new ScalarSpec("trigger_time", ScalarKind.F4)])]);
	public string TypeName => "BT_TURRET";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_TURRET ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); var value = new BT_TURRET { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref o), Info = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), Animation = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), Hardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o), Joint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref o), AnimMuzzleFlash = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), MuzzleFlashWidth = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), MuzzleFlashTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), ColorMod = UtfDbTypeParserHelpers.ParseBtColor(rawData, ref o) }; o += 1; value.WarmUpBlast = UtfDbTypeParserHelpers.ParseBtTurretWarmUpBlast(rawData, ref o); return value; }
}

internal sealed class BT_UI_ANIMTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_UI_ANIM", "field", [UtfDbTypeLayouts.BasicData(), new ScalarSpec("effect_type", ScalarKind.Ascii32), new ScalarSpec("total_time", ScalarKind.F4), new ScalarSpec("sfx", ScalarKind.U4)]);
	public string TypeName => "BT_UI_ANIM";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch4TypeParserSupport.Annotate);
	private static BT_UI_ANIM ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_UI_ANIM { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, EffectType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), TotalTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), Sfx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; }
}
