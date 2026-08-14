namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_UI_ANIM : BASIC_DATA {
	public string EffectType { get; set; } = string.Empty;
	public float TotalTime { get; set; }
	public uint Sfx { get; set; }
}

