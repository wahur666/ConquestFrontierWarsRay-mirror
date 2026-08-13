namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_TURRET : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float Info { get; set; }
	public string Animation { get; set; } = string.Empty;
	public string Hardpoint { get; set; } = string.Empty;
	public string Joint { get; set; } = string.Empty;
	public string AnimMuzzleFlash { get; set; } = string.Empty;
	public uint MuzzleFlashWidth { get; set; }
	public float MuzzleFlashTime { get; set; }
	public BT_COLOR ColorMod { get; set; } = new();
	public BT_TURRET_WARM_UP_BLAST WarmUpBlast { get; set; } = new();
}

