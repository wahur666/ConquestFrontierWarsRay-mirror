namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_TEMPHQ_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float ChargeRegenRate { get; set; }
	public float ChargeUseRate { get; set; }
	public float MaxCharge { get; set; }
}

