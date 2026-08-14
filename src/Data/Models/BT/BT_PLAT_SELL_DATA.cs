namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PLAT_SELL_DATA : BASIC_DATA {
	public BT_BASE_PLATFORM_DATA BaseData { get; set; } = new();
	public string ShipHardpoint { get; set; } = string.Empty;
	public uint MaxQueueSize { get; set; }
}

