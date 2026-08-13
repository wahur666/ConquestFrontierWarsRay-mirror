namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_MULTICLOAK_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float CloakSupplyUse { get; set; }
	public float CloakShutoff { get; set; }
	public float TargetSupplyCostPerHull { get; set; }
	public BT_SINGLE_TECHNODE TechNode { get; set; } = new();
}

