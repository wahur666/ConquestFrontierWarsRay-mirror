namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_RECONPROBE_DATA : BASIC_DATA {
	public BT_BASE_SPACESHIP_DATA BaseData { get; set; } = new();
	public float LifeTime { get; set; }
	public string BlastType { get; set; } = string.Empty;
}

