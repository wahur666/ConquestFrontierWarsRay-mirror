namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_BLACKHOLE_DATA : BASIC_DATA {
	public BT_BILLBOARD_MESH[] BillboardMesh { get; set; } = [];
	public string RingObjectName { get; set; } = string.Empty;
	public string SysMapIcon { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public uint Damage { get; set; }
}

