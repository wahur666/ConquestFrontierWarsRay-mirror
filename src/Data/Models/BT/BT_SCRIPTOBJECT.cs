namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_SCRIPTOBJECT : BASIC_DATA {
	public string FileName { get; set; } = string.Empty;
	public BT_MISSION_DATA MissionData { get; set; } = new();
	public uint AmbientSound { get; set; }
	public BT_BLINKER_DATA Blinkers { get; set; } = new();
	public string AmbientAnimation { get; set; } = string.Empty;
	public bool SysMapActive { get; set; }
}

