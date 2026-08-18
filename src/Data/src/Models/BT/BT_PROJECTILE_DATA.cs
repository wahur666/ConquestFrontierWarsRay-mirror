namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PROJECTILE_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public uint Damage { get; set; }
	public float MaxVelocity { get; set; }
	public string BlastType { get; set; } = string.Empty;
	public string EngineTrailType { get; set; } = string.Empty;
}

