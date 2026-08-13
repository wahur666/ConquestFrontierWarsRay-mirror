namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PLAT_REPAIR_DATA : BASIC_DATA {
	public BT_BASE_PLATFORM_DATA BaseData { get; set; } = new();
	public float SupplyPerSecond { get; set; }
	public uint RepairRate { get; set; }
	public uint SupplyRate { get; set; }
	public float SupplyRange { get; set; }
	public string RepairDroneType { get; set; } = string.Empty;
	public string RepairDroneHardpoint { get; set; } = string.Empty;
}

