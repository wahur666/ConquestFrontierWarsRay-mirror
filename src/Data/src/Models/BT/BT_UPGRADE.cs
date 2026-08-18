namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_UPGRADE : BASIC_DATA {
	public BT_BASE_RESEARCH_DATA BaseData { get; set; } = new();
	public BT_SINGLE_TECHNODE Dependancy { get; set; } = new();
	public uint ExtensionId { get; set; }
	public string ResFinishedSound { get; set; } = string.Empty;
	public RESEARCH_SUBTITLE ResFinishSubtitle { get; set; }
}

