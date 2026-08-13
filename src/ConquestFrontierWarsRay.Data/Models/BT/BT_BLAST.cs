namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_BLAST : BASIC_DATA {
	public string EffectType { get; set; } = string.Empty;
	public BT_FLASH_DATA Flash { get; set; } = new();
	public float TotalTime { get; set; }
	public uint Sfx { get; set; }
	public float LeadTime { get; set; }
	public bool DrawThroughFog { get; set; }
}

