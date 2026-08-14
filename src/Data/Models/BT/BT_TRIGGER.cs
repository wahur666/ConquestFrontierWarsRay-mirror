namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_TRIGGER : BASIC_DATA {
	public string FileName { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public TRIGGER_TYPE TriggerType { get; set; }
	public float Size { get; set; }
}

