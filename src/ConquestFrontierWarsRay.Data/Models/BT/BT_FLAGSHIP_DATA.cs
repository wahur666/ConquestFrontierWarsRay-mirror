namespace ConquestFrontierWarsRay.Data.Models.BT;

public sealed class BT_FLAGSHIP_DATA : BASIC_DATA {
	public BT_BASE_SPACESHIP_DATA BaseData { get; set; } = new();
	public float AttackRadius { get; set; }
	public BT_ADMIRAL_BONUSES AdmiralBonuses { get; set; } = new();
	public string[] CommandKits { get; set; } = [];
	public string[] StartingFormations { get; set; } = [];
	public BT_ARTIFACT_BUTTON_INFO ToolbarInfo { get; set; } = new();
	public uint MaxQueueSize { get; set; }
}

