namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_RESEARCH : BASIC_DATA {
	public BT_BASE_RESEARCH_DATA BaseData { get; set; } = new();
	public BT_SINGLE_TECHNODE ResearchTech { get; set; } = new();
	public BT_SINGLE_TECHNODE Dependancy { get; set; } = new();
	public string ResFinishedSound { get; set; } = string.Empty;
	public RESEARCH_SUBTITLE ResFinishSubtitle { get; set; }
}

