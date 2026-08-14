namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PLASMABOLT_DATA : BASIC_DATA {
	public BT_PROJECTILE_DATA_BASE BaseProjectile { get; set; } = new();
	public string TextureName { get; set; } = string.Empty;
	public string AnimName { get; set; } = string.Empty;
	public float NumBolts { get; set; }
	public BT_PLASMABOLT_SEGMENT[] Bolts { get; set; } = [];
}

