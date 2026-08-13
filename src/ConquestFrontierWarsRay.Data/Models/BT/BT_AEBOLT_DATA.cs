namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_AEBOLT_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public string ExplosionEffect { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public float MaxVelocity { get; set; }
	public uint Damage { get; set; }
	public float ExplosionRange { get; set; }
	public BT_ARMOR_DATA ArmorData { get; set; } = new();
}

