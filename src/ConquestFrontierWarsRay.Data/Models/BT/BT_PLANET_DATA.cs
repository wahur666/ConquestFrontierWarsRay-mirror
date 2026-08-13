namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PLANET_DATA : BASIC_DATA {
	public string FileMame { get; set; } = string.Empty;
	public string AmbientAnimation { get; set; } = string.Empty;
	public string SysMapIcon { get; set; } = string.Empty;
	public string AmbientEffect { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public ushort MaxMetal { get; set; }
	public ushort MaxGas { get; set; }
	public ushort MaxCrew { get; set; }
	public float MetalRegen { get; set; }
	public float GasRegen { get; set; }
	public float CrewRegen { get; set; }
	public PLANET_TYPE PlanetType { get; set; }
	public string TerraParticle { get; set; } = string.Empty;
	public BT_COLOR TeraColor { get; set; } = new();
	public string TeraExplosions { get; set; } = string.Empty;
	public BT_PLANET_HALO Halo { get; set; } = new();
	public uint Flags {
		get {
			var value = 0u;
			if (Moon) value |= 1;
			if (Uncommon) value |= 2;
			return value;
		}
		set {
			Moon = (value & 1) != 0;
			Uncommon = (value & 2) != 0;
		}
	}
	public bool Moon { get; set; }
	public bool Uncommon { get; set; }
}

