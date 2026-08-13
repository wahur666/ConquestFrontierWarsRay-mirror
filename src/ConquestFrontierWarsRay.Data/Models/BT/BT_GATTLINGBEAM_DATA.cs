namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_GATTLINGBEAM_DATA : BASIC_DATA {
	public BASE_WEAPON_DATA WpnClass { get; set; } = new();
	public string FileName { get; set; } = string.Empty;
	public uint LaunchSfx { get; set; }
	public uint Damage { get; set; }
	public float Lifetime { get; set; }
	public float MaxSweepDist { get; set; }
	public float BeamWidth { get; set; }
	public string ContactBlast { get; set; } = string.Empty;
}

