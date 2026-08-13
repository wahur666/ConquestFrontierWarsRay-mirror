namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_MASS_DISRUPTOR_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public uint LaunchSfx { get; set; }
	public string FileName { get; set; } = string.Empty;
	public string ContactBlastType { get; set; } = string.Empty;
	public string WarpAnim { get; set; } = string.Empty;
	public uint AnimWidth { get; set; }
	public float BoltSpeed { get; set; }
	public float DamagePercent { get; set; }
}

