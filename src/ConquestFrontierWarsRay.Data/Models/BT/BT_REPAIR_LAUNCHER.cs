namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_REPAIR_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float RangeRadius { get; set; }
	public float RepairRate { get; set; }
	public float RepairCostPerPoint { get; set; }
}

