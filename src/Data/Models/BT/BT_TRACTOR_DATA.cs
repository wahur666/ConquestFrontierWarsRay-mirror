namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_TRACTOR_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public string Hardpoint { get; set; } = string.Empty;
	public string ContactBlastType { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public float Duration { get; set; }
	public float DamagePerSecond { get; set; }
	public BT_SINGLE_TECHNODE NeededTech { get; set; } = new();
	public int SupplyCost { get; set; }
	public float RefirePeriod { get; set; }
}

