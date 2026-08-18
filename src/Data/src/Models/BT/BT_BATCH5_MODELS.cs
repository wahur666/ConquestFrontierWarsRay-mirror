namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ANMBOLT_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public uint Damage { get; set; }
	public float MaxVelocity { get; set; }
	public string BlastType { get; set; } = string.Empty;
	public string EngineTrailType { get; set; } = string.Empty;
	public string AnimFile { get; set; } = string.Empty;
	public float BoltSize { get; set; }
}

public sealed class BT_GASHARVESTER_DATA : BASIC_DATA {
	public BT_BASE_PLATFORM_DATA BaseData { get; set; } = new();
	public BT_MINER_DRONE_RELEASE[] DroneRelease { get; set; } = [];
	public uint MaxMinerLoad { get; set; }
	public float MineNebTime { get; set; }
	public float MineRadius { get; set; }
	public string ShipHardpoint { get; set; } = string.Empty;
}

public sealed class BT_LASERSPRAY_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public uint Damage { get; set; }
	public float Lifetime { get; set; }
	public float MaxSweepDist { get; set; }
	public float BeamWidth { get; set; }
	public float Velocity { get; set; }
	public string ContactBlast { get; set; } = string.Empty;
}

public sealed class BT_MAP_GEN {
	public BT_MAP_TERRAIN_THEME[] Themes { get; set; } = [];
}

public sealed class BT_OVERDRIVE_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public float Speed { get; set; }
	public uint LaunchSfx { get; set; }
}

public sealed class BT_RECOVERYSHIP_DATA : BASIC_DATA {
	public BT_BASE_SPACESHIP_DATA BaseData { get; set; } = new();
	public float RecoveryRadius { get; set; }
	public float RecoverTime { get; set; }
	public string BeamPointName1 { get; set; } = string.Empty;
	public string BeamPointName2 { get; set; } = string.Empty;
}

public sealed class BT_SPECIALBOLT_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public uint Damage { get; set; }
	public float MaxVelocity { get; set; }
	public string BlastType { get; set; } = string.Empty;
	public string SparkType { get; set; } = string.Empty;
	public BT_FLASH_DATA Flash { get; set; } = new();
	public float Mass { get; set; }
	public BT_SPECIAL_DAMAGE Special { get; set; } = new();
}

public sealed class BT_TRACTOR_WAVE_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float Duration { get; set; }
	public string SpaceWaveType { get; set; } = string.Empty;
	public float WaveFrequency { get; set; }
	public float WaveLifeTime { get; set; }
}

public sealed class BT_VERTICAL_LAUNCH : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public BT_VERTICAL_SALVO[] Upgrade { get; set; } = [];
	public float MiniRefire { get; set; }
	public string[] Hardpoint { get; set; } = [];
}

public sealed class BT_WAYPOINT : BASIC_DATA {
	public string FileName { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
}

public sealed class BT_WORMHOLE_EFFECT : BASIC_DATA {
	public EFFECTCLASS FxClass { get; set; }
}
