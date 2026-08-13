using System.Xml.Linq;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal static class BTBatch1TypeParserSupport {
	public static UtfDbField Annotate(UtfDbField field) {
		return field.Label switch {
			"base.obj_class" => field with { EnumValue = UtfDbTypeParserHelpers.FormatEnum((OBJCLASS)uint.Parse(field.Value)) },
			"base.b_edit_dropable" => field with { EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0) },
			"type.type" or "base_launcher.type" or "base_launder.type" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((LAUNCHCLASS)uint.Parse(field.Value))
			},
			"wpn_class.wpn_class" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((WPNCLASS)uint.Parse(field.Value))
			},
			"fx_class" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((EFFECTCLASS)uint.Parse(field.Value))
			},
			"sound_fx" or "launch_sfx" or "sfx" or "warmup_sound" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((SFX_ID)uint.Parse(field.Value))
			},
			"special_ability" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatEnum((UNIT_SPECIAL_ABILITY)uint.Parse(field.Value))
			},
			"special" or "worm_weapon" or "self_target" or "draw_through_fog" => field with {
				EnumValue = UtfDbTypeParserHelpers.FormatBoolean(uint.Parse(field.Value) != 0)
			},
			_ => field
		};
	}
}

internal sealed class BT_AIR_DEFENSETypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_AIR_DEFENSE", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("hardpoint", ScalarKind.Ascii64),
		new ScalarSpec("base_accuracy", ScalarKind.F4),
		new ScalarSpec("flash_texture_name", ScalarKind.Ascii32),
		new ScalarSpec("flash_width", ScalarKind.F4),
		new ScalarSpec("flash_frequency", ScalarKind.F4),
		UtfDbTypeLayouts.Colora("flash_color"),
		new ScalarSpec("sound_fx", ScalarKind.U4)
	]);

	public string TypeName => "BT_AIR_DEFENSE";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_AIR_DEFENSE ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_AIR_DEFENSE {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			Hardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref offset),
			BaseAccuracy = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			FlashTextureName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset),
			FlashWidth = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			FlashFrequency = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			FlashColor = UtfDbTypeParserHelpers.ParseColora(rawData, ref offset),
			SoundFx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset)
		};
	}
}

internal sealed class BT_ARTIFACT_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_ARTIFACT_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("base_launder"),
		new ScalarSpec("artifact_name", ScalarKind.Ascii32)
	]);

	public string TypeName => "BT_ARTIFACT_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_ARTIFACT_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_ARTIFACT_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			BaseLaunder = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			ArtifactName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset)
		};
	}
}

internal sealed class BT_ARTILERY_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_ARTILERY_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("base_launcher"),
		new ScalarSpec("area_radius", ScalarKind.F4),
		new ScalarSpec("damage_per_sec", ScalarKind.U4),
		new ScalarSpec("special", ScalarKind.U1),
		new ScalarSpec("explosion_type", ScalarKind.Ascii32),
		new ScalarSpec("flash_type", ScalarKind.Ascii32)
	]);

	public string TypeName => "BT_ARTILERY_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_ARTILERY_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_ARTILERY_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			BaseLauncher = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			AreaRadius = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			DamagePerSec = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			Special = UtfDbTypeParserHelpers.ReadBooleanU1(rawData, ref offset),
			ExplosionType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset),
			FlashType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset)
		};
	}
}

internal sealed class BT_BARRAGE_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_BARRAGE_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("damage_per_sec", ScalarKind.F4),
		new ScalarSpec("range_radius", ScalarKind.F4),
		new ScalarSpec("flash_type", ScalarKind.Ascii32)
	]);

	public string TypeName => "BT_BARRAGE_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_BARRAGE_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_BARRAGE_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			DamagePerSec = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			RangeRadius = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			FlashType = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset)
		};
	}
}

internal sealed class BT_BEAM_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_BEAM_DATA", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseWeaponData("wpn_class"),
		new ScalarSpec("file_name", ScalarKind.Ascii32),
		new ScalarSpec("launch_sfx", ScalarKind.U4),
		new ScalarSpec("damage", ScalarKind.U4),
		new ScalarSpec("lifetime", ScalarKind.F4),
		new ScalarSpec("max_sweep_dist", ScalarKind.F4),
		new ScalarSpec("beam_width", ScalarKind.F4),
		UtfDbTypeLayouts.Color("blur_color"),
		new ScalarSpec("contact_blast", ScalarKind.Ascii32),
		new PaddingSpec("padding", 1)
	]);

	public string TypeName => "BT_BEAM_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_BEAM_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		var value = new BT_BEAM_DATA {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			WpnClass = UtfDbTypeParserHelpers.ParseBaseWeaponData(rawData, ref offset),
			FileName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset),
			LaunchSfx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			Damage = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			Lifetime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			MaxSweepDist = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			BeamWidth = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			BlurColor = UtfDbTypeParserHelpers.ParseBtColor(rawData, ref offset),
			ContactBlast = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset)
		};
		offset += 1;
		return value;
	}
}

internal sealed class BT_BLASTTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_BLAST", "field", [
		UtfDbTypeLayouts.BasicData(),
		new RepeatSpec(new ScalarSpec("effect_type", ScalarKind.Ascii32), 3),
		UtfDbTypeLayouts.FlashData("flash"),
		new ScalarSpec("total_time", ScalarKind.F4),
		new ScalarSpec("sfx", ScalarKind.U4),
		new ScalarSpec("lead_time", ScalarKind.F4),
		new ScalarSpec("draw_through_fog", ScalarKind.U1)
	]);

	public string TypeName => "BT_BLAST";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_BLAST ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		var effectTypes = BTBatch2TypeParserSupport.ReadAscii32Array(rawData, ref offset, 3);
		var value = new BT_BLAST {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			EffectType = effectTypes[0],
			Flash = UtfDbTypeParserHelpers.ParseFlashData(rawData, ref offset)
		};
		value.TotalTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset);
		value.Sfx = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		value.LeadTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset);
		value.DrawThroughFog = UtfDbTypeParserHelpers.ReadBooleanU1(rawData, ref offset);
		return value;
	}
}

internal sealed class BT_BUFF_ARTIFACTTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_BUFF_ARTIFACT", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseWeaponData("wpn_class"),
		new ScalarSpec("ship_name", ScalarKind.Ascii32),
		new ScalarSpec("buff_type", ScalarKind.U4),
		UtfDbTypeLayouts.ArtifactButtonInfo("button_info")
	]);

	public string TypeName => "BT_BUFF_ARTIFACT";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_BUFF_ARTIFACT ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_BUFF_ARTIFACT {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			WpnClass = UtfDbTypeParserHelpers.ParseBaseWeaponData(rawData, ref offset),
			ShipName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset),
			BuffType = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			ButtonInfo = UtfDbTypeParserHelpers.ParseBtArtifactButtonInfo(rawData, ref offset)
		};
	}
}

internal sealed class BT_BUFF_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_BUFF_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("range_radius", ScalarKind.F4),
		new ScalarSpec("buff_type", ScalarKind.U4),
		new ScalarSpec("target_type", ScalarKind.U4),
		new ScalarSpec("supply_use_type", ScalarKind.U4),
		new ScalarSpec("visual_name", ScalarKind.Ascii32)
	]);

	public string TypeName => "BT_BUFF_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_BUFF_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_BUFF_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			RangeRadius = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			BuffType = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			TargetType = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			SupplyUseType = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			VisualName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset)
		};
	}
}

internal sealed class BT_CLOAK_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_CLOAK_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("cloak_supply_use", ScalarKind.F4),
		new ScalarSpec("cloak_shutoff", ScalarKind.F4),
		UtfDbTypeLayouts.SingleTechnode("tech_node")
	]);

	public string TypeName => "BT_CLOAK_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_CLOAK_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_CLOAK_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			CloakSupplyUse = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			CloakShutoff = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			TechNode = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref offset)
		};
	}
}

internal sealed class BT_EFFECT_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_EFFECT_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("weapon_damage", ScalarKind.U4),
		new ScalarSpec("weapon_fire_delay", ScalarKind.F4),
		new ScalarSpec("weapon_velocity", ScalarKind.F4)
	]);

	public string TypeName => "BT_EFFECT_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_EFFECT_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_EFFECT_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			WeaponDamage = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			WeaponFireDelay = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			WeaponVelocity = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset)
		};
	}
}

internal sealed class BT_FANCY_LAUNCHTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_FANCY_LAUNCH", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("animation", ScalarKind.Ascii32),
		new ScalarSpec("hardpoint", ScalarKind.Ascii64),
		new ScalarSpec("anim_time", ScalarKind.F4),
		new ScalarSpec("effect_duration", ScalarKind.F4),
		new ScalarSpec("warmup_sound", ScalarKind.U4),
		UtfDbTypeLayouts.SingleTechnode("needed_tech"),
		new ScalarSpec("flags", ScalarKind.U4),
		new ScalarSpec("special_ability", ScalarKind.U4)
	]);

	public string TypeName => "BT_FANCY_LAUNCH";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_FANCY_LAUNCH ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		var flags = 0U;
		var value = new BT_FANCY_LAUNCH {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			Animation = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset),
			Hardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref offset),
			AnimTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			EffectDuration = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			WarmupSound = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			NeededTech = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref offset)
		};
		flags = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		value.Flags = flags;
		value.SpecialAbility = (UNIT_SPECIAL_ABILITY)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset);
		value.BSpecialWeapon = (flags & 1) != 0;
		value.BTargetRequired = (flags & 2) != 0;
		value.BWormHole = (flags & 4) != 0;
		return value;
	}
}

internal sealed class BT_FIREBALL_DATATypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_FIREBALL_DATA", "field", [
		UtfDbTypeLayouts.BasicData(),
		new ScalarSpec("fx_class", ScalarKind.U4),
		new ScalarSpec("anim_name", ScalarKind.Ascii32)
	]);

	public string TypeName => "BT_FIREBALL_DATA";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_FIREBALL_DATA ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_FIREBALL_DATA {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			FxClass = (EFFECTCLASS)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			AnimName = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset)
		};
	}
}

internal sealed class BT_JUMP_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_JUMP_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("jump_particle", ScalarKind.Ascii32)
	]);

	public string TypeName => "BT_JUMP_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_JUMP_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_JUMP_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			JumpParticle = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset)
		};
	}
}

internal sealed class BT_MOON_RESOURCE_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_MOON_RESOURCE_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("ore_regen_rate", ScalarKind.F4),
		new ScalarSpec("gas_regen_rate", ScalarKind.F4),
		new ScalarSpec("crew_regen_rate", ScalarKind.F4)
	]);

	public string TypeName => "BT_MOON_RESOURCE_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_MOON_RESOURCE_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_MOON_RESOURCE_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			OreRegenRate = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			GasRegenRate = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			CrewRegenRate = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset)
		};
	}
}

internal sealed class BT_MULTICLOAK_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_MULTICLOAK_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("cloak_supply_use", ScalarKind.F4),
		new ScalarSpec("cloak_shutoff", ScalarKind.F4),
		new ScalarSpec("target_supply_cost_per_hull", ScalarKind.F4),
		UtfDbTypeLayouts.SingleTechnode("tech_node")
	]);

	public string TypeName => "BT_MULTICLOAK_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_MULTICLOAK_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_MULTICLOAK_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			CloakSupplyUse = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			CloakShutoff = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			TargetSupplyCostPerHull = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			TechNode = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref offset)
		};
	}
}

internal sealed class BT_NOVA_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_NOVA_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("charge_time", ScalarKind.F4),
		new ScalarSpec("nova_explosion", ScalarKind.Ascii32)
	]);

	public string TypeName => "BT_NOVA_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_NOVA_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_NOVA_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			ChargeTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			NovaExplosion = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset)
		};
	}
}

internal sealed class BT_PING_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_PING_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		UtfDbTypeLayouts.SingleTechnode("tech_node")
	]);

	public string TypeName => "BT_PING_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_PING_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_PING_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			TechNode = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref offset)
		};
	}
}

internal sealed class BT_RECON_LAUNCHTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_RECON_LAUNCH", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("animation", ScalarKind.Ascii32),
		new ScalarSpec("hardpoint", ScalarKind.Ascii64),
		new ScalarSpec("anim_time", ScalarKind.F4),
		new ScalarSpec("effect_duration", ScalarKind.F4),
		new ScalarSpec("warmup_sound", ScalarKind.U4),
		UtfDbTypeLayouts.SingleTechnode("needed_tech"),
		new ScalarSpec("special_ability", ScalarKind.U4),
		new ScalarSpec("worm_weapon", ScalarKind.U1),
		new ScalarSpec("self_target", ScalarKind.U1),
		new PaddingSpec("padding", 2)
	]);

	public string TypeName => "BT_RECON_LAUNCH";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_RECON_LAUNCH ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		var value = new BT_RECON_LAUNCH {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			Animation = UtfDbTypeParserHelpers.ReadAscii32(rawData, ref offset),
			Hardpoint = UtfDbTypeParserHelpers.ReadAscii64(rawData, ref offset),
			AnimTime = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			EffectDuration = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			WarmupSound = UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			NeededTech = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref offset),
			SpecialAbility = (UNIT_SPECIAL_ABILITY)UtfDbTypeParserHelpers.ReadUInt32(rawData, ref offset),
			WormWeapon = UtfDbTypeParserHelpers.ReadBooleanU1(rawData, ref offset),
			SelfTarget = UtfDbTypeParserHelpers.ReadBooleanU1(rawData, ref offset)
		};
		offset += 2;
		return value;
	}
}

internal sealed class BT_REPAIR_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_REPAIR_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		new ScalarSpec("range_radius", ScalarKind.F4),
		new ScalarSpec("repair_rate", ScalarKind.F4),
		new ScalarSpec("repair_cost_per_point", ScalarKind.F4)
	]);

	public string TypeName => "BT_REPAIR_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_REPAIR_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_REPAIR_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			RangeRadius = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			RepairRate = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset),
			RepairCostPerPoint = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset)
		};
	}
}

internal sealed class BT_WORMHOLE_LAUNCHERTypeParser : IUtfDbTypeParser {
	private static readonly FixedLayoutTypeParser FieldParser = new("BT_WORMHOLE_LAUNCHER", "field", [
		UtfDbTypeLayouts.BasicData(),
		UtfDbTypeLayouts.BaseLauncher("type"),
		UtfDbTypeLayouts.SingleTechnode("tech_mode"),
		new ScalarSpec("damage_per_sec", ScalarKind.F4)
	]);

	public string TypeName => "BT_WORMHOLE_LAUNCHER";
	public UtfDbEntryDetails Parse(string databaseName, string fileName, ReadOnlySpan<byte> rawData, XDocument? xmlDocument) =>
		UtfDbTypeParserHelpers.ParseTyped(databaseName, fileName, rawData, xmlDocument, FieldParser, ParseTypedValue, BTBatch1TypeParserSupport.Annotate);

	private static BT_WORMHOLE_LAUNCHER ParseTypedValue(ReadOnlySpan<byte> rawData) {
		var offset = 0;
		var baseData = UtfDbTypeParserHelpers.ParseBasicData(rawData, ref offset);
		return new BT_WORMHOLE_LAUNCHER {
			ObjClass = baseData.ObjClass,
			BEditDropable = baseData.BEditDropable,
			Type = UtfDbTypeParserHelpers.ParseBaseLauncher(rawData, ref offset),
			TechMode = UtfDbTypeParserHelpers.ParseBtSingleTechnode(rawData, ref offset),
			DamagePerSec = UtfDbTypeParserHelpers.ReadSingle(rawData, ref offset)
		};
	}
}
