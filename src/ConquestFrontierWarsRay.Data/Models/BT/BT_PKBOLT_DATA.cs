namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PKBOLT_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public string ExplosionEffect { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public float MaxVelocity { get; set; }
	public string NewPlanetType { get; set; } = string.Empty;
	public float ChangeTime { get; set; }
	public string EngineTrailType { get; set; } = string.Empty;
}

