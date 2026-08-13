namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_MOON_RESOURCE_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public float OreRegenRate { get; set; }
	public float GasRegenRate { get; set; }
	public float CrewRegenRate { get; set; }
}

