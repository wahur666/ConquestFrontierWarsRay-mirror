namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BASE_LAUNCHER {
	public LAUNCHCLASS Type { get; set; }
	public string WeaponType { get; set; } = string.Empty;
	public int SupplyCost { get; set; }
	public float RefirePeriod { get; set; }
	public uint LauncherSpecialId { get; set; }
}

public sealed class BASE_WEAPON_DATA {
	public WPNCLASS WpnClass { get; set; }
}

public sealed class BASE_FIELD_DATA {
	public FIELDCLASS FieldClass { get; set; }
	public uint InfoHelpId { get; set; }
}

public sealed class BT_COLOR {
	public byte Red { get; set; }
	public byte Green { get; set; }
	public byte Blue { get; set; }
}

public sealed class BT_COLORA {
	public byte Red { get; set; }
	public byte Green { get; set; }
	public byte Blue { get; set; }
	public byte Alpha { get; set; }
}

public sealed class BT_FLASH_DATA {
	public float LifeTime { get; set; }
	public float Range { get; set; }
	public BT_COLOR Color { get; set; } = new();
}

public sealed class BT_SINGLE_TECHNODE {
	public uint RaceId { get; set; }
	public uint Tech { get; set; }
	public uint Build { get; set; }
	public uint Common { get; set; }
	public uint CommonExtra { get; set; }
	public uint Cq2Vars1 { get; set; }
	public uint Cq2Vars2 { get; set; }
}

public sealed class BT_ARTIFACT_BUTTON_INFO {
	public uint BaseButton { get; set; }
	public uint Tooltip { get; set; }
	public uint HelpBox { get; set; }
	public uint HintBox { get; set; }
}

public sealed class BT_VECTOR {
	public float X { get; set; }
	public float Y { get; set; }
	public float Z { get; set; }
}

public sealed class BT_RESOURCE_COST {
	public byte Gas { get; set; }
	public byte Metal { get; set; }
	public byte Crew { get; set; }
	public byte CommandPt { get; set; }
}

public sealed class BT_ARMOR_DAMAGE {
	public float NoArmor { get; set; }
	public float LightArmor { get; set; }
	public float MediumArmor { get; set; }
	public float HeavyArmor { get; set; }
}

public sealed class BT_ARMOR_DATA {
	public uint MyArmor { get; set; }
	public BT_ARMOR_DAMAGE ArmorDamage { get; set; } = new();
}

public sealed class BT_OBJ_CLASS_AND_RACE_AND_DISPLAY_NAME {
	private uint _mObjClass;
	private uint _race;
	private uint _displayName;

	public uint Raw {
		get => (_mObjClass & 0x1FF) | ((_race & 0xF) << 9) | ((_displayName & 0x1FFFF) << 13);
		set {
			_mObjClass = value & 0x1FF;
			_race = (value >> 9) & 0xF;
			_displayName = (value >> 13) & 0x1FFFF;
		}
	}

	public uint MObjClass {
		get => _mObjClass;
		set => _mObjClass = value & 0x1FF;
	}

	public uint Race {
		get => _race;
		set => _race = value & 0xF;
	}

	public uint DisplayName {
		get => _displayName;
		set => _displayName = value & 0x1FFFF;
	}
}

public sealed class BT_CAPS_BLOCK {
	private ushort _flagsLo;
	private byte _flagsHi;

	public byte Padding { get; set; }
	public ushort FlagsLo {
		get => PackFlagsLo();
		set {
			_flagsLo = value;
			ApplyPackedFlags();
		}
	}

	public byte FlagsHi {
		get => PackFlagsHi();
		set {
			_flagsHi = value;
			ApplyPackedFlags();
		}
	}

	public bool MoveOk { get; set; }
	public bool AttackOk { get; set; }
	public bool SpecialAttackOk { get; set; }
	public bool SpecialEOAOk { get; set; }
	public bool SpecialAbilityOk { get; set; }
	public bool SpecialAttackWormOk { get; set; }
	public bool DefendOk { get; set; }
	public bool SupplyOk { get; set; }
	public bool HarvestOk { get; set; }
	public bool BuildOk { get; set; }
	public bool RepairOk { get; set; }
	public bool JumpOk { get; set; }
	public bool AdmiralOk { get; set; }
	public bool CaptureOk { get; set; }
	public bool SalvageOk { get; set; }
	public bool ProbeOk { get; set; }
	public bool MimicOk { get; set; }
	public bool RecoverOk { get; set; }
	public bool CreateWormholeOk { get; set; }
	public bool SynthesisOk { get; set; }
	public bool CloakOk { get; set; }
	public bool SpecialAttackShipOk { get; set; }
	public bool TargetPositionOk { get; set; }
	public bool SpecialTargetPlanetOk { get; set; }

	private ushort PackFlagsLo() {
		ushort value = 0;
		SetBit(ref value, 0, MoveOk);
		SetBit(ref value, 1, AttackOk);
		SetBit(ref value, 2, SpecialAttackOk);
		SetBit(ref value, 3, SpecialEOAOk);
		SetBit(ref value, 4, SpecialAbilityOk);
		SetBit(ref value, 5, SpecialAttackWormOk);
		SetBit(ref value, 6, DefendOk);
		SetBit(ref value, 7, SupplyOk);
		SetBit(ref value, 8, HarvestOk);
		SetBit(ref value, 9, BuildOk);
		SetBit(ref value, 10, RepairOk);
		SetBit(ref value, 11, JumpOk);
		SetBit(ref value, 12, AdmiralOk);
		SetBit(ref value, 13, CaptureOk);
		SetBit(ref value, 14, SalvageOk);
		SetBit(ref value, 15, ProbeOk);
		return value;
	}

	private byte PackFlagsHi() {
		byte value = 0;
		SetBit(ref value, 0, MimicOk);
		SetBit(ref value, 1, RecoverOk);
		SetBit(ref value, 2, CreateWormholeOk);
		SetBit(ref value, 3, SynthesisOk);
		SetBit(ref value, 4, CloakOk);
		SetBit(ref value, 5, SpecialAttackShipOk);
		SetBit(ref value, 6, TargetPositionOk);
		SetBit(ref value, 7, SpecialTargetPlanetOk);
		return value;
	}

	private void ApplyPackedFlags() {
		MoveOk = (_flagsLo & (1 << 0)) != 0;
		AttackOk = (_flagsLo & (1 << 1)) != 0;
		SpecialAttackOk = (_flagsLo & (1 << 2)) != 0;
		SpecialEOAOk = (_flagsLo & (1 << 3)) != 0;
		SpecialAbilityOk = (_flagsLo & (1 << 4)) != 0;
		SpecialAttackWormOk = (_flagsLo & (1 << 5)) != 0;
		DefendOk = (_flagsLo & (1 << 6)) != 0;
		SupplyOk = (_flagsLo & (1 << 7)) != 0;
		HarvestOk = (_flagsLo & (1 << 8)) != 0;
		BuildOk = (_flagsLo & (1 << 9)) != 0;
		RepairOk = (_flagsLo & (1 << 10)) != 0;
		JumpOk = (_flagsLo & (1 << 11)) != 0;
		AdmiralOk = (_flagsLo & (1 << 12)) != 0;
		CaptureOk = (_flagsLo & (1 << 13)) != 0;
		SalvageOk = (_flagsLo & (1 << 14)) != 0;
		ProbeOk = (_flagsLo & (1 << 15)) != 0;
		MimicOk = (_flagsHi & (1 << 0)) != 0;
		RecoverOk = (_flagsHi & (1 << 1)) != 0;
		CreateWormholeOk = (_flagsHi & (1 << 2)) != 0;
		SynthesisOk = (_flagsHi & (1 << 3)) != 0;
		CloakOk = (_flagsHi & (1 << 4)) != 0;
		SpecialAttackShipOk = (_flagsHi & (1 << 5)) != 0;
		TargetPositionOk = (_flagsHi & (1 << 6)) != 0;
		SpecialTargetPlanetOk = (_flagsHi & (1 << 7)) != 0;
	}

	private static void SetBit(ref ushort value, int bit, bool enabled) {
		if (enabled) {
			value |= (ushort)(1 << bit);
		}
	}

	private static void SetBit(ref byte value, int bit, bool enabled) {
		if (enabled) {
			value = (byte)(value | (1 << bit));
		}
	}
}

public sealed class BT_MISSION_DATA {
	public BT_OBJ_CLASS_AND_RACE_AND_DISPLAY_NAME ObjClassAndRaceAndDisplayName { get; set; } = new();
	public BT_CAPS_BLOCK CapsRaw { get; set; } = new();
	public ushort HullPointsMax { get; set; }
	public ushort SupplyPointsMax { get; set; }
	public ushort ScrapValue { get; set; }
	public ushort BuildTime { get; set; }
	public BT_RESOURCE_COST ResourceCost { get; set; } = new();
	public float SensorRadius { get; set; }
	public float CloakedSensorRadius { get; set; }
	public float MaxVelocity { get; set; }
	public float BaseWeaponAccuracy { get; set; }
	public float BaseShieldLevel { get; set; }
	public BT_ARMOR_DATA ArmorData { get; set; } = new();
	public uint SilhouetteImage { get; set; }
	public uint SpecialAbility { get; set; }
	public uint SpecialAbility1 { get; set; }
	public uint SpecialAbility2 { get; set; }
	public uint SpeechPriority { get; set; }
}

public sealed class BT_FIELD_ATTRIBUTES {
	public float SensorDamp { get; set; }
	public float ToHitPenalty { get; set; }
	public float Damage { get; set; }
	public float MoveSpeedModifier { get; set; }
	public float ManeuverModifier { get; set; }
}

public sealed class BT_AMBIENT_NEBULA_LIGHT {
	public BT_COLOR Color { get; set; } = new();
	public byte Padding { get; set; }
	public float PulseFrequency { get; set; }
}

public sealed class BT_PROJECTILE_DATA_BASE {
	public BASIC_DATA Base { get; set; } = new();
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public uint Damage { get; set; }
	public float MaxVelocity { get; set; }
	public string BlastType { get; set; } = string.Empty;
	public string EngineTrailType { get; set; } = string.Empty;
}

public sealed class BT_PLASMABOLT_SEGMENT {
	public BT_VECTOR RollSpeed { get; set; } = new();
	public float BoltWidth { get; set; }
	public float BoltSpacing { get; set; }
	public uint SegmentsX { get; set; }
	public uint SegmentsY { get; set; }
	public uint SegmentsZ { get; set; }
	public BT_VECTOR Offset { get; set; } = new();
	public BT_COLORA BoltColor { get; set; } = new();
}

public sealed class BT_DYNAMICS_DATA {
	public float LinearAcceleration { get; set; }
	public float AngAcceleration { get; set; }
	public float MaxLinearVelocity { get; set; }
	public float MaxAngVelocity { get; set; }
}

public sealed class BT_DYNAMICS_DATA_JR {
	public float MaxLinearVelocity { get; set; }
	public float LinearAcceleration { get; set; }
	public float MaxAngVelocity { get; set; }
}

public sealed class BT_ROCKING_DATA {
	public BT_DYNAMICS_DATA Base { get; set; } = new();
	public float RockLinearMax { get; set; }
	public float RockAngMax { get; set; }
}

public sealed class BT_ENGINE_GLOW_DATA {
	public int[] Size { get; set; } = [];
	public BT_COLOR Color { get; set; } = new();
	public string EngineTextureName { get; set; } = string.Empty;
	public byte Padding { get; set; }
}

public sealed class BT_BLINKER_DATA {
	public string LightScript { get; set; } = string.Empty;
	public string TextureName { get; set; } = string.Empty;
}

public sealed class BT_SHIELD_DATA {
	public string MeshName { get; set; } = string.Empty;
	public string AnimName { get; set; } = string.Empty;
	public string FizzAnimName { get; set; } = string.Empty;
	public uint Sfx { get; set; }
	public uint FizzOut { get; set; }
	public uint FizzIn { get; set; }
}

public sealed class BT_CLOAK_DATA {
	public string CloakTex { get; set; } = string.Empty;
	public bool AutoCloak { get; set; }
	public string CloakEffectType { get; set; } = string.Empty;
}

public sealed class BT_BILLBOARD_DATA {
	public string BillboardTexName { get; set; } = string.Empty;
	public uint BillboardThreshhold { get; set; }
	public bool BTex2 { get; set; }
}

public sealed class BT_BILLBOARD_MESH {
	public string MeshName { get; set; } = string.Empty;
	public string TexName { get; set; } = string.Empty;
	public int SizeMin { get; set; }
	public int SizeMax { get; set; }
	public BT_VECTOR Offset { get; set; } = new();
	public uint BlendMode { get; set; }
}

public sealed class BT_FORMATION_FILTER {
	public uint Flags {
		get {
			uint value = 0;
			SetBit(ref value, 0, ShortRange);
			SetBit(ref value, 1, MediumRange);
			SetBit(ref value, 2, LongRange);
			SetBit(ref value, 3, Recon);
			SetBit(ref value, 4, LoneRecon);
			SetBit(ref value, 5, Fighters);
			SetBit(ref value, 6, SupplyShip);
			SetBit(ref value, 7, AirDefence);
			SetBit(ref value, 8, MissileCruiser);
			SetBit(ref value, 9, Lancer);
			SetBit(ref value, 10, Extra2);
			SetBit(ref value, 11, Extra3);
			SetBit(ref value, 12, Extra4);
			SetBit(ref value, 13, Extra5);
			SetBit(ref value, 14, Extra6);
			SetBit(ref value, 15, Extra7);
			SetBit(ref value, 16, Extra10);
			SetBit(ref value, 17, Extra11);
			SetBit(ref value, 18, Extra12);
			SetBit(ref value, 19, Extra13);
			SetBit(ref value, 20, Extra14);
			SetBit(ref value, 21, Extra15);
			SetBit(ref value, 22, Extra16);
			SetBit(ref value, 23, Extra17);
			SetBit(ref value, 24, Extra20);
			SetBit(ref value, 25, Extra21);
			SetBit(ref value, 26, Extra22);
			SetBit(ref value, 27, Extra23);
			SetBit(ref value, 28, Extra24);
			SetBit(ref value, 29, Extra25);
			SetBit(ref value, 30, Extra26);
			SetBit(ref value, 31, Extra27);
			return value;
		}
		set {
			ShortRange = (value & (1u << 0)) != 0;
			MediumRange = (value & (1u << 1)) != 0;
			LongRange = (value & (1u << 2)) != 0;
			Recon = (value & (1u << 3)) != 0;
			LoneRecon = (value & (1u << 4)) != 0;
			Fighters = (value & (1u << 5)) != 0;
			SupplyShip = (value & (1u << 6)) != 0;
			AirDefence = (value & (1u << 7)) != 0;
			MissileCruiser = (value & (1u << 8)) != 0;
			Lancer = (value & (1u << 9)) != 0;
			Extra2 = (value & (1u << 10)) != 0;
			Extra3 = (value & (1u << 11)) != 0;
			Extra4 = (value & (1u << 12)) != 0;
			Extra5 = (value & (1u << 13)) != 0;
			Extra6 = (value & (1u << 14)) != 0;
			Extra7 = (value & (1u << 15)) != 0;
			Extra10 = (value & (1u << 16)) != 0;
			Extra11 = (value & (1u << 17)) != 0;
			Extra12 = (value & (1u << 18)) != 0;
			Extra13 = (value & (1u << 19)) != 0;
			Extra14 = (value & (1u << 20)) != 0;
			Extra15 = (value & (1u << 21)) != 0;
			Extra16 = (value & (1u << 22)) != 0;
			Extra17 = (value & (1u << 23)) != 0;
			Extra20 = (value & (1u << 24)) != 0;
			Extra21 = (value & (1u << 25)) != 0;
			Extra22 = (value & (1u << 26)) != 0;
			Extra23 = (value & (1u << 27)) != 0;
			Extra24 = (value & (1u << 28)) != 0;
			Extra25 = (value & (1u << 29)) != 0;
			Extra26 = (value & (1u << 30)) != 0;
			Extra27 = (value & (1u << 31)) != 0;
		}
	}

	public bool ShortRange { get; set; }
	public bool MediumRange { get; set; }
	public bool LongRange { get; set; }
	public bool Recon { get; set; }
	public bool LoneRecon { get; set; }
	public bool Fighters { get; set; }
	public bool SupplyShip { get; set; }
	public bool AirDefence { get; set; }
	public bool MissileCruiser { get; set; }
	public bool Lancer { get; set; }
	public bool Extra2 { get; set; }
	public bool Extra3 { get; set; }
	public bool Extra4 { get; set; }
	public bool Extra5 { get; set; }
	public bool Extra6 { get; set; }
	public bool Extra7 { get; set; }
	public bool Extra10 { get; set; }
	public bool Extra11 { get; set; }
	public bool Extra12 { get; set; }
	public bool Extra13 { get; set; }
	public bool Extra14 { get; set; }
	public bool Extra15 { get; set; }
	public bool Extra16 { get; set; }
	public bool Extra17 { get; set; }
	public bool Extra20 { get; set; }
	public bool Extra21 { get; set; }
	public bool Extra22 { get; set; }
	public bool Extra23 { get; set; }
	public bool Extra24 { get; set; }
	public bool Extra25 { get; set; }
	public bool Extra26 { get; set; }
	public bool Extra27 { get; set; }

	private static void SetBit(ref uint value, int bit, bool enabled) {
		if (enabled) {
			value |= 1u << bit;
		}
	}
}

public sealed class BT_BASE_SPACESHIP_DATA {
	public BASIC_DATA Base { get; set; } = new();
	public SPACESHIPCLASS Type { get; set; }
	public string FileName { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public BT_DYNAMICS_DATA DynamicsData { get; set; } = new();
	public BT_ROCKING_DATA RockingData { get; set; } = new();
	public string ExplosionType { get; set; } = string.Empty;
	public string TrailType { get; set; } = string.Empty;
	public string AmbientAnimation { get; set; } = string.Empty;
	public string AmbientEffect { get; set; } = string.Empty;
	public BT_ENGINE_GLOW_DATA EngineGlow { get; set; } = new();
	public BT_BLINKER_DATA Blinkers { get; set; } = new();
	public BT_SHIELD_DATA Shield { get; set; } = new();
	public string DamageBlast { get; set; } = string.Empty;
	public BT_CLOAK_DATA Cloak { get; set; } = new();
	public BT_BILLBOARD_DATA Billboard { get; set; } = new();
	public BT_SINGLE_TECHNODE TechActive { get; set; } = new();
	public BT_FORMATION_FILTER FormationFilter { get; set; } = new();
	public uint LargeShip { get; set; }
}

public sealed class BT_DOCK_TIMING {
	public float UnloadRate { get; set; }
	public float OffDist { get; set; }
	public uint DockingSound { get; set; }
}

public sealed class BT_NUGGET_TIMING {
	public float NuggetTime { get; set; }
	public float OffDist { get; set; }
	public float MinePerSecond { get; set; }
	public string HardpointName { get; set; } = string.Empty;
	public uint NuggetSound { get; set; }
}

public sealed class BT_COMPLEX_COLOR {
	public byte RedHi { get; set; }
	public byte GreenHi { get; set; }
	public byte BlueHi { get; set; }
	public byte RedLo { get; set; }
	public byte GreenLo { get; set; }
	public byte BlueLo { get; set; }
	public byte AlphaIn { get; set; }
	public byte AlphaOut { get; set; }
}

public sealed class BT_PLANET_HALO {
	public BT_COLOR Color { get; set; } = new();
	public byte Padding { get; set; }
	public float SizeInner { get; set; }
	public float SizeOuter { get; set; }
}

public sealed class BT_PLAYERBOMB_TYPE {
	public string ArchetypeName { get; set; } = string.Empty;
}

public sealed class BT_PLAYER_RACE {
	public BT_PLAYERBOMB_TYPE[] MinBombType { get; set; } = [];
	public BT_PLAYERBOMB_TYPE[] BombType { get; set; } = [];
	public BT_PLAYERBOMB_TYPE[] LargeBombType { get; set; } = [];
}

public sealed class BT_EXTENSION_DATA {
	public string ExtensionName { get; set; } = string.Empty;
}

public sealed class BT_RACE_DAMAGE {
	public float Terran { get; set; }
	public float Mantis { get; set; }
	public float Celareon { get; set; }
	public float Vyrium { get; set; }
}

public sealed class BT_BONUS_VALUES {
	public float Damage { get; set; }
	public float SupplyUsage { get; set; }
	public float RangeModifier { get; set; }
	public float Speed { get; set; }
	public float Sensors { get; set; }
	public float Defence { get; set; }
	public float PlatformDamage { get; set; }
	public BT_RACE_DAMAGE HatedRaceDamage { get; set; } = new();
	public BT_ARMOR_DAMAGE HatedArmorDamage { get; set; } = new();
}

public sealed class BT_ADMIRAL_BONUSES {
	public BT_BONUS_VALUES BaseBonuses { get; set; } = new();
	public BT_BONUS_VALUES FavoredShipBonus { get; set; } = new();
	public BT_BONUS_VALUES FavoredArmor { get; set; } = new();
	public uint[] BonusShips { get; set; } = [];
	public uint Flags {
		get {
			uint value = 0;
			if (NoArmorFavored) value |= 1;
			if (LightArmorFavored) value |= 2;
			if (MediumArmorFavored) value |= 4;
			if (HeavyArmorFavored) value |= 8;
			return value;
		}
		set {
			NoArmorFavored = (value & 1) != 0;
			LightArmorFavored = (value & 2) != 0;
			MediumArmorFavored = (value & 4) != 0;
			HeavyArmorFavored = (value & 8) != 0;
		}
	}

	public bool NoArmorFavored { get; set; }
	public bool LightArmorFavored { get; set; }
	public bool MediumArmorFavored { get; set; }
	public bool HeavyArmorFavored { get; set; }
}

public sealed class BT_SHIP_FILTERS {
	public BT_FORMATION_FILTER PositiveFilter { get; set; } = new();
	public BT_FORMATION_FILTER NegativeFilter { get; set; } = new();
	public uint Max { get; set; }
	public uint Min { get; set; }
	public bool OverflowOnly { get; set; }
}

public sealed class BT_ADVANCED_PLACEMENT {
	public PLACEMENT_TYPE PlacementType { get; set; }
	public uint PlacementDirX { get; set; }
	public uint PlacementDirY { get; set; }
}

public sealed class BT_FLEET_GROUP_DEF {
	public BT_SHIP_FILTERS[] Filters { get; set; } = [new(), new(), new(), new(), new(), new()];
	public uint Flags {
		get {
			var value = 0u;
			if (Active) value |= 1;
			if (OverflowCreation) value |= 2;
			return value;
		}
		set {
			Active = (value & 1) != 0;
			OverflowCreation = (value & 2) != 0;
		}
	}
	public uint ParentGroup { get; set; }
	public uint ParentGroupNumber { get; set; }
	public uint Priority { get; set; }
	public uint CreationNumber { get; set; }
	public RELATIVE_TYPE RelativeTo { get; set; }
	public uint RelativeGroupId { get; set; }
	public RELATION Relation { get; set; }
	public uint RelDirX { get; set; }
	public uint RelDirY { get; set; }
	public BT_ADVANCED_PLACEMENT AdvancedPlacement { get; set; } = new();
	public AI_TYPE AiType { get; set; }
	public bool Active { get; set; }
	public bool OverflowCreation { get; set; }
}

public sealed class BT_DRONE_RELEASE {
	public string Hardpoint { get; set; } = string.Empty;
	public string BuilderType { get; set; } = string.Empty;
	public byte NumDrones { get; set; }
}

public sealed class BT_BASE_RESEARCH_DATA {
	public BASIC_DATA Base { get; set; } = new();
	public RESEARCH_TYPE Type { get; set; }
	public BT_RESOURCE_COST Cost { get; set; } = new();
	public uint Time { get; set; }
}

public sealed class BT_BASE_PLATFORM_DATA {
	public BASIC_DATA Base { get; set; } = new();
	public PLATFORMCLASS Type { get; set; }
	public string FileName { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public BT_EXTENSION_DATA[] Extension { get; set; } = [];
	public byte ExtensionBits { get; set; }
	public sbyte ExtensionLevel { get; set; }
	public string ExplosionType { get; set; } = string.Empty;
	public string ShieldHitType { get; set; } = string.Empty;
	public string AmbientAnimation { get; set; } = string.Empty;
	public string AmbientEffect { get; set; } = string.Empty;
	public float Mass { get; set; }
	public BT_SINGLE_TECHNODE TechActive { get; set; } = new();
	public BT_SHIELD_DATA Shield { get; set; } = new();
	public uint SlotsNeeded { get; set; }
	public BT_BLINKER_DATA Blinkers { get; set; } = new();
	public uint CommandPoints { get; set; }
	public uint MetalStorage { get; set; }
	public uint GasStorage { get; set; }
	public uint CrewStorage { get; set; }
	public byte Size { get; set; }
	public bool MoonPlatform { get; set; }
}

public sealed class BT_TURRET_WARM_UP_BLAST {
	public string BlastType { get; set; } = string.Empty;
	public float TriggerTime { get; set; }
}

public sealed class BT_SPECIAL_DAMAGE {
	public uint SupplyDamage { get; set; }
	public float ShieldFraction { get; set; }
	public float MoveFraction { get; set; }
	public float SensorFraction { get; set; }
}

public sealed class BT_MINER_DRONE_RELEASE {
	public string Hardpoint { get; set; } = string.Empty;
	public string BuilderType { get; set; } = string.Empty;
}

public sealed class BT_VERTICAL_SALVO {
	public BT_SINGLE_TECHNODE TechNeed { get; set; } = new();
	public uint Salvo { get; set; }
}

public sealed class BT_MAP_TERRAIN_INFO {
	public string TerrainArchType { get; set; } = string.Empty;
	public float Probability { get; set; }
	public uint MinToPlace { get; set; }
	public uint MaxToPlace { get; set; }
	public MAP_DMAP_FUNC NumberFunc { get; set; }
	public uint Size { get; set; }
	public uint RequiredToPlace { get; set; }
	public MAP_OVERLAP Overlap { get; set; }
	public MAP_PLACEMENT Placement { get; set; }
}

public sealed class BT_MAP_MACRO {
	public MAP_MACRO_OPERATION Operation { get; set; }
	public uint Range { get; set; }
	public bool Active { get; set; }
	public BT_MAP_TERRAIN_INFO Info { get; set; } = new();
}

public sealed class BT_MAP_TERRAIN_THEME {
	public string[] SystemKit { get; set; } = [];
	public string[] MetalPlanets { get; set; } = [];
	public string[] GasPlanets { get; set; } = [];
	public string[] HabitablePlanets { get; set; } = [];
	public string[] OtherPlanets { get; set; } = [];
	public string[] MoonTypes { get; set; } = [];
	public MAP_SECTOR_SIZE SizeOk { get; set; }
	public uint MinSize { get; set; }
	public uint MaxSize { get; set; }
	public MAP_DMAP_FUNC SizeFunc { get; set; }
	public uint[] NumHabitablePlanets { get; set; } = [];
	public uint[] NumMetalPlanets { get; set; } = [];
	public uint[] NumGasPlanets { get; set; } = [];
	public uint[] NumOtherPlanets { get; set; } = [];
	public uint MinMoonsPerPlanet { get; set; }
	public uint MaxMoonsPerPlanet { get; set; }
	public MAP_DMAP_FUNC MoonNumberFunc { get; set; }
	public uint[] NumNuggetPatchesMetal { get; set; } = [];
	public uint[] NumNuggetPatchesGas { get; set; } = [];
	public BT_MAP_TERRAIN_INFO[] Terrain { get; set; } = [];
	public BT_MAP_TERRAIN_INFO[] NuggetMetalTypes { get; set; } = [];
	public BT_MAP_TERRAIN_INFO[] NuggetGasTypes { get; set; } = [];
	public uint FlagsRaw { get; set; }
	public bool OkForPlayerStart { get; set; }
	public bool OkForRemoteSystem { get; set; }
	public float[] Density { get; set; } = [];
	public BT_MAP_MACRO[] Macros { get; set; } = [];
}
