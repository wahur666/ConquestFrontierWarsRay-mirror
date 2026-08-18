namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ARC_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public uint Damage { get; set; }
	public float JumpRange { get; set; }
	public float FeedbackRange { get; set; }
	public uint MaxTendrilStages { get; set; }
	public float DamageDrop { get; set; }
	public uint MaxTargets { get; set; }
	public float Period { get; set; }
	public bool DrainSupplies { get; set; }
}

