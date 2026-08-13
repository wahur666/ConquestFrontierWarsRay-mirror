namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_JUMPGATE_DATA : BASIC_DATA {
	public BT_BILLBOARD_MESH[] BillboardMesh { get; set; } = [];
	public uint Enter1 { get; set; }
	public uint Enter2 { get; set; }
	public uint Arrive1 { get; set; }
	public uint Arrive2 { get; set; }
	public uint Ambience { get; set; }
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public float MinHoldTime { get; set; }
	public float MinStaggerTime { get; set; }
}

