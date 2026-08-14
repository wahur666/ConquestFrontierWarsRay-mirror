namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_STREAK_DATA : BASIC_DATA {
	public EFFECTCLASS FxClass { get; set; }
	public string AnimName { get; set; } = string.Empty;
	public uint NumLines { get; set; }
	public float LineTime { get; set; }
}

