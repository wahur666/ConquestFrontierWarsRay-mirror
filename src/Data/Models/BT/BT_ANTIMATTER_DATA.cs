namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ANTIMATTER_DATA : BASIC_DATA {
	public uint FieldClass { get; set; }
	public uint InfoHelpId { get; set; }
	public string TextureName { get; set; } = string.Empty;
	public string MapTexName { get; set; } = string.Empty;
	public string SoftwareTexClearName { get; set; } = string.Empty;
	public string SoftwareTexFogName { get; set; } = string.Empty;
	public int Height { get; set; }
	public int SegmentWidth { get; set; }
	public int Spacing { get; set; }
	public BT_MISSION_DATA MissionData { get; set; } = new();
}

