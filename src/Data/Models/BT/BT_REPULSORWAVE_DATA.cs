namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_REPULSORWAVE_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public uint LaunchSfx { get; set; }
	public float Duration { get; set; }
	public float Range { get; set; }
	public float RingTime { get; set; }
	public float InterRingTime { get; set; }
	public BT_MISSION_DATA MissionData { get; set; } = new();
}

