namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_TROOPPOD_DATA : BASIC_DATA {
	public EFFECTCLASS FxClass { get; set; }
	public string PodType { get; set; } = string.Empty;
	public string[] PodHardpoints { get; set; } = [];
}

