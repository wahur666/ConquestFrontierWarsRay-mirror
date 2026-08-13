namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_MINELAYER_DATA : BASIC_DATA {
	public BT_BASE_SPACESHIP_DATA BaseData { get; set; } = new();
	public string DropAnim { get; set; } = string.Empty;
	public string MineFieldType { get; set; } = string.Empty;
	public string MineReleaseHardpoint { get; set; } = string.Empty;
	public uint FieldSupplyCostPerMine { get; set; }
	public uint FieldCompletionTime { get; set; }
	public uint MineReleaseVelocity { get; set; }
	public float DropAnimDelay { get; set; }
}

