namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_MINEFIELD_DATA : BASIC_DATA {
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public uint MaxMineNumber { get; set; }
	public uint DamagePerHit { get; set; }
	public uint SupplyDamagePerHit { get; set; }
	public uint HullLostPerHit { get; set; }
	public MINETYPE MineType { get; set; }
	public string RegAnimation { get; set; } = string.Empty;
	public string BlastType { get; set; } = string.Empty;
	public uint MineWidth { get; set; }
	public int MaxVerticalVelocity { get; set; }
	public int MaxHorizontalVelocity { get; set; }
	public uint MineAcceleration { get; set; }
	public float ExplosionRange { get; set; }
	public BT_RESOURCE_COST Cost { get; set; } = new();
}

