namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_NOVA_EXPLOSION : BASIC_DATA {
	public EFFECTCLASS FxClass { get; set; }
	public float InterRingTime { get; set; }
	public float RingTime { get; set; }
	public float Range { get; set; }
	public float Duration { get; set; }
}

