namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_AEGIS_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public float SupplyPerSec { get; set; }
	public BT_SINGLE_TECHNODE NeededTech { get; set; } = new();
}

