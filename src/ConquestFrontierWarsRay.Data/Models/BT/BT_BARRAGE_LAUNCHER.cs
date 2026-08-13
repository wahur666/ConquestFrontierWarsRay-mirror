namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_BARRAGE_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float DamagePerSec { get; set; }
	public float RangeRadius { get; set; }
	public string FlashType { get; set; } = string.Empty;
}

