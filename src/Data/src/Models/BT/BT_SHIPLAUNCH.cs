namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_SHIPLAUNCH : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public string Hardpoint { get; set; } = string.Empty;
	public string Animation { get; set; } = string.Empty;
}

