namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_ADMIRAL_RES : BASIC_DATA {
	public BT_RESEARCH Research { get; set; } = new();
	public string FlagshipType { get; set; } = string.Empty;
}

