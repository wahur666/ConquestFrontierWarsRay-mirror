namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_STASISBOLT_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public string ExplosionEffect { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public float MaxVelocity { get; set; }
	public float ExplosionRange { get; set; }
	public float Duration { get; set; }
	public BT_MISSION_DATA MissionData { get; set; } = new();
}

