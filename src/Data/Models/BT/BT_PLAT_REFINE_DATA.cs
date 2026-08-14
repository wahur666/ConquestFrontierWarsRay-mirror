namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PLAT_REFINE_DATA : BASIC_DATA {
	public BT_BASE_PLATFORM_DATA BaseData { get; set; } = new();
	public string ShipHardpoint { get; set; } = string.Empty;
	public string DockHardpoint { get; set; } = string.Empty;
	public BT_DRONE_RELEASE[] DroneRelease { get; set; } = [];
	public int BuildRate { get; set; }
	public uint MaxQueueSize { get; set; }
	public string HarvesterArchetype { get; set; } = string.Empty;
	public float[] GasRate { get; set; } = [];
	public float[] MetalRate { get; set; } = [];
	public float[] CrewRate { get; set; } = [];
}

