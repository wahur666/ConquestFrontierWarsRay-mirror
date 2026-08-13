namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_PARTICLE_CIRCLE : BASIC_DATA {
	public EFFECTCLASS FxClass { get; set; }
	public BT_COLOR Color { get; set; } = new();
}

