namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_COMMAND_KIT : BASIC_DATA {
	public BT_BASE_RESEARCH_DATA BaseData { get; set; } = new();
	public BT_ADMIRAL_BONUSES KitBonuses { get; set; } = new();
	public BT_ARTIFACT_BUTTON_INFO ButtonInfo { get; set; } = new();
	public BT_SINGLE_TECHNODE Dependancy { get; set; } = new();
	public string[] Formations { get; set; } = [];
}

