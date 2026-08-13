namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_BUFF_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float RangeRadius { get; set; }
	public uint BuffType { get; set; }
	public uint TargetType { get; set; }
	public uint SupplyUseType { get; set; }
	public string VisualName { get; set; } = string.Empty;
}

