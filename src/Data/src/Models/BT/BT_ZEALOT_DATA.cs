namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ZEALOT_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public float KamikazeSpeed { get; set; }
	public float[] DamageAmount { get; set; } = [];
	public string ImpactBlastType { get; set; } = string.Empty;
	public BT_SINGLE_TECHNODE NeededTech { get; set; } = new();
}

