using System.Buffers.Binary;
using System.Numerics;
using ConquestFrontierWarsRay.Data.Models;
using ConquestFrontierWarsRay.Data.Models.BT;
using ConquestFrontierWarsRay.Data.Models.GT;
using ConquestFrontierWarsRay.Data.UtfDb.Models;

namespace ConquestFrontierWarsRay.Data.UtfDb.Types;

internal static class UtfDbTypeParserHelpers {
	public static UtfDbEntryDetails ParseTyped<T>(
		string databaseName,
		string fileName,
		ReadOnlySpan<byte> rawData,
		System.Xml.Linq.XDocument? xmlDocument,
		FixedLayoutTypeParser fieldParser,
		Func<ReadOnlySpan<byte>, T> parseTypedValue,
		Func<UtfDbField, UtfDbField>? annotateField = null) {
		var typedValue = parseTypedValue(rawData);
		var details = fieldParser.Parse(databaseName, fileName, rawData, xmlDocument);
		var fields = annotateField is null
			? details.Fields.ToArray()
			: details.Fields.Select(annotateField).ToArray();
		return details with { Fields = fields, TypedValue = typedValue };
	}

	public static string FormatEnum<TEnum>(TEnum value) where TEnum : struct, Enum {
		return $"{value} ({Convert.ToUInt64(value)})";
	}

	public static string FormatBoolean(bool value) {
		return value ? "True (1)" : "False (0)";
	}

	public static GT_COLOR ParseColor(ReadOnlySpan<byte> rawData, ref int offset) {
		return new GT_COLOR {
			Red = rawData[offset++],
			Green = rawData[offset++],
			Blue = rawData[offset++]
		};
	}

	public static BT_COLOR ParseBtColor(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_COLOR {
			Red = rawData[offset++],
			Green = rawData[offset++],
			Blue = rawData[offset++]
		};
	}

	public static BT_COLORA ParseColora(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_COLORA {
			Red = rawData[offset++],
			Green = rawData[offset++],
			Blue = rawData[offset++],
			Alpha = rawData[offset++]
		};
	}

	public static GT_VECTOR ParseVector(ReadOnlySpan<byte> rawData, ref int offset) {
		var vector = new GT_VECTOR {
			X = ReadSingle(rawData, ref offset),
			Y = ReadSingle(rawData, ref offset),
			Z = ReadSingle(rawData, ref offset)
		};
		return vector;
	}

	public static BASIC_DATA ParseBasicData(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = new BASIC_DATA {
			ObjClass = (OBJCLASS)ReadUInt32(rawData, ref offset),
			BEditDropable = ReadUInt32(rawData, ref offset) != 0
		};
		return value;
	}

	public static BASE_LAUNCHER ParseBaseLauncher(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BASE_LAUNCHER {
			Type = (LAUNCHCLASS)ReadUInt32(rawData, ref offset),
			WeaponType = ReadAscii32(rawData, ref offset),
			SupplyCost = ReadInt32(rawData, ref offset),
			RefirePeriod = ReadSingle(rawData, ref offset),
			LauncherSpecialId = ReadUInt32(rawData, ref offset)
		};
	}

	public static BASE_WEAPON_DATA ParseBaseWeaponData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BASE_WEAPON_DATA {
			WpnClass = (WPNCLASS)ReadUInt32(rawData, ref offset)
		};
	}

	public static BASE_FIELD_DATA ParseBaseFieldData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BASE_FIELD_DATA {
			FieldClass = (FIELDCLASS)ReadUInt32(rawData, ref offset),
			InfoHelpId = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_FLASH_DATA ParseFlashData(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = new BT_FLASH_DATA {
			LifeTime = ReadSingle(rawData, ref offset),
			Range = ReadSingle(rawData, ref offset),
			Color = ParseBtColor(rawData, ref offset)
		};
		offset += 1;
		return value;
	}

	public static BT_SINGLE_TECHNODE ParseBtSingleTechnode(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_SINGLE_TECHNODE {
			RaceId = ReadUInt32(rawData, ref offset),
			Tech = ReadUInt32(rawData, ref offset),
			Build = ReadUInt32(rawData, ref offset),
			Common = ReadUInt32(rawData, ref offset),
			CommonExtra = ReadUInt32(rawData, ref offset),
			Cq2Vars1 = ReadUInt32(rawData, ref offset),
			Cq2Vars2 = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_ARTIFACT_BUTTON_INFO ParseBtArtifactButtonInfo(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_ARTIFACT_BUTTON_INFO {
			BaseButton = ReadUInt32(rawData, ref offset),
			Tooltip = ReadUInt32(rawData, ref offset),
			HelpBox = ReadUInt32(rawData, ref offset),
			HintBox = ReadUInt32(rawData, ref offset)
		};
	}

	public static GENBASE_TYPE ParseGenBaseType(ReadOnlySpan<byte> rawData, ref int offset) {
		return (GENBASE_TYPE)ReadUInt32(rawData, ref offset);
	}

	public static uint ReadUInt32(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = BinaryPrimitives.ReadUInt32LittleEndian(rawData.Slice(offset, 4));
		offset += 4;
		return value;
	}

	public static ushort ReadUInt16(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = BinaryPrimitives.ReadUInt16LittleEndian(rawData.Slice(offset, 2));
		offset += 2;
		return value;
	}

	public static int ReadInt32(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = BinaryPrimitives.ReadInt32LittleEndian(rawData.Slice(offset, 4));
		offset += 4;
		return value;
	}

	public static float ReadSingle(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = BitConverter.ToSingle(rawData.Slice(offset, 4));
		offset += 4;
		return value;
	}

	public static sbyte ReadSByte(ReadOnlySpan<byte> rawData, ref int offset) {
		return unchecked((sbyte)rawData[offset++]);
	}

	public static bool ReadBooleanU1(ReadOnlySpan<byte> rawData, ref int offset) {
		return rawData[offset++] != 0;
	}

	public static string ReadAscii32(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = FixedLayoutTypeParser.DecodeAscii(rawData.Slice(offset, 32));
		offset += 32;
		return value;
	}

	public static string ReadAscii64(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = FixedLayoutTypeParser.DecodeAscii(rawData.Slice(offset, 64));
		offset += 64;
		return value;
	}

	public static string ReadUtf16String64(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = FixedLayoutTypeParser.DecodeUtf16(rawData.Slice(offset, 64));
		offset += 64;
		return value;
	}

	public static BT_VECTOR ParseBtVector(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_VECTOR {
			X = ReadSingle(rawData, ref offset),
			Y = ReadSingle(rawData, ref offset),
			Z = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_RESOURCE_COST ParseBtResourceCost(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_RESOURCE_COST {
			Gas = rawData[offset++],
			Metal = rawData[offset++],
			Crew = rawData[offset++],
			CommandPt = rawData[offset++]
		};
	}

	public static BT_ARMOR_DAMAGE ParseBtArmorDamage(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_ARMOR_DAMAGE {
			NoArmor = ReadSingle(rawData, ref offset),
			LightArmor = ReadSingle(rawData, ref offset),
			MediumArmor = ReadSingle(rawData, ref offset),
			HeavyArmor = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_ARMOR_DATA ParseBtArmorData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_ARMOR_DATA {
			MyArmor = ReadUInt32(rawData, ref offset),
			ArmorDamage = ParseBtArmorDamage(rawData, ref offset)
		};
	}

	public static BT_OBJ_CLASS_AND_RACE_AND_DISPLAY_NAME ParseBtObjClassAndRaceAndDisplayName(ReadOnlySpan<byte> rawData, ref int offset) {
		var raw = ReadUInt32(rawData, ref offset);
		return new BT_OBJ_CLASS_AND_RACE_AND_DISPLAY_NAME {
			Raw = raw
		};
	}

	public static BT_CAPS_BLOCK ParseBtCapsBlock(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_CAPS_BLOCK {
			Padding = rawData[offset++],
			FlagsLo = ReadUInt16(rawData, ref offset),
			FlagsHi = rawData[offset++]
		};
	}

	public static BT_MISSION_DATA ParseBtMissionData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_MISSION_DATA {
			ObjClassAndRaceAndDisplayName = ParseBtObjClassAndRaceAndDisplayName(rawData, ref offset),
			CapsRaw = ParseBtCapsBlock(rawData, ref offset),
			HullPointsMax = ReadUInt16(rawData, ref offset),
			SupplyPointsMax = ReadUInt16(rawData, ref offset),
			ScrapValue = ReadUInt16(rawData, ref offset),
			BuildTime = ReadUInt16(rawData, ref offset),
			ResourceCost = ParseBtResourceCost(rawData, ref offset),
			SensorRadius = ReadSingle(rawData, ref offset),
			CloakedSensorRadius = ReadSingle(rawData, ref offset),
			MaxVelocity = ReadSingle(rawData, ref offset),
			BaseWeaponAccuracy = ReadSingle(rawData, ref offset),
			BaseShieldLevel = ReadSingle(rawData, ref offset),
			ArmorData = ParseBtArmorData(rawData, ref offset),
			SilhouetteImage = ReadUInt32(rawData, ref offset),
			SpecialAbility = ReadUInt32(rawData, ref offset),
			SpecialAbility1 = ReadUInt32(rawData, ref offset),
			SpecialAbility2 = ReadUInt32(rawData, ref offset),
			SpeechPriority = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_FIELD_ATTRIBUTES ParseBtFieldAttributes(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_FIELD_ATTRIBUTES {
			SensorDamp = ReadSingle(rawData, ref offset),
			ToHitPenalty = ReadSingle(rawData, ref offset),
			Damage = ReadSingle(rawData, ref offset),
			MoveSpeedModifier = ReadSingle(rawData, ref offset),
			ManeuverModifier = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_AMBIENT_NEBULA_LIGHT ParseBtAmbientNebulaLight(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_AMBIENT_NEBULA_LIGHT {
			Color = ParseBtColor(rawData, ref offset),
			Padding = rawData[offset++],
			PulseFrequency = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_PROJECTILE_DATA_BASE ParseBtProjectileDataBase(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_PROJECTILE_DATA_BASE {
			Base = ParseBasicData(rawData, ref offset),
			WpnClass = ParseBaseWeaponData(rawData, ref offset),
			FileName = ReadAscii32(rawData, ref offset),
			LaunchSfx = ReadUInt32(rawData, ref offset),
			Damage = ReadUInt32(rawData, ref offset),
			MaxVelocity = ReadSingle(rawData, ref offset),
			BlastType = ReadAscii32(rawData, ref offset),
			EngineTrailType = ReadAscii32(rawData, ref offset)
		};
	}

	public static BT_DYNAMICS_DATA ParseBtDynamicsData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_DYNAMICS_DATA {
			LinearAcceleration = ReadSingle(rawData, ref offset),
			AngAcceleration = ReadSingle(rawData, ref offset),
			MaxLinearVelocity = ReadSingle(rawData, ref offset),
			MaxAngVelocity = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_DYNAMICS_DATA_JR ParseBtDynamicsDataJr(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_DYNAMICS_DATA_JR {
			MaxLinearVelocity = ReadSingle(rawData, ref offset),
			LinearAcceleration = ReadSingle(rawData, ref offset),
			MaxAngVelocity = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_ROCKING_DATA ParseBtRockingData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_ROCKING_DATA {
			Base = ParseBtDynamicsData(rawData, ref offset),
			RockLinearMax = ReadSingle(rawData, ref offset),
			RockAngMax = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_ENGINE_GLOW_DATA ParseBtEngineGlowData(ReadOnlySpan<byte> rawData, ref int offset) {
		var size = new int[6];
		for (var i = 0; i < size.Length; i++) size[i] = ReadInt32(rawData, ref offset);
		return new BT_ENGINE_GLOW_DATA {
			Size = size,
			Color = ParseBtColor(rawData, ref offset),
			EngineTextureName = ReadAscii32(rawData, ref offset),
			Padding = rawData[offset++]
		};
	}

	public static BT_BLINKER_DATA ParseBtBlinkerData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_BLINKER_DATA {
			LightScript = ReadAscii32(rawData, ref offset),
			TextureName = ReadAscii32(rawData, ref offset)
		};
	}

	public static BT_SHIELD_DATA ParseBtShieldData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_SHIELD_DATA {
			MeshName = ReadAscii32(rawData, ref offset),
			AnimName = ReadAscii32(rawData, ref offset),
			FizzAnimName = ReadAscii32(rawData, ref offset),
			Sfx = ReadUInt32(rawData, ref offset),
			FizzOut = ReadUInt32(rawData, ref offset),
			FizzIn = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_CLOAK_DATA ParseBtCloakData(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = new BT_CLOAK_DATA {
			CloakTex = ReadAscii32(rawData, ref offset),
			AutoCloak = ReadBooleanU1(rawData, ref offset),
			CloakEffectType = ReadAscii32(rawData, ref offset)
		};
		offset += 3;
		return value;
	}

	public static BT_BILLBOARD_DATA ParseBtBillboardData(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = new BT_BILLBOARD_DATA {
			BillboardTexName = ReadAscii32(rawData, ref offset),
			BillboardThreshhold = ReadUInt32(rawData, ref offset),
			BTex2 = ReadBooleanU1(rawData, ref offset)
		};
		offset += 3;
		return value;
	}

	public static BT_BILLBOARD_MESH ParseBtBillboardMesh(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_BILLBOARD_MESH {
			MeshName = ReadAscii32(rawData, ref offset),
			TexName = ReadAscii32(rawData, ref offset),
			SizeMin = ReadInt32(rawData, ref offset),
			SizeMax = ReadInt32(rawData, ref offset),
			Offset = ParseBtVector(rawData, ref offset),
			BlendMode = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_FORMATION_FILTER ParseBtFormationFilter(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_FORMATION_FILTER { Flags = ReadUInt32(rawData, ref offset) };
	}

	public static BT_BASE_SPACESHIP_DATA ParseBtBaseSpaceshipData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_BASE_SPACESHIP_DATA {
			Base = ParseBasicData(rawData, ref offset),
			Type = (SPACESHIPCLASS)ReadUInt32(rawData, ref offset),
			FileName = ReadAscii32(rawData, ref offset),
			MissionData = ParseBtMissionData(rawData, ref offset),
			DynamicsData = ParseBtDynamicsData(rawData, ref offset),
			RockingData = ParseBtRockingData(rawData, ref offset),
			ExplosionType = ReadAscii32(rawData, ref offset),
			TrailType = ReadAscii32(rawData, ref offset),
			AmbientAnimation = ReadAscii32(rawData, ref offset),
			AmbientEffect = ReadAscii32(rawData, ref offset),
			EngineGlow = ParseBtEngineGlowData(rawData, ref offset),
			Blinkers = ParseBtBlinkerData(rawData, ref offset),
			Shield = ParseBtShieldData(rawData, ref offset),
			DamageBlast = ReadAscii32(rawData, ref offset),
			Cloak = ParseBtCloakData(rawData, ref offset),
			Billboard = ParseBtBillboardData(rawData, ref offset),
			TechActive = ParseBtSingleTechnode(rawData, ref offset),
			FormationFilter = ParseBtFormationFilter(rawData, ref offset),
			LargeShip = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_DOCK_TIMING ParseBtDockTiming(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_DOCK_TIMING {
			UnloadRate = ReadSingle(rawData, ref offset),
			OffDist = ReadSingle(rawData, ref offset),
			DockingSound = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_NUGGET_TIMING ParseBtNuggetTiming(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_NUGGET_TIMING {
			NuggetTime = ReadSingle(rawData, ref offset),
			OffDist = ReadSingle(rawData, ref offset),
			MinePerSecond = ReadSingle(rawData, ref offset),
			HardpointName = ReadAscii32(rawData, ref offset),
			NuggetSound = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_COMPLEX_COLOR ParseBtComplexColor(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_COMPLEX_COLOR {
			RedHi = rawData[offset++],
			GreenHi = rawData[offset++],
			BlueHi = rawData[offset++],
			RedLo = rawData[offset++],
			GreenLo = rawData[offset++],
			BlueLo = rawData[offset++],
			AlphaIn = rawData[offset++],
			AlphaOut = rawData[offset++]
		};
	}

	public static BT_PLANET_HALO ParseBtPlanetHalo(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_PLANET_HALO {
			Color = ParseBtColor(rawData, ref offset),
			Padding = rawData[offset++],
			SizeInner = ReadSingle(rawData, ref offset),
			SizeOuter = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_PLAYERBOMB_TYPE ParseBtPlayerbombType(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_PLAYERBOMB_TYPE { ArchetypeName = ReadAscii32(rawData, ref offset) };
	}

	public static BT_PLAYER_RACE ParseBtPlayerRace(ReadOnlySpan<byte> rawData, ref int offset) {
		var minBombType = new BT_PLAYERBOMB_TYPE[4];
		for (var i = 0; i < minBombType.Length; i++) minBombType[i] = ParseBtPlayerbombType(rawData, ref offset);
		var bombType = new BT_PLAYERBOMB_TYPE[4];
		for (var i = 0; i < bombType.Length; i++) bombType[i] = ParseBtPlayerbombType(rawData, ref offset);
		var largeBombType = new BT_PLAYERBOMB_TYPE[8];
		for (var i = 0; i < largeBombType.Length; i++) largeBombType[i] = ParseBtPlayerbombType(rawData, ref offset);
		return new BT_PLAYER_RACE {
			MinBombType = minBombType,
			BombType = bombType,
			LargeBombType = largeBombType
		};
	}

	public static RECT ParseRect(ReadOnlySpan<byte> rawData, ref int offset) {
		return new RECT {
			Left = ReadInt32(rawData, ref offset),
			Top = ReadInt32(rawData, ref offset),
			Right = ReadInt32(rawData, ref offset),
			Bottom = ReadInt32(rawData, ref offset)
		};
	}

	public static BUTTON_DATA ParseButtonData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BUTTON_DATA {
			ButtonType = ReadAscii32(rawData, ref offset),
			ButtonText = ReadUInt32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			ButtonArea = ParseRect(rawData, ref offset)
		};
	}

	public static STATIC_DATA ParseStaticData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new STATIC_DATA {
			StaticType = ReadAscii32(rawData, ref offset),
			StaticText = ReadUInt32(rawData, ref offset),
			StaticTooltip = ReadUInt32(rawData, ref offset),
			StaticHintbox = ReadUInt32(rawData, ref offset),
			Alignment = ReadUInt32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			Width = ReadInt32(rawData, ref offset),
			Height = ReadInt32(rawData, ref offset)
		};
	}

	public static ANIMATE_DATA ParseAnimateData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new ANIMATE_DATA {
			AnimateType = ReadAscii32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			Timer = ReadUInt32(rawData, ref offset),
			FuzzEffect = ReadUInt32(rawData, ref offset) != 0
		};
	}

	public static EDIT_DATA ParseEditData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new EDIT_DATA {
			EditType = ReadAscii32(rawData, ref offset),
			EditText = ReadUInt32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset)
		};
	}

	public static LISTBOX_DATA ParseListboxData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new LISTBOX_DATA {
			ListboxType = ReadAscii32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			TextArea = ParseRect(rawData, ref offset),
			LeadingHeight = ReadUInt32(rawData, ref offset),
			Flags = ReadUInt32(rawData, ref offset)
		};
	}

	public static DIPLOMACYBUTTON_DATA ParseDiplomacyButtonData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new DIPLOMACYBUTTON_DATA {
			ButtonType = ReadAscii32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset)
		};
	}

	public static SLIDER_DATA ParseSliderData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new SLIDER_DATA {
			SliderType = ReadAscii32(rawData, ref offset),
			ScreenRect = ParseRect(rawData, ref offset),
			Origin = new Vector2(
				ReadInt32(rawData, ref offset),
				ReadInt32(rawData, ref offset))
		};
	}

	public static DROPDOWN_DATA ParseDropdownData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new DROPDOWN_DATA {
			DropdownType = ReadAscii32(rawData, ref offset),
			ScreenRect = ParseRect(rawData, ref offset),
			ButtonData = ParseButtonData(rawData, ref offset),
			ListboxData = ParseListboxData(rawData, ref offset)
		};
	}

	public static COMBOBOX_DATA ParseComboboxData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new COMBOBOX_DATA {
			ComboboxType = ReadAscii32(rawData, ref offset),
			ScreenRect = ParseRect(rawData, ref offset),
			EditData = ParseEditData(rawData, ref offset),
			ButtonData = ParseButtonData(rawData, ref offset),
			ListboxData = ParseListboxData(rawData, ref offset)
		};
	}

	public static TABCONTROL_DATA ParseTabcontrolData(ReadOnlySpan<byte> rawData, ref int offset) {
		var tabControlType = ReadAscii32(rawData, ref offset);
		var hotButtonType = ReadAscii32(rawData, ref offset);
		var baseImage = ReadInt32(rawData, ref offset);
		var numTabs = ReadInt32(rawData, ref offset);
		var textIds = new uint[6];
		for (var index = 0; index < textIds.Length; index++) {
			textIds[index] = ReadUInt32(rawData, ref offset);
		}

		return new TABCONTROL_DATA {
			TabControlType = tabControlType,
			HotButtonType = hotButtonType,
			BaseImage = baseImage,
			NumTabs = numTabs,
			TextIds = textIds,
			UpperTabs = ReadUInt32(rawData, ref offset),
			XPos = ReadInt32(rawData, ref offset),
			YPos = ReadInt32(rawData, ref offset)
		};
	}

	public static HOTSTATIC_DATA ParseHotstaticData(ReadOnlySpan<byte> rawData, ref int offset) {
		var baseImage = ReadUInt32(rawData, ref offset);
		var numTechLevels = ReadUInt32(rawData, ref offset);
		var xOrigin = ReadInt32(rawData, ref offset);
		var yOrigin = ReadInt32(rawData, ref offset);
		var width = ReadInt32(rawData, ref offset);
		var height = ReadInt32(rawData, ref offset);
		var barStartX = ReadInt32(rawData, ref offset);
		var barSpacing = ReadUInt32(rawData, ref offset);
		var text = ReadUInt32(rawData, ref offset);
		var textColor = ParseColor(rawData, ref offset);
		offset += 1;

		return new HOTSTATIC_DATA {
			BaseImage = baseImage,
			NumTechLevels = numTechLevels,
			XOrigin = xOrigin,
			YOrigin = yOrigin,
			Width = width,
			Height = height,
			BarStartX = barStartX,
			BarSpacing = barSpacing,
			Text = text,
			TextColor = textColor
		};
	}

	public static HOTBUTTON_DATA ParseHotbuttonData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new HOTBUTTON_DATA {
			BaseImage = ReadUInt32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			ButtonText = ReadUInt32(rawData, ref offset),
			ButtonInfo = ReadUInt32(rawData, ref offset),
			ButtonHint = ReadUInt32(rawData, ref offset),
			Hotkey = ReadUInt32(rawData, ref offset),
			Disabled = ReadUInt32(rawData, ref offset)
		};
	}

	public static ICON_DATA ParseIconData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new ICON_DATA {
			BaseImage = ReadUInt32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			Tooltip = ReadUInt32(rawData, ref offset)
		};
	}

	public static QUEUECONTROL_DATA ParseQueuecontrolData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new QUEUECONTROL_DATA {
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			Width = ReadInt32(rawData, ref offset),
			Height = ReadInt32(rawData, ref offset)
		};
	}

	public static MULTIHOTBUTTON_DATA ParseMultihotbuttonData(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = new MULTIHOTBUTTON_DATA {
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			Hotkey = ReadUInt32(rawData, ref offset),
			SingleShape = rawData[offset++],
			Disabled = rawData[offset++]
		};
		offset += 2;
		return value;
	}

	public static SINGLE_TECHNODE ParseSingleTechnode(ReadOnlySpan<byte> rawData, ref int offset) {
		return new SINGLE_TECHNODE {
			RaceId = ReadUInt32(rawData, ref offset),
			Tech = ReadUInt32(rawData, ref offset),
			Build = ReadUInt32(rawData, ref offset),
			Common = ReadUInt32(rawData, ref offset),
			CommonExtra = ReadUInt32(rawData, ref offset),
			Cq2Vars1 = ReadUInt32(rawData, ref offset),
			Cq2Vars2 = ReadUInt32(rawData, ref offset)
		};
	}

	public static BUILDBUTTON_DATA ParseBuildbuttonData(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = new BUILDBUTTON_DATA {
			BaseImage = ReadUInt32(rawData, ref offset),
			NoMoneyImage = ReadUInt32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			RtArchetype = ReadAscii32(rawData, ref offset),
			TechDependency = ParseSingleTechnode(rawData, ref offset),
			TechGreyed = ParseSingleTechnode(rawData, ref offset),
			GreyedTooltip = ReadUInt32(rawData, ref offset),
			BuildInfo = ReadUInt32(rawData, ref offset),
			Hotkey = ReadUInt32(rawData, ref offset),
			Disabled = rawData[offset++]
		};
		offset += 3;
		return value;
	}

	public static RESEARCHBUTTON_DATA ParseResearchbuttonData(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = new RESEARCHBUTTON_DATA {
			BaseImage = ReadUInt32(rawData, ref offset),
			NoMoneyImage = ReadUInt32(rawData, ref offset),
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset),
			RtArchetype = ReadAscii32(rawData, ref offset),
			Tooltip = ReadUInt32(rawData, ref offset),
			ResearchInfo = ReadUInt32(rawData, ref offset),
			Hotkey = ReadUInt32(rawData, ref offset),
			Disabled = rawData[offset++]
		};
		offset += 3;
		return value;
	}

	public static SHIPSILBUTTON_DATA ParseShipsilbuttonData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new SHIPSILBUTTON_DATA {
			XOrigin = ReadInt32(rawData, ref offset),
			YOrigin = ReadInt32(rawData, ref offset)
		};
	}

	public static ARTIFACT_BUTTON_INFO ParseArtifactButtonInfo(ReadOnlySpan<byte> rawData, ref int offset) {
		return new ARTIFACT_BUTTON_INFO {
			BaseButton = ReadUInt32(rawData, ref offset),
			Tooltip = ReadUInt32(rawData, ref offset),
			HelpBox = ReadUInt32(rawData, ref offset),
			HintBox = ReadUInt32(rawData, ref offset)
		};
	}

	public static GT_SYSTEM_KIT_LIGHT_INFO ParseSystemKitLightInfo(ReadOnlySpan<byte> rawData, ref int offset) {
		var color = ParseColor(rawData, ref offset);
		offset += 1;
		var range = ReadInt32(rawData, ref offset);
		var position = ParseVector(rawData, ref offset);
		var direction = ParseVector(rawData, ref offset);
		var cutoff = ReadSingle(rawData, ref offset);
		var infinite = ReadBooleanU1(rawData, ref offset);
		var name = ReadAscii32(rawData, ref offset);
		var ambient = ReadBooleanU1(rawData, ref offset);
		offset += 2;

		return new GT_SYSTEM_KIT_LIGHT_INFO {
			Color = color,
			Range = range,
			Position = position,
			Direction = direction,
			Cutoff = cutoff,
			Infinite = infinite,
			Name = name,
			Ambient = ambient
		};
	}

	public static BT_EXTENSION_DATA ParseBtExtensionData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_EXTENSION_DATA { ExtensionName = ReadAscii32(rawData, ref offset) };
	}

	public static BT_RACE_DAMAGE ParseBtRaceDamage(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_RACE_DAMAGE {
			Terran = ReadSingle(rawData, ref offset),
			Mantis = ReadSingle(rawData, ref offset),
			Celareon = ReadSingle(rawData, ref offset),
			Vyrium = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_BONUS_VALUES ParseBtBonusValues(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_BONUS_VALUES {
			Damage = ReadSingle(rawData, ref offset),
			SupplyUsage = ReadSingle(rawData, ref offset),
			RangeModifier = ReadSingle(rawData, ref offset),
			Speed = ReadSingle(rawData, ref offset),
			Sensors = ReadSingle(rawData, ref offset),
			Defence = ReadSingle(rawData, ref offset),
			PlatformDamage = ReadSingle(rawData, ref offset),
			HatedRaceDamage = ParseBtRaceDamage(rawData, ref offset),
			HatedArmorDamage = ParseBtArmorDamage(rawData, ref offset)
		};
	}

	public static BT_ADMIRAL_BONUSES ParseBtAdmiralBonuses(ReadOnlySpan<byte> rawData, ref int offset) {
		var baseBonuses = ParseBtBonusValues(rawData, ref offset);
		var favoredShipBonus = ParseBtBonusValues(rawData, ref offset);
		var favoredArmor = ParseBtBonusValues(rawData, ref offset);
		var bonusShips = new uint[5];
		for (var i = 0; i < bonusShips.Length; i++) bonusShips[i] = ReadUInt32(rawData, ref offset);
		return new BT_ADMIRAL_BONUSES {
			BaseBonuses = baseBonuses,
			FavoredShipBonus = favoredShipBonus,
			FavoredArmor = favoredArmor,
			BonusShips = bonusShips,
			Flags = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_SHIP_FILTERS ParseBtShipFilters(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_SHIP_FILTERS {
			PositiveFilter = ParseBtFormationFilter(rawData, ref offset),
			NegativeFilter = ParseBtFormationFilter(rawData, ref offset),
			Max = ReadUInt32(rawData, ref offset),
			Min = ReadUInt32(rawData, ref offset),
			OverflowOnly = ReadUInt32(rawData, ref offset) != 0
		};
	}

	public static BT_ADVANCED_PLACEMENT ParseBtAdvancedPlacement(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_ADVANCED_PLACEMENT {
			PlacementType = (PLACEMENT_TYPE)ReadUInt32(rawData, ref offset),
			PlacementDirX = ReadUInt32(rawData, ref offset),
			PlacementDirY = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_FLEET_GROUP_DEF ParseBtFleetGroupDef(ReadOnlySpan<byte> rawData, ref int offset) {
		var filters = new BT_SHIP_FILTERS[6];
		for (var i = 0; i < filters.Length; i++) filters[i] = ParseBtShipFilters(rawData, ref offset);
		return new BT_FLEET_GROUP_DEF {
			Filters = filters,
			Flags = ReadUInt32(rawData, ref offset),
			ParentGroup = ReadUInt32(rawData, ref offset),
			ParentGroupNumber = ReadUInt32(rawData, ref offset),
			Priority = ReadUInt32(rawData, ref offset),
			CreationNumber = ReadUInt32(rawData, ref offset),
			RelativeTo = (RELATIVE_TYPE)ReadUInt32(rawData, ref offset),
			RelativeGroupId = ReadUInt32(rawData, ref offset),
			Relation = (RELATION)ReadUInt32(rawData, ref offset),
			RelDirX = ReadUInt32(rawData, ref offset),
			RelDirY = ReadUInt32(rawData, ref offset),
			AdvancedPlacement = ParseBtAdvancedPlacement(rawData, ref offset),
			AiType = (AI_TYPE)ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_DRONE_RELEASE ParseBtDroneRelease(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_DRONE_RELEASE {
			Hardpoint = ReadAscii64(rawData, ref offset),
			BuilderType = ReadAscii32(rawData, ref offset),
			NumDrones = rawData[offset++]
		};
	}

	public static BT_BASE_RESEARCH_DATA ParseBtBaseResearchData(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_BASE_RESEARCH_DATA {
			Base = ParseBasicData(rawData, ref offset),
			Type = (RESEARCH_TYPE)ReadUInt32(rawData, ref offset),
			Cost = ParseBtResourceCost(rawData, ref offset),
			Time = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_BASE_PLATFORM_DATA ParseBtBasePlatformData(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = new BT_BASE_PLATFORM_DATA {
			Base = ParseBasicData(rawData, ref offset),
			Type = (PLATFORMCLASS)ReadUInt32(rawData, ref offset),
			FileName = ReadAscii32(rawData, ref offset),
			MissionData = ParseBtMissionData(rawData, ref offset),
			Extension = [
				ParseBtExtensionData(rawData, ref offset),
				ParseBtExtensionData(rawData, ref offset),
				ParseBtExtensionData(rawData, ref offset),
				ParseBtExtensionData(rawData, ref offset)
			],
			ExtensionBits = rawData[offset++],
			ExtensionLevel = ReadSByte(rawData, ref offset),
			ExplosionType = ReadAscii32(rawData, ref offset),
			ShieldHitType = ReadAscii32(rawData, ref offset),
			AmbientAnimation = ReadAscii32(rawData, ref offset),
			AmbientEffect = ReadAscii32(rawData, ref offset)
		};
		offset += 2;
		value.Mass = ReadSingle(rawData, ref offset);
		value.TechActive = ParseBtSingleTechnode(rawData, ref offset);
		value.Shield = ParseBtShieldData(rawData, ref offset);
		value.SlotsNeeded = ReadUInt32(rawData, ref offset);
		value.Blinkers = ParseBtBlinkerData(rawData, ref offset);
		value.CommandPoints = rawData[offset++];
		offset += 3;
		value.MetalStorage = ReadUInt32(rawData, ref offset);
		value.GasStorage = ReadUInt32(rawData, ref offset);
		value.CrewStorage = ReadUInt32(rawData, ref offset);
		value.Size = rawData[offset++];
		offset += 2;
		value.MoonPlatform = ReadBooleanU1(rawData, ref offset);
		return value;
	}

	public static BT_TURRET_WARM_UP_BLAST ParseBtTurretWarmUpBlast(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_TURRET_WARM_UP_BLAST {
			BlastType = ReadAscii32(rawData, ref offset),
			TriggerTime = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_SPECIAL_DAMAGE ParseBtSpecialDamage(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_SPECIAL_DAMAGE {
			SupplyDamage = ReadUInt32(rawData, ref offset),
			ShieldFraction = ReadSingle(rawData, ref offset),
			MoveFraction = ReadSingle(rawData, ref offset),
			SensorFraction = ReadSingle(rawData, ref offset)
		};
	}

	public static BT_MINER_DRONE_RELEASE ParseBtMinerDroneRelease(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_MINER_DRONE_RELEASE {
			Hardpoint = ReadAscii64(rawData, ref offset),
			BuilderType = ReadAscii32(rawData, ref offset)
		};
	}

	public static BT_VERTICAL_SALVO ParseBtVerticalSalvo(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_VERTICAL_SALVO {
			TechNeed = ParseBtSingleTechnode(rawData, ref offset),
			Salvo = ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_MAP_TERRAIN_INFO ParseBtMapTerrainInfo(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_MAP_TERRAIN_INFO {
			TerrainArchType = ReadAscii32(rawData, ref offset),
			Probability = ReadSingle(rawData, ref offset),
			MinToPlace = ReadUInt32(rawData, ref offset),
			MaxToPlace = ReadUInt32(rawData, ref offset),
			NumberFunc = (MAP_DMAP_FUNC)ReadUInt32(rawData, ref offset),
			Size = ReadUInt32(rawData, ref offset),
			RequiredToPlace = ReadUInt32(rawData, ref offset),
			Overlap = (MAP_OVERLAP)ReadUInt32(rawData, ref offset),
			Placement = (MAP_PLACEMENT)ReadUInt32(rawData, ref offset)
		};
	}

	public static BT_MAP_MACRO ParseBtMapMacro(ReadOnlySpan<byte> rawData, ref int offset) {
		return new BT_MAP_MACRO {
			Operation = (MAP_MACRO_OPERATION)ReadUInt32(rawData, ref offset),
			Range = ReadUInt32(rawData, ref offset),
			Active = ReadUInt32(rawData, ref offset) != 0,
			Info = ParseBtMapTerrainInfo(rawData, ref offset)
		};
	}

	public static BT_MAP_TERRAIN_THEME ParseBtMapTerrainTheme(ReadOnlySpan<byte> rawData, ref int offset) {
		var value = new BT_MAP_TERRAIN_THEME {
			SystemKit = [
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset),
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset)
			],
			MetalPlanets = [
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset),
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset)
			],
			GasPlanets = [
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset),
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset)
			],
			HabitablePlanets = [
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset),
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset)
			],
			OtherPlanets = [
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset),
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset)
			],
			MoonTypes = [
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset),
				ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset), ReadAscii32(rawData, ref offset)
			],
			SizeOk = (MAP_SECTOR_SIZE)ReadUInt32(rawData, ref offset),
			MinSize = ReadUInt32(rawData, ref offset),
			MaxSize = ReadUInt32(rawData, ref offset),
			SizeFunc = (MAP_DMAP_FUNC)ReadUInt32(rawData, ref offset),
			NumHabitablePlanets = [ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset)],
			NumMetalPlanets = [ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset)],
			NumGasPlanets = [ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset)],
			NumOtherPlanets = [ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset)],
			MinMoonsPerPlanet = ReadUInt32(rawData, ref offset),
			MaxMoonsPerPlanet = ReadUInt32(rawData, ref offset),
			MoonNumberFunc = (MAP_DMAP_FUNC)ReadUInt32(rawData, ref offset),
			NumNuggetPatchesMetal = [ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset)],
			NumNuggetPatchesGas = [ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset), ReadUInt32(rawData, ref offset)]
		};
		var terrain = new BT_MAP_TERRAIN_INFO[20];
		for (var i = 0; i < terrain.Length; i++) terrain[i] = ParseBtMapTerrainInfo(rawData, ref offset);
		var nuggetMetalTypes = new BT_MAP_TERRAIN_INFO[6];
		for (var i = 0; i < nuggetMetalTypes.Length; i++) nuggetMetalTypes[i] = ParseBtMapTerrainInfo(rawData, ref offset);
		var nuggetGasTypes = new BT_MAP_TERRAIN_INFO[6];
		for (var i = 0; i < nuggetGasTypes.Length; i++) nuggetGasTypes[i] = ParseBtMapTerrainInfo(rawData, ref offset);
		value.Terrain = terrain;
		value.NuggetMetalTypes = nuggetMetalTypes;
		value.NuggetGasTypes = nuggetGasTypes;
		value.FlagsRaw = ReadUInt32(rawData, ref offset);
		value.OkForPlayerStart = (value.FlagsRaw & 0x01) != 0;
		value.OkForRemoteSystem = (value.FlagsRaw & 0x02) != 0;
		value.Density = [ReadSingle(rawData, ref offset), ReadSingle(rawData, ref offset), ReadSingle(rawData, ref offset)];
		var macros = new BT_MAP_MACRO[15];
		for (var i = 0; i < macros.Length; i++) macros[i] = ParseBtMapMacro(rawData, ref offset);
		value.Macros = macros;
		return value;
	}
}
