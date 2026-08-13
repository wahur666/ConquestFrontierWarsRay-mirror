namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_MIMIC_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public float SupplyUse { get; set; }
	public float Shutoff { get; set; }
	public BT_SINGLE_TECHNODE TechNode { get; set; } = new();
}

