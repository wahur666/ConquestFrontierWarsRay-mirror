namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_OBJECT_GENERATOR : BASIC_DATA {
	public string FileName { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public float Mean { get; set; }
	public float MinDiff { get; set; }
	public string GenerateType { get; set; } = string.Empty;
	public bool StartEnabled { get; set; }
}

