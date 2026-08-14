namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_JUMP_LAUNCHER : BASIC_DATA {
	public BASE_LAUNCHER Type { get; set; } = new();
	public string JumpParticle { get; set; } = string.Empty;
}

