namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_FIGHTER_DATA : BASIC_DATA {
	public string FileName { get; set; } = string.Empty;
	public BT_DYNAMICS_DATA DynamicsData { get; set; } = new();
	public BT_ARMOR_DATA ArmorData { get; set; } = new();
	public string ExplosionType { get; set; } = string.Empty;
	public string WeaponType { get; set; } = string.Empty;
	public string EngineTrailType { get; set; } = string.Empty;
	public ushort SensorRadius { get; set; }
	public ushort CloakedSensorRadius { get; set; }
	public float PatrolRadius { get; set; }
	public float PatrolAltitude { get; set; }
	public float AttackRange { get; set; }
	public float Dodge { get; set; }
	public uint HullPointsMax { get; set; }
	public uint MaxSupplies { get; set; }
	public float RefirePeriod { get; set; }
	public float PatrolPeriod { get; set; }
	public uint KamikazeDamage { get; set; }
	public uint KamikazeYell { get; set; }
	public uint FighterWhoosh { get; set; }
	public uint FighterLaunch { get; set; }
}

