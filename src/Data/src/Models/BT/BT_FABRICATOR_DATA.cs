namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_FABRICATOR_DATA : BASIC_DATA {
	public BT_BASE_SPACESHIP_DATA BaseData { get; set; } = new();
	public BT_DRONE_RELEASE[] DroneRelease { get; set; } = [];
	public float RepairRate { get; set; }
	public uint MaxQueueSize { get; set; }
	public uint BuildSound { get; set; }
	public uint BeginBuildSfx { get; set; }
}

