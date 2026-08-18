namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ARTILERY_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER BaseLauncher { get; set; } = new();
	public float AreaRadius { get; set; }
	public uint DamagePerSec { get; set; }
	public bool Special { get; set; }
	public string ExplosionType { get; set; } = string.Empty;
	public string FlashType { get; set; } = string.Empty;
}

