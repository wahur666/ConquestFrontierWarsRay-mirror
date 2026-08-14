namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_FIGHTER_WING : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float BaseAirAccuracy { get; set; }
	public float BaseGroundAccuracy { get; set; }
	public uint MaxFighters { get; set; }
	public uint MaxCapFighters { get; set; }
	public uint MaxWingFighters { get; set; }
	public float MinLaunchPeriod { get; set; }
	public uint CostOfNewFighter { get; set; }
	public uint CostOfRefueling { get; set; }
	public uint SpecialWeapon { get; set; }
	public BT_SINGLE_TECHNODE NeededTech { get; set; } = new();
	public string Animation { get; set; } = string.Empty;
	public string Hardpoint { get; set; } = string.Empty;
}

