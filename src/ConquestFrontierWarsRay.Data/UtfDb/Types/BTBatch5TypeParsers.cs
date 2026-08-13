using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal static class BTBatch5TypeParserSupport {
	public static UtfDbField Annotate(UtfDbField field) {
		if (field.Label.EndsWith(".number_func", StringComparison.Ordinal) || field.Label.EndsWith(".size_func", StringComparison.Ordinal) || field.Label.EndsWith(".moon_number_func", StringComparison.Ordinal)) {
			return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((MAP_DMAP_FUNC)uint.Parse(field.Value)) };
		}
		if (field.Label.EndsWith(".overlap", StringComparison.Ordinal)) return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((MAP_OVERLAP)uint.Parse(field.Value)) };
		if (field.Label.EndsWith(".placement", StringComparison.Ordinal)) return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((MAP_PLACEMENT)uint.Parse(field.Value)) };
		if (field.Label.EndsWith(".operation", StringComparison.Ordinal)) return field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((MAP_MACRO_OPERATION)uint.Parse(field.Value)) };
		return field.Label switch {
			"base.obj_class" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((OBJCLASS)uint.Parse(field.Value)) },
			"base.b_edit_dropable" => field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0) },
			"type.type" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((LAUNCHCLASS)uint.Parse(field.Value)) },
			"wpn_class.wpn_class" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((WPNCLASS)uint.Parse(field.Value)) },
			"fx_class" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((EFFECTCLASS)uint.Parse(field.Value)) },
			"launch_sfx" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((SFX_ID)uint.Parse(field.Value)) },
			"active" or "themes[0].ok_for_player_start" or "themes[0].ok_for_remote_system" => field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0) },
			"themes[0].size_ok" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((MAP_SECTOR_SIZE)uint.Parse(field.Value)) },
			_ => BTBatch4TypeParserSupport.Annotate(field)
		};
	}

	public static string[] ReadAscii32Array(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new string[count];
		for (var i = 0; i < count; i++) values[i] = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset);
		return values;
	}

	public static BT_MINER_DRONE_RELEASE[] ReadMinerDroneReleaseArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new BT_MINER_DRONE_RELEASE[count];
		for (var i = 0; i < count; i++) values[i] = UtfDbTypeParserHelpers.ParseBtMinerDroneRelease(rawData, ref offset);
		return values;
	}

	public static BT_VERTICAL_SALVO[] ReadVerticalSalvoArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new BT_VERTICAL_SALVO[count];
		for (var i = 0; i < count; i++) values[i] = UtfDbTypeParserHelpers.ParseBtVerticalSalvo(rawData, ref offset);
		return values;
	}

	public static BT_MAP_TERRAIN_THEME[] ReadMapTerrainThemeArray(ReadOnlySpan<byte> rawData, ref int offset, int count) {
		var values = new BT_MAP_TERRAIN_THEME[count];
		for (var i = 0; i < count; i++) values[i] = UtfDbTypeParserHelpers.ParseBtMapTerrainTheme(rawData, ref offset);
		return values;
	}

	public static GroupSpec MapTerrainInfo(string label) => new(label, [
		new ScalarSpec("terrain_arch_type", ScalarKind.Ascii32),
		new ScalarSpec("probability", ScalarKind.F4),
		new ScalarSpec("min_to_place", ScalarKind.U4),
		new ScalarSpec("max_to_place", ScalarKind.U4),
		new ScalarSpec("number_func", ScalarKind.U4),
		new ScalarSpec("size", ScalarKind.U4),
		new ScalarSpec("required_to_place", ScalarKind.U4),
		new ScalarSpec("overlap", ScalarKind.U4),
		new ScalarSpec("placement", ScalarKind.U4)
	]);

	public static GroupSpec MapMacro(string label) => new(label, [
		new ScalarSpec("operation", ScalarKind.U4),
		new ScalarSpec("range", ScalarKind.U4),
		new ScalarSpec("active", ScalarKind.U4),
		MapTerrainInfo("info")
	]);

	public static GroupSpec MapTerrainTheme(string label) => new(label, [
		new RepeatSpec(new ScalarSpec("system_kit", ScalarKind.Ascii32), 6),
		new RepeatSpec(new ScalarSpec("metal_planets", ScalarKind.Ascii32), 6),
		new RepeatSpec(new ScalarSpec("gas_planets", ScalarKind.Ascii32), 6),
		new RepeatSpec(new ScalarSpec("habitable_planets", ScalarKind.Ascii32), 6),
		new RepeatSpec(new ScalarSpec("other_planets", ScalarKind.Ascii32), 6),
		new RepeatSpec(new ScalarSpec("moon_types", ScalarKind.Ascii32), 6),
		new ScalarSpec("size_ok", ScalarKind.U4),
		new ScalarSpec("min_size", ScalarKind.U4),
		new ScalarSpec("max_size", ScalarKind.U4),
		new ScalarSpec("size_func", ScalarKind.U4),
		new RepeatSpec(new ScalarSpec("num_habitable_planets", ScalarKind.U4), 3),
		new RepeatSpec(new ScalarSpec("num_metal_planets", ScalarKind.U4), 3),
		new RepeatSpec(new ScalarSpec("num_gas_planets", ScalarKind.U4), 3),
		new RepeatSpec(new ScalarSpec("num_other_planets", ScalarKind.U4), 3),
		new ScalarSpec("min_moons_per_planet", ScalarKind.U4),
		new ScalarSpec("max_moons_per_planet", ScalarKind.U4),
		new ScalarSpec("moon_number_func", ScalarKind.U4),
		new RepeatSpec(new ScalarSpec("num_nugget_patches_metal", ScalarKind.U4), 3),
		new RepeatSpec(new ScalarSpec("num_nugget_patches_gas", ScalarKind.U4), 3),
		new RepeatSpec(MapTerrainInfo("terrain"), 20),
		new RepeatSpec(MapTerrainInfo("nugget_metal_types"), 6),
		new RepeatSpec(MapTerrainInfo("nugget_gas_types"), 6),
		new ScalarSpec("flags_raw", ScalarKind.U4),
		new RepeatSpec(new ScalarSpec("density", ScalarKind.F4), 3),
		new RepeatSpec(MapMacro("macros"), 15)
	]);
}

internal sealed class BT_ANMBOLT_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_ANMBOLT_DATA", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseWeaponData("wpn_class"), new ScalarSpec("file_name", ScalarKind.Ascii32), new ScalarSpec("launch_sfx", ScalarKind.U4), new ScalarSpec("damage", ScalarKind.U4), new ScalarSpec("max_velocity", ScalarKind.F4), new ScalarSpec("blast_type", ScalarKind.Ascii32), new ScalarSpec("engine_trail_type", ScalarKind.Ascii32), new ScalarSpec("anim_file", ScalarKind.Ascii32), new ScalarSpec("bolt_size", ScalarKind.F4)]);
	public string TypeName => "BT_ANMBOLT_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_ANMBOLT_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_ANMBOLT_DATA { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, WpnClass = UtfDbTypeParserHelpers.ParseBaseWeaponData(rawData, ref o), FileName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), LaunchSfx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), Damage = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), MaxVelocity = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), BlastType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), EngineTrailType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), AnimFile = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), BoltSize = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o) }; }
}

internal sealed class BT_GASHARVESTER_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_GASHARVESTER_DATA", "field", [UtfDbTypeLayouts.BasePlatformData("base_data"), new RepeatSpec(new GroupSpec("drone_release", [new ScalarSpec("hardpoint", ScalarKind.Ascii64), new ScalarSpec("builder_type", ScalarKind.Ascii32)]), 6), new ScalarSpec("max_miner_load", ScalarKind.U4), new ScalarSpec("mine_neb_time", ScalarKind.F4), new ScalarSpec("mine_radius", ScalarKind.F4), new ScalarSpec("ship_hardpoint", ScalarKind.Ascii32)]);
	public string TypeName => "BT_GASHARVESTER_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_GASHARVESTER_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBasePlatformData(rawData, ref o); return new BT_GASHARVESTER_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, DroneRelease = BTBatch5TypeParserSupport.ReadMinerDroneReleaseArray(rawData, ref o, 6), MaxMinerLoad = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), MineNebTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), MineRadius = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), ShipHardpoint = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o) }; }
}

internal sealed class BT_LASERSPRAY_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_LASERSPRAY_DATA", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseWeaponData("wpn_class"), new ScalarSpec("file_name", ScalarKind.Ascii32), new ScalarSpec("launch_sfx", ScalarKind.U4), new ScalarSpec("damage", ScalarKind.U4), new ScalarSpec("lifetime", ScalarKind.F4), new ScalarSpec("max_sweep_dist", ScalarKind.F4), new ScalarSpec("beam_width", ScalarKind.F4), new ScalarSpec("velocity", ScalarKind.F4), new ScalarSpec("contact_blast", ScalarKind.Ascii32)]);
	public string TypeName => "BT_LASERSPRAY_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_LASERSPRAY_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_LASERSPRAY_DATA { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, WpnClass = UtfDbTypeParserHelpers.ParseBaseWeaponData(rawData, ref o), FileName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), LaunchSfx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), Damage = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), Lifetime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), MaxSweepDist = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), BeamWidth = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), Velocity = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), ContactBlast = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o) }; }
}

internal sealed class BT_MAP_GENTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_MAP_GEN", "field", [new RepeatSpec(BTBatch5TypeParserSupport.MapTerrainTheme("themes"), 30)]);
	public string TypeName => "BT_MAP_GEN";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_MAP_GEN ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; return new BT_MAP_GEN { Themes = BTBatch5TypeParserSupport.ReadMapTerrainThemeArray(rawData, ref o, 30) }; }
}

internal sealed class BT_OVERDRIVE_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_OVERDRIVE_DATA", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseWeaponData("wpn_class"), new ScalarSpec("speed", ScalarKind.F4), new ScalarSpec("launch_sfx", ScalarKind.U4)]);
	public string TypeName => "BT_OVERDRIVE_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_OVERDRIVE_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_OVERDRIVE_DATA { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, WpnClass = UtfDbTypeParserHelpers.ParseBaseWeaponData(rawData, ref o), Speed = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), LaunchSfx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; }
}

internal sealed class BT_RECOVERYSHIP_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_RECOVERYSHIP_DATA", "field", [UtfDbTypeLayouts.BaseSpaceshipData("base_data"), new ScalarSpec("recovery_radius", ScalarKind.F4), new ScalarSpec("recover_time", ScalarKind.F4), new ScalarSpec("beam_point_name1", ScalarKind.Ascii32), new ScalarSpec("beam_point_name2", ScalarKind.Ascii32)]);
	public string TypeName => "BT_RECOVERYSHIP_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_RECOVERYSHIP_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var bd = UtfDbTypeParserHelpers.ParseBtBaseSpaceshipData(rawData, ref o); return new BT_RECOVERYSHIP_DATA { ObjClass = bd.Base.ObjClass, BEditDropable = bd.Base.BEditDropable, BaseData = bd, RecoveryRadius = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), RecoverTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), BeamPointName1 = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), BeamPointName2 = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o) }; }
}

internal sealed class BT_SPECIALBOLT_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_SPECIALBOLT_DATA", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseWeaponData("wpn_class"), new ScalarSpec("file_name", ScalarKind.Ascii32), new ScalarSpec("launch_sfx", ScalarKind.U4), new ScalarSpec("damage", ScalarKind.U4), new ScalarSpec("max_velocity", ScalarKind.F4), new ScalarSpec("blast_type", ScalarKind.Ascii32), new ScalarSpec("spark_type", ScalarKind.Ascii32), UtfDbTypeLayouts.FlashData("flash"), new PaddingSpec("padding", 1), new ScalarSpec("mass", ScalarKind.F4), new GroupSpec("special", [new ScalarSpec("supply_damage", ScalarKind.U4), new ScalarSpec("shield_fraction", ScalarKind.F4), new ScalarSpec("move_fraction", ScalarKind.F4), new ScalarSpec("sensor_fraction", ScalarKind.F4)])]);
	public string TypeName => "BT_SPECIALBOLT_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_SPECIALBOLT_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); var value = new BT_SPECIALBOLT_DATA { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, WpnClass = UtfDbTypeParserHelpers.ParseBaseWeaponData(rawData, ref o), FileName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), LaunchSfx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), Damage = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o), MaxVelocity = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), BlastType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), SparkType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), Flash = UtfDbTypeParserHelpers.ParseFlashData(rawData, ref o) }; o += 1; value.Mass = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o); value.Special = UtfDbTypeParserHelpers.ParseBtSpecialDamage(rawData, ref o); return value; }
}

internal sealed class BT_TRACTOR_WAVE_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_TRACTOR_WAVE_LAUNCHER", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseLauncher("type"), new ScalarSpec("duration", ScalarKind.F4), new ScalarSpec("space_wave_type", ScalarKind.Ascii32), new ScalarSpec("wave_frequency", ScalarKind.F4), new ScalarSpec("wave_life_time", ScalarKind.F4)]);
	public string TypeName => "BT_TRACTOR_WAVE_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_TRACTOR_WAVE_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_TRACTOR_WAVE_LAUNCHER { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref o), Duration = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), SpaceWaveType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), WaveFrequency = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), WaveLifeTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o) }; }
}

internal sealed class BT_VERTICAL_LAUNCHTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_VERTICAL_LAUNCH", "field", [UtfDbTypeLayouts.BasicData(), UtfDbTypeLayouts.BaseLauncher("type"), new RepeatSpec(new GroupSpec("upgrade", [UtfDbTypeLayouts.SingleTechnode("tech_need"), new ScalarSpec("salvo", ScalarKind.U4)]), 4), new ScalarSpec("mini_refire", ScalarKind.F4), new RepeatSpec(new ScalarSpec("hardpoint", ScalarKind.Ascii64), 10)]);
	public string TypeName => "BT_VERTICAL_LAUNCH";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_VERTICAL_LAUNCH ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_VERTICAL_LAUNCH { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref o), Upgrade = BTBatch5TypeParserSupport.ReadVerticalSalvoArray(rawData, ref o, 4), MiniRefire = UtfDbTypeParserHelpers.ReadSingle(rawData, ref o), Hardpoint = BTBatch4TypeParserSupport.ReadAscii64Array(rawData, ref o, 10) }; }
}

internal sealed class BT_WAYPOINTTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_WAYPOINT", "field", [UtfDbTypeLayouts.BasicData(), new ScalarSpec("file_name", ScalarKind.Ascii32), UtfDbTypeLayouts.MissionData("mission_data")]);
	public string TypeName => "BT_WAYPOINT";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_WAYPOINT ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_WAYPOINT { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, FileName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref o), MissionData = UtfDbTypeParserHelpers.ParseBtMissionData(rawData, ref o) }; }
}

internal sealed class BT_WORMHOLE_EFFECTTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_WORMHOLE_EFFECT", "field", [UtfDbTypeLayouts.BasicData(), new ScalarSpec("fx_class", ScalarKind.U4)]);
	public string TypeName => "BT_WORMHOLE_EFFECT";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) => UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch5TypeParserSupport.Annotate);
	private static BT_WORMHOLE_EFFECT ParseTypedValue(ReadOnlySpan<byte> rawData) { var o = 0; var b = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref o); return new BT_WORMHOLE_EFFECT { ObjClass = b.ObjClass, BEditDropable = b.BEditDropable, FxClass = (EFFECTCLASS)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref o) }; }
}
