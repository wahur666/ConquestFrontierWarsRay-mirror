namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_REPULSOR_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public string Hardpoint { get; set; } = string.Empty;
	public string ContactBlastType { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public float PushTime { get; set; }
	public float MinimumMass { get; set; }
	public float BasePushPower { get; set; }
	public float PushPerMass { get; set; }
	public BT_SINGLE_TECHNODE NeededTech { get; set; } = new();
}

