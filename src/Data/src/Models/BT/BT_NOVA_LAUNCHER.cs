namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_NOVA_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float ChargeTime { get; set; }
	public string NovaExplosion { get; set; } = string.Empty;
}

