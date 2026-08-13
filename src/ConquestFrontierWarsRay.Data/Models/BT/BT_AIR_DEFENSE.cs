namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_AIR_DEFENSE : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public string Hardpoint { get; set; } = string.Empty;
	public float BaseAccuracy { get; set; }
	public string FlashTextureName { get; set; } = string.Empty;
	public float FlashWidth { get; set; }
	public float FlashFrequency { get; set; }
	public BT_COLORA FlashColor { get; set; } = new();
	public uint SoundFx { get; set; }
}

