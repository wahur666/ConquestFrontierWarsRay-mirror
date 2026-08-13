namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_SYNTHESIS_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public BT_SINGLE_TECHNODE TechNode { get; set; } = new();
	public string AnimName { get; set; } = string.Empty;
}

