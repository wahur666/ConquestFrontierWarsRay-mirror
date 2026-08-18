namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_WORMHOLE_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public BT_SINGLE_TECHNODE TechMode { get; set; } = new();
	public float DamagePerSec { get; set; }
}

